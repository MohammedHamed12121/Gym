namespace GymManager.MauiView;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(MemberDetailsPage), typeof(MemberDetailsPage));
	}
}
