namespace GymManager.Business.Models;

public sealed record MemberListItem(
	int MemberId,
	string MemberIdText,
	string Name,
	string Phone,
	string Plan,
	string EndDateText,
	string Status,
	string StatusColorHex);
