namespace GymManager.Business.Models;

using GymManager.Core.Enums;

public sealed record UpdatePlanRequest(int Id, string Name, string Description, decimal Price, Period Period, int PlanTypeId);
