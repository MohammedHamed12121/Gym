namespace GymManager.Business.Models;

public sealed class PlanTypeListItem
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Status { get; set; } = string.Empty;

	public string StatusColorHex { get; set; } = string.Empty;

	public bool IsActive { get; set; }
}
