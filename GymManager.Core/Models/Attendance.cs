namespace GymManager.Core.Models;

public sealed class Attendance
{
	public int Id { get; set; }

	public int MemberId { get; set; }

	public string MemberName { get; set; } = string.Empty;

	public DateTime AttendDateTime { get; set; }

	public int AttendDay { get; set; }

	public int AttendMonth { get; set; }

	public int AttendYear { get; set; }

	public string PlayType { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;
}
