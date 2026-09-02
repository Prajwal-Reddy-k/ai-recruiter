using AIRecruiter.Application.DTOs.CareerGoals;

namespace AIRecruiter.Application.Interfaces;

public interface ICareerGoalService
{
    Task<CareerGoalsSummaryDto> GetMyGoalsAsync(int userId, string? statusFilter, CancellationToken ct = default);
    Task<CareerGoalDto> CreateAsync(int userId, UpsertCareerGoalRequest request, CancellationToken ct = default);
    Task<CareerGoalDto> UpdateAsync(int userId, int goalId, UpsertCareerGoalRequest request, CancellationToken ct = default);
    Task DeleteAsync(int userId, int goalId, CancellationToken ct = default);
}
