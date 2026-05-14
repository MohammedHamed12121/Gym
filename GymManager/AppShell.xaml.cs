namespace GymManager.MauiView;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(MemberDetailsPage), typeof(MemberDetailsPage));
		Routing.RegisterRoute(nameof(PlanTypePage), typeof(PlanTypePage));
		Routing.RegisterRoute(nameof(PlanPage), typeof(PlanPage));
	}
}
