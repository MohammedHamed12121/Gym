namespace GymManager.Core.Models;

using GymManager.Core.Enums;

public sealed class PlanType
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public PlanTypeStatus Status { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
