namespace GymManager.Business.Services;

using System.Globalization;
using GymManager.Business.Models;
using GymManager.Core.Enums;
using GymManager.Core.Models;
using Microsoft.Data.Sqlite;

public sealed class PlanTypeService
{
	private readonly string connectionString;

	public PlanTypeService(string databasePath)
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
		SeedPlanTypesIfEmpty();
	}

	public string DatabasePath { get; }

	public int AddPlanType(AddPlanTypeRequest request)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			INSERT INTO PlanTypes (Name, Status, CreatedAt)
			VALUES ($name, $status, $createdAt);
			SELECT last_insert_rowid();
			""";

		command.Parameters.AddWithValue("$name", request.Name.Trim());
		command.Parameters.AddWithValue("$status", (int)PlanTypeStatus.Active);
		command.Parameters.AddWithValue("$createdAt", ToDatabaseDate(DateTime.Now));

		return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
	}

	public IReadOnlyList<PlanTypeListItem> GetAllPlanTypes()
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT Id, Name, Status, CreatedAt
			FROM PlanTypes
			ORDER BY Id DESC;
			""";

		using var reader = command.ExecuteReader();
		var items = new List<PlanTypeListItem>();

		while (reader.Read())
		{
			var status = (PlanTypeStatus)reader.GetInt32(2);

			items.Add(new PlanTypeListItem
			{
				Id = reader.GetInt32(0),
				Name = reader.GetString(1),
				Status = GetStatusName(status),
				StatusColorHex = GetStatusColorHex(status),
				IsActive = status == PlanTypeStatus.Active
			});
		}

		return items;
	}

	public void SetPlanTypeStatus(int planTypeId, PlanTypeStatus status)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			UPDATE PlanTypes
			SET Status = $status
			WHERE Id = $id;
			""";

		command.Parameters.AddWithValue("$id", planTypeId);
		command.Parameters.AddWithValue("$status", (int)status);
		command.ExecuteNonQuery();
	}

	public void DeletePlanType(int planTypeId)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = "DELETE FROM PlanTypes WHERE Id = $id;";
		command.Parameters.AddWithValue("$id", planTypeId);
		command.ExecuteNonQuery();
	}

	public bool IsValidAddPlanTypeRequest(AddPlanTypeRequest request, out string validationMessage)
	{
		if (string.IsNullOrWhiteSpace(request.Name))
		{
			validationMessage = "اسم نوع الخطة مطلوب.";
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
			CREATE TABLE IF NOT EXISTS PlanTypes (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				Name TEXT NOT NULL,
				Status INTEGER NOT NULL,
				CreatedAt TEXT NOT NULL
			);
			""";

		command.ExecuteNonQuery();
	}

	private void SeedPlanTypesIfEmpty()
	{
		using var connection = OpenConnection();

		using var countCommand = connection.CreateCommand();
		countCommand.CommandText = "SELECT COUNT(*) FROM PlanTypes;";

		var count = Convert.ToInt32(countCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
		if (count > 0)
		{
			return;
		}

		InsertSeedPlanType(connection, "شهري");
		InsertSeedPlanType(connection, "ربع سنوي");
		InsertSeedPlanType(connection, "سنوي");
		InsertSeedPlanType(connection, "يومي");
	}

	private void InsertSeedPlanType(SqliteConnection connection, string name)
	{
		using var command = connection.CreateCommand();

		command.CommandText = """
			INSERT INTO PlanTypes (Name, Status, CreatedAt)
			VALUES ($name, $status, $createdAt);
			""";

		command.Parameters.AddWithValue("$name", name);
		command.Parameters.AddWithValue("$status", (int)PlanTypeStatus.Active);
		command.Parameters.AddWithValue("$createdAt", ToDatabaseDate(DateTime.Now));
		command.ExecuteNonQuery();
	}

	private SqliteConnection OpenConnection()
	{
		var connection = new SqliteConnection(connectionString);
		connection.Open();
		return connection;
	}

	private static string GetStatusName(PlanTypeStatus status)
	{
		return status switch
		{
			PlanTypeStatus.NotActive => "غير نشط",
			_ => "نشط"
		};
	}

	private static string GetStatusColorHex(PlanTypeStatus status)
	{
		return status switch
		{
			PlanTypeStatus.NotActive => "#6B7280",
			_ => "#22A06B"
		};
	}

	private static string ToDatabaseDate(DateTime date)
	{
		return date.ToString("O", CultureInfo.InvariantCulture);
	}
}
