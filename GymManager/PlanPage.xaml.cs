namespace GymManager.MauiView;

using System.Collections.ObjectModel;
using GymManager.Business.Models;
using GymManager.Business.Services;
using GymManager.Core.Enums;

public partial class PlanPage : ContentPage
{
	private readonly PlanService planService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly PlanTypeService planTypeService = new(
		Path.Combine(FileSystem.AppDataDirectory, "gym-manager.db"));
	private readonly ObservableCollection<PlanListItem> planItems = [];

	private int? editingPlanId;

	public PlanPage()
	{
		InitializeComponent();
		PlansCollection.ItemsSource = planItems;
		LoadPeriods();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		LoadPlanTypes();
		RefreshPlans();
	}

	private void LoadPeriods()
	{
		PeriodPicker.ItemsSource = new List<PeriodItem>
		{
			new((int)Period.Daily, "يومي"),
			new((int)Period.Monthly, "شهري"),
			new((int)Period.Quarter, "ربع سنوي"),
			new((int)Period.Year, "سنوي")
		};
		PeriodPicker.ItemDisplayBinding = new Microsoft.Maui.Controls.Binding("Name");
		PeriodPicker.SelectedIndex = 1;
	}

	private void OnSavePlanClicked(object sender, EventArgs e)
	{
		FormMessageLabel.Text = string.Empty;
		FormMessageLabel.TextColor = Color.FromArgb("#C2410C");

		if (!decimal.TryParse(PlanPriceEntry.Text?.Trim(), out var price))
		{
			FormMessageLabel.Text = "السعر يجب أن يكون رقماً صحيحاً.";
			return;
		}

		var selectedPlanTypeId = PlanTypePicker.SelectedIndex >= 0
			? (int)(PlanTypePicker.SelectedItem as PlanTypeItem)!.Id
			: 0;

		var selectedPeriod = PeriodPicker.SelectedIndex >= 0
			? (Period)(PeriodPicker.SelectedItem as PeriodItem)!.Id
			: Period.Monthly;

		if (editingPlanId.HasValue)
		{
			var request = new UpdatePlanRequest(
				Id: editingPlanId.Value,
				Name: PlanNameEntry.Text?.Trim() ?? string.Empty,
				Description: PlanDescriptionEditor.Text?.Trim() ?? string.Empty,
				Price: price,
				Period: selectedPeriod,
				PlanTypeId: selectedPlanTypeId);

			if (!planService.IsValidUpdatePlanRequest(request, out var validationMessage))
			{
				FormMessageLabel.Text = validationMessage;
				return;
			}

			planService.UpdatePlan(request);
			ClearForm();
			RefreshPlans();

			FormMessageLabel.TextColor = Color.FromArgb("#22A06B");
			FormMessageLabel.Text = "تم تحديث الخطة.";
		}
		else
		{
			var request = new AddPlanRequest(
				Name: PlanNameEntry.Text?.Trim() ?? string.Empty,
				Description: PlanDescriptionEditor.Text?.Trim() ?? string.Empty,
				Price: price,
				Period: selectedPeriod,
				PlanTypeId: selectedPlanTypeId);

			if (!planService.IsValidAddPlanRequest(request, out var validationMessage))
			{
				FormMessageLabel.Text = validationMessage;
				return;
			}

			planService.AddPlan(request);
			ClearForm();
			RefreshPlans();

			FormMessageLabel.TextColor = Color.FromArgb("#22A06B");
			FormMessageLabel.Text = "تمت إضافة الخطة.";
		}
	}

	private void OnEditPlanClicked(object sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not PlanListItem item)
		{
			return;
		}

		var plan = planService.GetPlanById(item.Id);
		if (plan is null)
		{
			return;
		}

		editingPlanId = plan.Id;
		PlanNameEntry.Text = plan.Name;
		PlanDescriptionEditor.Text = plan.Description;
		PlanPriceEntry.Text = plan.Price.ToString("F2");

		for (var i = 0; i < PlanTypePicker.Items.Count; i++)
		{
			if (PlanTypePicker.ItemsSource is List<PlanTypeItem> items && items[i].Id == plan.PlanTypeId)
			{
				PlanTypePicker.SelectedIndex = i;
				break;
			}
		}

		for (var i = 0; i < PeriodPicker.Items.Count; i++)
		{
			if (PeriodPicker.ItemsSource is List<PeriodItem> items && items[i].Id == (int)plan.Period)
			{
				PeriodPicker.SelectedIndex = i;
				break;
			}
		}

		FormTitle.Text = "تعديل الخطة";
		SaveButton.Text = "حفظ التعديلات";
		CancelButton.IsVisible = true;
		FormMessageLabel.Text = string.Empty;
	}

	private void OnDuplicatePlanClicked(object sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not PlanListItem item)
		{
			return;
		}

		var plan = planService.GetPlanById(item.Id);
		if (plan is null)
		{
			return;
		}

		editingPlanId = null;
		PlanNameEntry.Text = plan.Name;
		PlanDescriptionEditor.Text = plan.Description;
		PlanPriceEntry.Text = plan.Price.ToString("F2");

		for (var i = 0; i < PlanTypePicker.Items.Count; i++)
		{
			if (PlanTypePicker.ItemsSource is List<PlanTypeItem> items && items[i].Id == plan.PlanTypeId)
			{
				PlanTypePicker.SelectedIndex = i;
				break;
			}
		}

		for (var i = 0; i < PeriodPicker.Items.Count; i++)
		{
			if (PeriodPicker.ItemsSource is List<PeriodItem> items && items[i].Id == (int)plan.Period)
			{
				PeriodPicker.SelectedIndex = i;
				break;
			}
		}

		FormTitle.Text = "إضافة خطة (نسخ)";
		SaveButton.Text = "إضافة";
		CancelButton.IsVisible = false;
		FormMessageLabel.Text = string.Empty;
	}

	private void OnCancelEditClicked(object sender, EventArgs e)
	{
		ClearForm();
	}

	private async void OnDeletePlanClicked(object sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not PlanListItem item)
		{
			return;
		}

		var confirmed = await DisplayAlert("تأكيد الحذف", $"هل أنت متأكد من حذف الخطة \"{item.Name}\"؟", "نعم", "إلغاء");
		if (!confirmed)
		{
			return;
		}

		planService.DeletePlan(item.Id);
		RefreshPlans();
	}

	private void OnToggleStatusClicked(object sender, EventArgs e)
	{
		if (sender is not Button button || button.BindingContext is not PlanListItem item)
		{
			return;
		}

		var newStatus = item.IsActive ? PlanStatus.NotActive : PlanStatus.Active;
		planService.SetPlanStatus(item.Id, newStatus);
		RefreshPlans();
	}

	private void LoadPlanTypes()
	{
		var planTypes = planTypeService.GetAllPlanTypes();
		var items = planTypes.Select(pt => new PlanTypeItem(pt.Id, pt.Name)).ToList();
		PlanTypePicker.ItemsSource = items;
		PlanTypePicker.ItemDisplayBinding = new Microsoft.Maui.Controls.Binding("Name");
		if (PlanTypePicker.Items.Count > 0)
		{
			PlanTypePicker.SelectedIndex = 0;
		}
	}

	private void RefreshPlans()
	{
		planItems.Clear();
		foreach (var item in planService.GetAllPlans())
		{
			planItems.Add(item);
		}
	}

	private void ClearForm()
	{
		editingPlanId = null;
		PlanNameEntry.Text = string.Empty;
		PlanDescriptionEditor.Text = string.Empty;
		PlanPriceEntry.Text = string.Empty;
		if (PlanTypePicker.Items.Count > 0)
		{
			PlanTypePicker.SelectedIndex = 0;
		}
		if (PeriodPicker.Items.Count > 0)
		{
			PeriodPicker.SelectedIndex = 1;
		}
		FormTitle.Text = "إضافة خطة";
		SaveButton.Text = "إضافة";
		CancelButton.IsVisible = false;
		FormMessageLabel.Text = string.Empty;
	}

	private sealed record PlanTypeItem(int Id, string Name);

	private sealed record PeriodItem(int Id, string Name);
}
