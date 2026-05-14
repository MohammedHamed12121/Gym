namespace GymManager.Business.Models;

public sealed class PlanListItem
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string PriceText { get; set; } = string.Empty;

	public string PeriodName { get; set; } = string.Empty;

	public string PlanTypeName { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string StatusColorHex { get; set; } = string.Empty;

	public bool IsActive { get; set; }
}
