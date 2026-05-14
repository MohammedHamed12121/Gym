namespace GymManager.Business.Services;

using System.Globalization;
using GymManager.Business.Models;
using GymManager.Core.Enums;
using GymManager.Core.Models;
using Microsoft.Data.Sqlite;

public sealed class MemberDirectoryService
{
	private readonly string connectionString;

	public MemberDirectoryService(string databasePath)
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
		SeedMembersIfEmpty();
	}

	public string DatabasePath { get; }

	public IReadOnlyList<string> GetPlanNames()
	{
		return ["شهري", "ربع سنوي", "سنوي", "يومي"];
	}

	public int AddMember(AddMemberRequest request)
	{
		var plan = ParsePlan(request.PlanName);
		var startDate = request.StartDate.Date;
		var endDate = startDate.AddDays(GetPlanDurationDays(plan));

		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			INSERT INTO Members (FullName, PhoneNumber, Plan, StartDate, EndDate, CreatedAt, IsCanceled, CanceledAt)
			VALUES ($fullName, $phoneNumber, $plan, $startDate, $endDate, $createdAt, 0, NULL);
			SELECT last_insert_rowid();
			""";

		command.Parameters.AddWithValue("$fullName", request.FullName.Trim());
		command.Parameters.AddWithValue("$phoneNumber", request.PhoneNumber.Trim());
		command.Parameters.AddWithValue("$plan", (int)plan);
		command.Parameters.AddWithValue("$startDate", ToDatabaseDate(startDate));
		command.Parameters.AddWithValue("$endDate", ToDatabaseDate(endDate));
		command.Parameters.AddWithValue("$createdAt", ToDatabaseDate(DateTime.Now));

		return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
	}

	public void CancelMembership(int memberId)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			UPDATE Members
			SET IsCanceled = 1,
				CanceledAt = $canceledAt
			WHERE Id = $id;
			""";

		command.Parameters.AddWithValue("$id", memberId);
		command.Parameters.AddWithValue("$canceledAt", ToDatabaseDate(DateTime.Now));
		command.ExecuteNonQuery();
	}

	public IReadOnlyList<MemberListItem> SearchMembers(string? searchText)
	{
		var search = searchText?.Trim() ?? string.Empty;
		var members = LoadMembers();

		if (ShouldSearchByExactMemberId(search, out var memberId))
		{
			return members
				.Where(member => member.Id == memberId)
				.Select(CreateListItem)
		.Take(30)
		.ToList();
        }

		return members
			.Where(member => MatchesText(member, search))
			.Select(CreateListItem)
		.Take(30)
		.ToList();
	}

	public DashboardSummary GetDashboardSummary()
	{
		var members = LoadMembers();

		return new DashboardSummary(
			ActiveMembers: members.Count(member => !member.IsCanceled && member.EndDate >= DateTime.Today),
			ExpiringMembers: members.Count(member => !member.IsCanceled && member.EndDate >= DateTime.Today && member.EndDate <= DateTime.Today.AddDays(7)),
			TodayCheckIns: 0,
			ExpiredSubscriptions: members.Count(member => !member.IsCanceled && member.EndDate < DateTime.Today));
	}

	public MemberDetails? GetMemberDetails(int memberId)
	{
		var member = LoadMembers().FirstOrDefault(member => member.Id == memberId);
		if (member is null)
		{
			return null;
		}

		var status = GetStatus(member);

		return new MemberDetails(
			MemberId: member.Id,
			MemberIdText: $"رقم العضوية: {member.Id}",
			Name: member.FullName,
			Phone: member.PhoneNumber,
			Plan: GetPlanName(member.Plan),
			StartDateText: $"تاريخ البداية: {member.StartDate:dd MMMM yyyy}",
			EndDateText: $"تاريخ الانتهاء: {member.EndDate:dd MMMM yyyy}",
			CreatedAtText: $"تاريخ التسجيل: {member.CreatedAt:dd MMMM yyyy}",
			CanceledAtText: member.CanceledAt is null ? string.Empty : $"تاريخ الإلغاء: {member.CanceledAt:dd MMMM yyyy}",
			Status: GetStatusName(status),
			StatusColorHex: GetStatusColorHex(status),
			RemainingDaysText: GetRemainingDaysText(member),
			CanCancelMembership: !member.IsCanceled);
	}

	public bool IsValidAddMemberRequest(AddMemberRequest request, out string validationMessage)
	{
		if (string.IsNullOrWhiteSpace(request.FullName))
		{
			validationMessage = "اسم العضو مطلوب.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(request.PhoneNumber))
		{
			validationMessage = "رقم الهاتف مطلوب.";
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
			CREATE TABLE IF NOT EXISTS Members (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				FullName TEXT NOT NULL,
				PhoneNumber TEXT NOT NULL,
				Plan INTEGER NOT NULL,
				StartDate TEXT NOT NULL,
				EndDate TEXT NOT NULL,
				CreatedAt TEXT NOT NULL,
				IsCanceled INTEGER NOT NULL DEFAULT 0,
				CanceledAt TEXT NULL
			);
			""";

		command.ExecuteNonQuery();
		AddColumnIfMissing(connection, "Members", "IsCanceled", "INTEGER NOT NULL DEFAULT 0");
		AddColumnIfMissing(connection, "Members", "CanceledAt", "TEXT NULL");
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

	private void SeedMembersIfEmpty()
	{
		using var connection = OpenConnection();

		using var countCommand = connection.CreateCommand();
		countCommand.CommandText = "SELECT COUNT(*) FROM Members;";

		var memberCount = Convert.ToInt32(countCommand.ExecuteScalar(), CultureInfo.InvariantCulture);
		if (memberCount > 0)
		{
			return;
		}

		InsertSeedMember(connection, "أحمد حسن", "01012345678", MembershipPlan.Monthly, DateTime.Today.AddDays(-8));
		InsertSeedMember(connection, "منى علي", "01176543210", MembershipPlan.Quarterly, DateTime.Today.AddDays(-70));
		InsertSeedMember(connection, "عمر سمير", "01211112222", MembershipPlan.Monthly, DateTime.Today.AddDays(-28));
	}

	private void InsertSeedMember(SqliteConnection connection, string fullName, string phoneNumber, MembershipPlan plan, DateTime startDate)
	{
		using var command = connection.CreateCommand();
		var endDate = startDate.Date.AddDays(GetPlanDurationDays(plan));

		command.CommandText = """
			INSERT INTO Members (FullName, PhoneNumber, Plan, StartDate, EndDate, CreatedAt, IsCanceled, CanceledAt)
			VALUES ($fullName, $phoneNumber, $plan, $startDate, $endDate, $createdAt, 0, NULL);
			""";

		command.Parameters.AddWithValue("$fullName", fullName);
		command.Parameters.AddWithValue("$phoneNumber", phoneNumber);
		command.Parameters.AddWithValue("$plan", (int)plan);
		command.Parameters.AddWithValue("$startDate", ToDatabaseDate(startDate.Date));
		command.Parameters.AddWithValue("$endDate", ToDatabaseDate(endDate));
		command.Parameters.AddWithValue("$createdAt", ToDatabaseDate(DateTime.Now));
		command.ExecuteNonQuery();
	}

	private List<Member> LoadMembers()
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT Id, FullName, PhoneNumber, Plan, StartDate, EndDate, CreatedAt, IsCanceled, CanceledAt
			FROM Members
			ORDER BY Id DESC;
			""";

		using var reader = command.ExecuteReader();
		var members = new List<Member>();

		while (reader.Read())
		{
			members.Add(new Member
			{
				Id = reader.GetInt32(0),
				FullName = reader.GetString(1),
				PhoneNumber = reader.GetString(2),
				Plan = (MembershipPlan)reader.GetInt32(3),
				StartDate = FromDatabaseDate(reader.GetString(4)),
				EndDate = FromDatabaseDate(reader.GetString(5)),
				CreatedAt = FromDatabaseDate(reader.GetString(6)),
				IsCanceled = reader.GetInt32(7) == 1,
				CanceledAt = reader.IsDBNull(8) ? null : FromDatabaseDate(reader.GetString(8))
			});
		}

		return members;
	}

	private SqliteConnection OpenConnection()
	{
		var connection = new SqliteConnection(connectionString);
		connection.Open();
		return connection;
	}

	private static bool ShouldSearchByExactMemberId(string search, out int memberId)
	{
		memberId = 0;

		return !string.IsNullOrWhiteSpace(search)
			&& !search.StartsWith('0')
			&& int.TryParse(search, NumberStyles.None, CultureInfo.InvariantCulture, out memberId)
			&& memberId > 0;
	}

	private static bool MatchesText(Member member, string search)
	{
		return string.IsNullOrWhiteSpace(search)
			|| member.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)
			|| member.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
			|| GetPlanName(member.Plan).Contains(search, StringComparison.OrdinalIgnoreCase)
			|| GetStatusName(GetStatus(member)).Contains(search, StringComparison.OrdinalIgnoreCase);
	}

	private static MemberListItem CreateListItem(Member member)
	{
		var status = GetStatus(member);

		return new MemberListItem
		{
			MemberId = member.Id,
			MemberIdText = $"رقم العضوية: {member.Id}",
			Name = member.FullName,
			Phone = member.PhoneNumber,
			Plan = GetPlanName(member.Plan),
			EndDateText = $"ينتهي في {member.EndDate:dd MMMM yyyy}",
			Status = GetStatusName(status),
			StatusColorHex = GetStatusColorHex(status),
			CanCheckIn = status is MembershipStatus.Active or MembershipStatus.RenewSoon
		};
	}

	private static MembershipStatus GetStatus(Member member)
	{
		if (member.IsCanceled)
		{
			return MembershipStatus.Canceled;
		}

		if (member.EndDate < DateTime.Today)
		{
			return MembershipStatus.Expired;
		}

		return member.EndDate <= DateTime.Today.AddDays(7)
			? MembershipStatus.RenewSoon
			: MembershipStatus.Active;
	}

	private static MembershipPlan ParsePlan(string planName)
	{
		return planName switch
		{
			"ربع سنوي" => MembershipPlan.Quarterly,
			"سنوي" => MembershipPlan.Annual,
			"يومي" => MembershipPlan.Daily,
			_ => MembershipPlan.Monthly
		};
	}

	private static string GetPlanName(MembershipPlan plan)
	{
		return plan switch
		{
			MembershipPlan.Quarterly => "ربع سنوي",
			MembershipPlan.Annual => "سنوي",
			MembershipPlan.Daily => "يومي",
			_ => "شهري"
		};
	}

	private static int GetPlanDurationDays(MembershipPlan plan)
	{
		return plan switch
		{
			MembershipPlan.Quarterly => 90,
			MembershipPlan.Annual => 365,
			MembershipPlan.Daily => 1,
			_ => 30
		};
	}

	private static string GetStatusName(MembershipStatus status)
	{
		return status switch
		{
			MembershipStatus.Canceled => "ملغي",
			MembershipStatus.Expired => "منتهي",
			MembershipStatus.RenewSoon => "تجديد قريب",
			_ => "نشط"
		};
	}

	private static string GetStatusColorHex(MembershipStatus status)
	{
		return status switch
		{
			MembershipStatus.Canceled => "#6B7280",
			MembershipStatus.Expired => "#C2410C",
			MembershipStatus.RenewSoon => "#B7791F",
			_ => "#22A06B"
		};
	}

	private static string GetRemainingDaysText(Member member)
	{
		if (member.IsCanceled)
		{
			return "تم إلغاء العضوية";
		}

		var remainingDays = (member.EndDate.Date - DateTime.Today).Days;

		if (remainingDays < 0)
		{
			return $"منتهي منذ {Math.Abs(remainingDays)} يوم";
		}

		if (remainingDays == 0)
		{
			return "ينتهي اليوم";
		}

		return $"متبقي {remainingDays} يوم";
	}

	private static string ToDatabaseDate(DateTime date)
	{
		return date.ToString("O", CultureInfo.InvariantCulture);
	}

	private static DateTime FromDatabaseDate(string date)
	{
		return DateTime.Parse(date, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
	}
}
