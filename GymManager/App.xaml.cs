namespace GymManager;

using System.Globalization;

public partial class App : Application
{
	public App()
	{
		var arabicCulture = new CultureInfo("ar-EG");
		CultureInfo.DefaultThreadCurrentCulture = arabicCulture;
		CultureInfo.DefaultThreadCurrentUICulture = arabicCulture;

		InitializeComponent();

		MainPage = new AppShell();
	}
}
