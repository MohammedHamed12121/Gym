namespace GymManager.Business.Models;

using GymManager.Core.Enums;

public sealed record AddPlanRequest(string Name, string Description, decimal Price, Period Period, int PlanTypeId);
