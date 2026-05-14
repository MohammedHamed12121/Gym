namespace GymManager.Core.Models;

using GymManager.Core.Enums;

public sealed class Plan
{
	public int Id { get; set; }

	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public decimal Price { get; set; }

	public Period Period { get; set; }

	public int PlanTypeId { get; set; }

	public PlanStatus Status { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.Now;
}
