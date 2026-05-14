namespace GymManager.Business.Services;

using System.Globalization;
using GymManager.Business.Models;
using GymManager.Core.Enums;
using GymManager.Core.Models;
using Microsoft.Data.Sqlite;

public sealed class PlanService
{
	private readonly string connectionString;

	public PlanService(string databasePath)
	{
		DatabasePath = databasePath;

		var databaseDirectory = Path.GetDirectoryName(databasePath);
		if (!string.IsNullOrWhiteSpace(databaseDirectory))
		{
			Directory.CreateDirectory(databaseDirectory);
		}

		connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = databasePath
		}.ToString();

		InitializeDatabase();
	}

	public string DatabasePath { get; }

	public int AddPlan(AddPlanRequest request)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			INSERT INTO Plans (Name, Description, Price, Period, PlanTypeId, Status, CreatedAt)
			VALUES ($name, $description, $price, $period, $planTypeId, $status, $createdAt);
			SELECT last_insert_rowid();
			""";

		command.Parameters.AddWithValue("$name", request.Name.Trim());
		command.Parameters.AddWithValue("$description", request.Description.Trim());
		command.Parameters.AddWithValue("$price", request.Price);
		command.Parameters.AddWithValue("$period", (int)request.Period);
		command.Parameters.AddWithValue("$planTypeId", request.PlanTypeId);
		command.Parameters.AddWithValue("$status", (int)PlanStatus.Active);
		command.Parameters.AddWithValue("$createdAt", ToDatabaseDate(DateTime.Now));

		return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
	}

	public void UpdatePlan(UpdatePlanRequest request)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			UPDATE Plans
			SET Name = $name,
				Description = $description,
				Price = $price,
				Period = $period,
				PlanTypeId = $planTypeId
			WHERE Id = $id;
			""";

		command.Parameters.AddWithValue("$id", request.Id);
		command.Parameters.AddWithValue("$name", request.Name.Trim());
		command.Parameters.AddWithValue("$description", request.Description.Trim());
		command.Parameters.AddWithValue("$price", request.Price);
		command.Parameters.AddWithValue("$period", (int)request.Period);
		command.Parameters.AddWithValue("$planTypeId", request.PlanTypeId);
		command.ExecuteNonQuery();
	}

	public void DeletePlan(int planId)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = "DELETE FROM Plans WHERE Id = $id;";
		command.Parameters.AddWithValue("$id", planId);
		command.ExecuteNonQuery();
	}

	public void SetPlanStatus(int planId, PlanStatus status)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			UPDATE Plans
			SET Status = $status
			WHERE Id = $id;
			""";

		command.Parameters.AddWithValue("$id", planId);
		command.Parameters.AddWithValue("$status", (int)status);
		command.ExecuteNonQuery();
	}

	public IReadOnlyList<PlanListItem> GetAllPlans()
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT p.Id, p.Name, p.Description, p.Price, p.Period, p.PlanTypeId, p.Status, p.CreatedAt,
				   pt.Name AS PlanTypeName
			FROM Plans p
			LEFT JOIN PlanTypes pt ON pt.Id = p.PlanTypeId
			ORDER BY p.Id DESC;
			""";

		using var reader = command.ExecuteReader();
		var items = new List<PlanListItem>();

		while (reader.Read())
		{
			var status = (PlanStatus)reader.GetInt32(6);
			var period = (Period)reader.GetInt32(4);

			items.Add(new PlanListItem
			{
				Id = reader.GetInt32(0),
				Name = reader.GetString(1),
				Description = reader.GetString(2),
				PriceText = reader.IsDBNull(3) ? "0" : reader.GetDecimal(3).ToString("F2", CultureInfo.InvariantCulture),
				PeriodName = GetPeriodName(period),
				PlanTypeName = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
				Status = GetStatusName(status),
				StatusColorHex = GetStatusColorHex(status),
				IsActive = status == PlanStatus.Active
			});
		}

		return items;
	}

	public Plan? GetPlanById(int planId)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT Id, Name, Description, Price, Period, PlanTypeId, Status, CreatedAt
			FROM Plans
			WHERE Id = $id;
			""";

		command.Parameters.AddWithValue("$id", planId);

		using var reader = command.ExecuteReader();
		if (!reader.Read())
		{
			return null;
		}

		return new Plan
		{
			Id = reader.GetInt32(0),
			Name = reader.GetString(1),
			Description = reader.GetString(2),
			Price = reader.GetDecimal(3),
			Period = (Period)reader.GetInt32(4),
			PlanTypeId = reader.GetInt32(5),
			Status = (PlanStatus)reader.GetInt32(6),
			CreatedAt = DateTime.Parse(reader.GetString(7), CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind)
		};
	}

	public bool IsValidAddPlanRequest(AddPlanRequest request, out string validationMessage)
	{
		if (string.IsNullOrWhiteSpace(request.Name))
		{
			validationMessage = "اسم الخطة مطلوب.";
			return false;
		}

		if (request.Price < 0)
		{
			validationMessage = "السعر يجب أن يكون قيمة موجبة.";
			return false;
		}

		if (request.PlanTypeId <= 0)
		{
			validationMessage = "نوع الخطة مطلوب.";
			return false;
		}

		validationMessage = string.Empty;
		return true;
	}

	public bool IsValidUpdatePlanRequest(UpdatePlanRequest request, out string validationMessage)
	{
		if (string.IsNullOrWhiteSpace(request.Name))
		{
			validationMessage = "اسم الخطة مطلوب.";
			return false;
		}

		if (request.Price < 0)
		{
			validationMessage = "السعر يجب أن يكون قيمة موجبة.";
			return false;
		}

		if (request.PlanTypeId <= 0)
		{
			validationMessage = "نوع الخطة مطلوب.";
			return false;
		}

		validationMessage = string.Empty;
		return true;
	}

	private void InitializeDatabase()
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			CREATE TABLE IF NOT EXISTS Plans (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				Name TEXT NOT NULL,
				Description TEXT NOT NULL,
				Price REAL NOT NULL,
				Period INTEGER NOT NULL DEFAULT 1,
				PlanTypeId INTEGER NOT NULL,
				Status INTEGER NOT NULL DEFAULT 1,
				CreatedAt TEXT NOT NULL,
				FOREIGN KEY (PlanTypeId) REFERENCES PlanTypes(Id)
			);
			""";

		command.ExecuteNonQuery();
		AddColumnIfMissing(connection, "Plans", "Period", "INTEGER NOT NULL DEFAULT 1");
	}

	private void AddColumnIfMissing(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
	{
		if (ColumnExists(connection, tableName, columnName))
		{
			return;
		}

		using var command = connection.CreateCommand();
		command.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};";
		command.ExecuteNonQuery();
	}

	private static bool ColumnExists(SqliteConnection connection, string tableName, string columnName)
	{
		using var command = connection.CreateCommand();
		command.CommandText = $"PRAGMA table_info({tableName});";

		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	private SqliteConnection OpenConnection()
	{
		var connection = new SqliteConnection(connectionString);
		connection.Open();
		return connection;
	}

	private static string GetPeriodName(Period period)
	{
		return period switch
		{
			Period.Daily => "يومي",
			Period.Quarter => "ربع سنوي",
			Period.Year => "سنوي",
			_ => "شهري"
		};
	}

	private static string GetStatusName(PlanStatus status)
	{
		return status switch
		{
			PlanStatus.NotActive => "غير نشط",
			_ => "نشط"
		};
	}

	private static string GetStatusColorHex(PlanStatus status)
	{
		return status switch
		{
			PlanStatus.NotActive => "#6B7280",
			_ => "#22A06B"
		};
	}

	private static string ToDatabaseDate(DateTime date)
	{
		return date.ToString("O", CultureInfo.InvariantCulture);
	}
}
