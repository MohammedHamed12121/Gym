namespace GymManager.MauiView;

using System.Collections.ObjectModel;
using GymManager.Business.Services;
using GymManager.Core.Models;

public partial class AttendancePage : ContentPage
{
	private readonly AttendanceService attendanceService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly ObservableCollection<Attendance> attendanceRecords = [];

	public AttendancePage()
	{
		InitializeComponent();
		AttendanceCollection.ItemsSource = attendanceRecords;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		LoadAttendance();
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		LoadAttendance();
	}

	private void LoadAttendance()
	{
		var summary = attendanceService.GetAttendanceSummary();
		TodayCountLabel.Text = summary.TodayCount.ToString();
		MonthCountLabel.Text = summary.MonthCount.ToString();

		attendanceRecords.Clear();

		foreach (var record in attendanceService.SearchAttendance(AttendanceSearchBar.Text))
		{
			attendanceRecords.Add(record);
		}
	}
}
