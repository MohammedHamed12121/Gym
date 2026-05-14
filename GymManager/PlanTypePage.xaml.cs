namespace GymManager.MauiView;

using System.Collections.ObjectModel;
using GymManager.Business.Models;
using GymManager.Business.Services;
using GymManager.Core.Enums;

public partial class PlanTypePage : ContentPage
{
	private readonly PlanTypeService planTypeService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly ObservableCollection<PlanTypeListItem> planTypeItems = [];

	public PlanTypePage()
	{
		InitializeComponent();
		PlanTypesCollection.ItemsSource = planTypeItems;
		RefreshPlanTypes();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		RefreshPlanTypes();
	}

	private void OnAddPlanTypeClicked(object sender, EventArgs e)
	{
		FormMessageLabel.Text = string.Empty;
		FormMessageLabel.TextColor = Color.FromArgb("#C2410C");

		var request = new AddPlanTypeRequest(
			Name: PlanTypeNameEntry.Text?.Trim() ?? string.Empty);

		if (!planTypeService.IsValidAddPlanTypeRequest(request, out var validationMessage))
		{
			FormMessageLabel.Text = validationMessage;
			return;
		}

		planTypeService.AddPlanType(request);
		RefreshPlanTypes();
		PlanTypeNameEntry.Text = string.Empty;

		FormMessageLabel.TextColor = Color.FromArgb("#22A06B");
		FormMessageLabel.Text = "تمت إضافة نوع الخطة.";
	}

	private void OnToggleStatusClicked(object sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not PlanTypeListItem item)
		{
			return;
		}

		var newStatus = item.IsActive ? PlanTypeStatus.NotActive : PlanTypeStatus.Active;
		planTypeService.SetPlanTypeStatus(item.Id, newStatus);
		RefreshPlanTypes();
	}

	private async void OnDeletePlanTypeClicked(object sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not PlanTypeListItem item)
		{
			return;
		}

		var confirmed = await DisplayAlert("تأكيد الحذف", $"هل أنت متأكد من حذف نوع الخطة \"{item.Name}\"؟", "نعم", "إلغاء");
		if (!confirmed)
		{
			return;
		}

		planTypeService.DeletePlanType(item.Id);
		RefreshPlanTypes();
	}

	private void RefreshPlanTypes()
	{
		planTypeItems.Clear();
		foreach (var item in planTypeService.GetAllPlanTypes())
		{
			planTypeItems.Add(item);
		}
	}
}
