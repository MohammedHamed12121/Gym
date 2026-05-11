namespace GymManager;

using System.Collections.ObjectModel;

public partial class MainPage : ContentPage
{
	private readonly ObservableCollection<MemberRow> members = [];
	private readonly ObservableCollection<MemberRow> filteredMembers = [];

	public MainPage()
	{
		InitializeComponent();
		MembersCollection.ItemsSource = filteredMembers;
		PlanPicker.SelectedIndex = 0;
		StartDatePicker.Date = DateTime.Today;

		AddMember(new MemberRow("أحمد حسن", "01012345678", "شهري", DateTime.Today.AddDays(-8), 30));
		AddMember(new MemberRow("منى علي", "01176543210", "ربع سنوي", DateTime.Today.AddDays(-70), 90));
		AddMember(new MemberRow("عمر سمير", "01211112222", "شهري", DateTime.Today.AddDays(-28), 30));
		ApplyFilter();
	}

	private void OnAddMemberClicked(object sender, EventArgs e)
	{
		FormMessageLabel.Text = string.Empty;
		FormMessageLabel.TextColor = Color.FromArgb("#C2410C");

		var name = NameEntry.Text?.Trim() ?? string.Empty;
		var phone = PhoneEntry.Text?.Trim() ?? string.Empty;
		var plan = PlanPicker.SelectedItem?.ToString() ?? "شهري";

		if (string.IsNullOrWhiteSpace(name))
		{
			FormMessageLabel.Text = "اسم العضو مطلوب.";
			return;
		}

		if (string.IsNullOrWhiteSpace(phone))
		{
			FormMessageLabel.Text = "رقم الهاتف مطلوب.";
			return;
		}

		var durationDays = plan switch
		{
			"ربع سنوي" => 90,
			"سنوي" => 365,
			_ => 30
		};

		AddMember(new MemberRow(name, phone, plan, StartDatePicker.Date, durationDays));
		ApplyFilter();

		NameEntry.Text = string.Empty;
		PhoneEntry.Text = string.Empty;
		PlanPicker.SelectedIndex = 0;
		StartDatePicker.Date = DateTime.Today;
		FormMessageLabel.TextColor = Color.FromArgb("#22A06B");
		FormMessageLabel.Text = "تمت إضافة العضو.";
	}

	private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
	{
		ApplyFilter();
	}

	private void AddMember(MemberRow member)
	{
		members.Insert(0, member);
	}

	private void ApplyFilter()
	{
		var search = MemberSearchBar.Text?.Trim() ?? string.Empty;
		filteredMembers.Clear();

		foreach (var member in members.Where(member => Matches(member, search)))
		{
			filteredMembers.Add(member);
		}

		UpdateDashboard();
	}

	private static bool Matches(MemberRow member, string search)
	{
		return string.IsNullOrWhiteSpace(search)
			|| member.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
			|| member.Phone.Contains(search, StringComparison.OrdinalIgnoreCase)
			|| member.Plan.Contains(search, StringComparison.OrdinalIgnoreCase);
	}

	private void UpdateDashboard()
	{
		ActiveMembersLabel.Text = members.Count(member => member.EndDate >= DateTime.Today).ToString();
		ExpiringMembersLabel.Text = members.Count(member => member.EndDate >= DateTime.Today && member.EndDate <= DateTime.Today.AddDays(7)).ToString();
		CheckInsLabel.Text = "0";
		PaymentsDueLabel.Text = members.Count(member => member.EndDate < DateTime.Today).ToString();
	}

	private sealed class MemberRow
	{
		public MemberRow(string name, string phone, string plan, DateTime startDate, int durationDays)
		{
			Name = name;
			Phone = phone;
			Plan = plan;
			StartDate = startDate;
			EndDate = startDate.AddDays(durationDays);
		}

		public string Name { get; }

		public string Phone { get; }

		public string Plan { get; }

		public DateTime StartDate { get; }

		public DateTime EndDate { get; }

		public string EndDateText => $"ينتهي في {EndDate:dd MMMM yyyy}";

		public string Status => EndDate < DateTime.Today ? "منتهي" : EndDate <= DateTime.Today.AddDays(7) ? "تجديد قريب" : "نشط";

		public Color StatusColor => Status switch
		{
			"منتهي" => Color.FromArgb("#C2410C"),
			"تجديد قريب" => Color.FromArgb("#B7791F"),
			_ => Color.FromArgb("#22A06B")
		};
	}
}
