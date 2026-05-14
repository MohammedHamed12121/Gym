namespace GymManager.MauiView;

using System.Collections.ObjectModel;
using GymManager.Business.Models;
using GymManager.Business.Services;

public partial class MainPage : ContentPage
{
	private readonly MemberDirectoryService memberDirectoryService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly AttendanceService attendanceService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly ObservableCollection<MemberListItem> filteredMembers = [];

	public MainPage()
	{
		InitializeComponent();

		MembersCollection.ItemsSource = filteredMembers;
		PlanPicker.ItemsSource = memberDirectoryService.GetPlanNames().ToList();
		PlanPicker.SelectedIndex = 0;
		StartDatePicker.Date = DateTime.Today;

		RefreshMembers();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		RefreshMembers();
	}

	private void OnAddMemberClicked(object sender, EventArgs e)
	{
		FormMessageLabel.Text = string.Empty;
		FormMessageLabel.TextColor = Color.FromArgb("#C2410C");

		var request = new AddMemberRequest(
			FullName: NameEntry.Text?.Trim() ?? string.Empty,
			PhoneNumber: PhoneEntry.Text?.Trim() ?? string.Empty,
			PlanName: PlanPicker.SelectedItem?.ToString() ?? "شهري",
			StartDate: StartDatePicker.Date);

		if (!memberDirectoryService.IsValidAddMemberRequest(request, out var validationMessage))
		{
			FormMessageLabel.Text = validationMessage;
			return;
		}

		var memberId = memberDirectoryService.AddMember(request);
		attendanceService.CheckIn(memberId, request.FullName, request.PlanName);
		RefreshMembers();
		ClearForm();

		FormMessageLabel.TextColor = Color.FromArgb("#22A06B");
		FormMessageLabel.Text = $"تمت إضافة العضو برقم عضوية {memberId}.";
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		RefreshMembers();
	}

	private async void OnMemberSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is not MemberListItem member)
		{
			return;
		}

		((CollectionView)sender).SelectedItem = null;
		await Shell.Current.GoToAsync($"{nameof(MemberDetailsPage)}?memberId={member.MemberId}");
	}

	private void OnCheckInClicked(object sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not MemberListItem member)
		{
			return;
		}

		attendanceService.CheckIn(member.MemberId, member.Name, member.Plan);
		RefreshMembers();
	}

	private void RefreshMembers()
	{
		var todayAttendees = attendanceService.GetTodayAttendance()
			.Select(a => a.MemberId)
			.ToHashSet();

		filteredMembers.Clear();

		foreach (var member in memberDirectoryService.SearchMembers(MemberSearchBar.Text))
		{
			member.IsCheckedIn = todayAttendees.Contains(member.MemberId);
			filteredMembers.Add(member);
		}

		UpdateDashboard();
	}

	private void UpdateDashboard()
	{
		var summary = memberDirectoryService.GetDashboardSummary();

		ActiveMembersLabel.Text = summary.ActiveMembers.ToString();
		ExpiringMembersLabel.Text = summary.ExpiringMembers.ToString();
		CheckInsLabel.Text = attendanceService.GetTodayCheckInCount().ToString();
		PaymentsDueLabel.Text = summary.ExpiredSubscriptions.ToString();
	}

	private void ClearForm()
	{
		NameEntry.Text = string.Empty;
		PhoneEntry.Text = string.Empty;
		PlanPicker.SelectedIndex = 0;
		StartDatePicker.Date = DateTime.Today;
	}
}
