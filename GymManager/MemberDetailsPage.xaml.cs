namespace GymManager.MauiView;

using System.Collections.ObjectModel;
using GymManager.Business.Services;
using GymManager.Core.Models;

[QueryProperty(nameof(MemberId), "memberId")]
public partial class MemberDetailsPage : ContentPage
{
	private static readonly string[] ArabicMonths =
		["الكل", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو", "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];

	private readonly MemberDirectoryService memberDirectoryService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly AttendanceService attendanceService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly ObservableCollection<Attendance> attendanceHistory = [];

	private int memberId;

	public MemberDetailsPage()
	{
		InitializeComponent();
		AttendanceHistoryCollection.ItemsSource = attendanceHistory;
	}

	public string MemberId
	{
		set
		{
			if (int.TryParse(value, out var parsedMemberId))
			{
				memberId = parsedMemberId;
				LoadMember();
				InitializeFilters();
				LoadAttendanceHistory();
			}
		}
	}

	private void LoadMember()
	{
		var details = memberDirectoryService.GetMemberDetails(memberId);
		BindingContext = details;
		DetailsPanel.IsVisible = details is not null;
		NotFoundPanel.IsVisible = details is null;
		AttendancePanel.IsVisible = details is not null;

		if (details is not null)
		{
			var totalDays = attendanceService.GetMemberAttendanceCount(memberId, null, null, null);
			TotalAttendanceLabel.Text = $"إجمالي أيام الحضور: {totalDays}";
		}
	}

	private void InitializeFilters()
	{
		var currentYear = DateTime.Now.Year;

		YearPicker.Items.Clear();
		YearPicker.Items.Add("الكل");
		for (var y = currentYear - 2; y <= currentYear + 1; y++)
		{
			YearPicker.Items.Add(y.ToString());
		}
		YearPicker.SelectedIndex = 0;

		MonthPicker.Items.Clear();
		foreach (var month in ArabicMonths)
		{
			MonthPicker.Items.Add(month);
		}
		MonthPicker.SelectedIndex = 0;

		DayPicker.Items.Clear();
		DayPicker.Items.Add("الكل");
		for (var d = 1; d <= 31; d++)
		{
			DayPicker.Items.Add(d.ToString());
		}
		DayPicker.SelectedIndex = 0;
	}

	private void OnFilterChanged(object sender, EventArgs e)
	{
		LoadAttendanceHistory();
	}

	private void LoadAttendanceHistory()
	{
		var year = GetFilterValue(YearPicker);
		var month = GetFilterValue(MonthPicker);
		var day = GetFilterValue(DayPicker);

		var records = attendanceService.GetMemberAttendanceFiltered(memberId, year, month, day, 20);

		attendanceHistory.Clear();
		foreach (var record in records)
		{
			attendanceHistory.Add(record);
		}
	}

	private void OnClearFiltersClicked(object sender, EventArgs e)
	{
		YearPicker.SelectedIndex = 0;
		MonthPicker.SelectedIndex = 0;
		DayPicker.SelectedIndex = 0;
	}

	private int? GetFilterValue(Picker picker)
	{
		if (picker.SelectedIndex <= 0)
		{
			return null;
		}

		var text = picker.Items[picker.SelectedIndex];

		if (picker == MonthPicker)
		{
			return Array.IndexOf(ArabicMonths, text);
		}

		return int.TryParse(text, out var value) ? value : null;
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
