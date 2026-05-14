namespace GymManager.Business.Services;

using System.Globalization;
using GymManager.Business.Models;
using GymManager.Core.Models;
using Microsoft.Data.Sqlite;

public sealed class AttendanceService
{
	private readonly string connectionString;

	public AttendanceService(string databasePath)
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

	public int CheckIn(int memberId, string memberName, string playType)
	{
		var now = DateTime.Now;

		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			INSERT INTO Attendance (MemberId, MemberName, AttendDateTime, AttendDay, AttendMonth, AttendYear, PlayType, Status)
			VALUES ($memberId, $memberName, $attendDateTime, $attendDay, $attendMonth, $attendYear, $playType, 'نشط');
			SELECT last_insert_rowid();
			""";

		command.Parameters.AddWithValue("$memberId", memberId);
		command.Parameters.AddWithValue("$memberName", memberName.Trim());
		command.Parameters.AddWithValue("$attendDateTime", ToDatabaseDate(now));
		command.Parameters.AddWithValue("$attendDay", now.Day);
		command.Parameters.AddWithValue("$attendMonth", now.Month);
		command.Parameters.AddWithValue("$attendYear", now.Year);
		command.Parameters.AddWithValue("$playType", playType.Trim());

		return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
	}

	public IReadOnlyList<Attendance> GetTodayAttendance()
	{
		return GetAttendanceByDate(DateTime.Today);
	}

	public IReadOnlyList<Attendance> GetAttendanceByDate(DateTime date)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT Id, MemberId, MemberName, AttendDateTime, AttendDay, AttendMonth, AttendYear, PlayType, Status
			FROM Attendance
			WHERE AttendYear = $year AND AttendMonth = $month AND AttendDay = $day
			ORDER BY AttendDateTime DESC;
			""";

		command.Parameters.AddWithValue("$year", date.Year);
		command.Parameters.AddWithValue("$month", date.Month);
		command.Parameters.AddWithValue("$day", date.Day);

		return ReadAttendances(command);
	}

	public IReadOnlyList<Attendance> GetAttendanceByMember(int memberId)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT Id, MemberId, MemberName, AttendDateTime, AttendDay, AttendMonth, AttendYear, PlayType, Status
			FROM Attendance
			WHERE MemberId = $memberId
			ORDER BY AttendDateTime DESC;
			""";

		command.Parameters.AddWithValue("$memberId", memberId);

		return ReadAttendances(command);
	}

	public IReadOnlyList<Attendance> SearchAttendance(string? searchText)
	{
		var search = searchText?.Trim() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(search))
		{
			return GetTodayAttendance();
		}

		if (int.TryParse(search, out var memberId))
		{
			return GetAttendanceByMember(memberId);
		}

		return GetTodayAttendance();
	}

	public AttendanceSummary GetAttendanceSummary()
	{
		var now = DateTime.Now;
		var todayCount = GetDateCount(now.Year, now.Month, now.Day);
		var monthCount = GetMonthCount(now.Year, now.Month);

		return new AttendanceSummary(todayCount, monthCount);
	}

	public int GetTodayCheckInCount()
	{
		var now = DateTime.Now;
		return GetDateCount(now.Year, now.Month, now.Day);
	}

	public int GetMonthCheckInCount(int year, int month)
	{
		return GetMonthCount(year, month);
	}

	private int GetDateCount(int year, int month, int day)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT COUNT(*)
			FROM Attendance
			WHERE AttendYear = $year AND AttendMonth = $month AND AttendDay = $day;
			""";

		command.Parameters.AddWithValue("$year", year);
		command.Parameters.AddWithValue("$month", month);
		command.Parameters.AddWithValue("$day", day);

		return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
	}

	private int GetMonthCount(int year, int month)
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			SELECT COUNT(*)
			FROM Attendance
			WHERE AttendYear = $year AND AttendMonth = $month;
			""";

		command.Parameters.AddWithValue("$year", year);
		command.Parameters.AddWithValue("$month", month);

		return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
	}

	private void InitializeDatabase()
	{
		using var connection = OpenConnection();
		using var command = connection.CreateCommand();

		command.CommandText = """
			CREATE TABLE IF NOT EXISTS Attendance (
				Id INTEGER PRIMARY KEY AUTOINCREMENT,
				MemberId INTEGER NOT NULL,
				MemberName TEXT NOT NULL,
				AttendDateTime TEXT NOT NULL,
				AttendDay INTEGER NOT NULL,
				AttendMonth INTEGER NOT NULL,
				AttendYear INTEGER NOT NULL,
				PlayType TEXT NOT NULL,
				Status TEXT NOT NULL DEFAULT 'نشط'
			);
			""";

		command.ExecuteNonQuery();
	}

	private static List<Attendance> ReadAttendances(SqliteCommand command)
	{
		using var reader = command.ExecuteReader();
		var attendances = new List<Attendance>();

		while (reader.Read())
		{
			attendances.Add(new Attendance
			{
				Id = reader.GetInt32(0),
				MemberId = reader.GetInt32(1),
				MemberName = reader.GetString(2),
				AttendDateTime = FromDatabaseDate(reader.GetString(3)),
				AttendDay = reader.GetInt32(4),
				AttendMonth = reader.GetInt32(5),
				AttendYear = reader.GetInt32(6),
				PlayType = reader.GetString(7),
				Status = reader.GetString(8)
			});
		}

		return attendances;
	}

	private SqliteConnection OpenConnection()
	{
		var connection = new SqliteConnection(connectionString);
		connection.Open();
		return connection;
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
