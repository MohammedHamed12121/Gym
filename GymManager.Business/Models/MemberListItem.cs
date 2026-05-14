namespace GymManager.Business.Models;

public sealed class MemberListItem
{
	public int MemberId { get; set; }

	public string MemberIdText { get; set; } = string.Empty;

	public string Name { get; set; } = string.Empty;

	public string Phone { get; set; } = string.Empty;

	public string Plan { get; set; } = string.Empty;

	public string EndDateText { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string StatusColorHex { get; set; } = string.Empty;

	public bool IsCheckedIn { get; set; }

	public bool CanCheckIn { get; set; }
}
