namespace GymManager.Business.Models;

public sealed record MemberDetails(
	int MemberId,
	string MemberIdText,
	string Name,
	string Phone,
	string Plan,
	string StartDateText,
	string EndDateText,
	string CreatedAtText,
	string CanceledAtText,
	string Status,
	string StatusColorHex,
	string RemainingDaysText,
	bool CanCancelMembership);
