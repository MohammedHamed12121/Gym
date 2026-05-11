namespace GymManager.Business.Models;

public sealed record AddMemberRequest(
	string FullName,
	string PhoneNumber,
	string PlanName,
	DateTime StartDate);
