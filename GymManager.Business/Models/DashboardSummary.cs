namespace GymManager.Business.Models;

public sealed record DashboardSummary(
	int ActiveMembers,
	int ExpiringMembers,
	int TodayCheckIns,
	int ExpiredSubscriptions);
