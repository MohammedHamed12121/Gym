namespace GymManager.MauiView;

using GymManager.Business.Services;

[QueryProperty(nameof(MemberId), "memberId")]
public partial class MemberDetailsPage : ContentPage
{
	private readonly MemberDirectoryService memberDirectoryService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));

	private int memberId;

	public MemberDetailsPage()
	{
		InitializeComponent();
	}

	public string MemberId
	{
		set
		{
			if (int.TryParse(value, out var parsedMemberId))
			{
				memberId = parsedMemberId;
				LoadMember();
			}
		}
	}

	private void LoadMember()
	{
		var details = memberDirectoryService.GetMemberDetails(memberId);
		BindingContext = details;
		DetailsPanel.IsVisible = details is not null;
		NotFoundPanel.IsVisible = details is null;
	}

	private async void OnCancelMembershipClicked(object sender, EventArgs e)
	{
		var confirm = await DisplayAlert(
			"إلغاء العضوية",
			"هل تريد إلغاء عضوية هذا العضو؟ سيظل العضو محفوظاً في النظام.",
			"نعم",
			"لا");

		if (!confirm)
		{
			return;
		}

		memberDirectoryService.CancelMembership(memberId);
		LoadMember();
	}

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("..");
	}
}
