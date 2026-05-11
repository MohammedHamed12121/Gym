namespace GymManager.Core.Models;

using GymManager.Core.Enums;

public sealed class Member
{
	public int Id { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string PhoneNumber { get; set; } = string.Empty;

	public MembershipPlan Plan { get; set; }

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;

	public bool IsCanceled { get; set; }

	public DateTime? CanceledAt { get; set; }
}
