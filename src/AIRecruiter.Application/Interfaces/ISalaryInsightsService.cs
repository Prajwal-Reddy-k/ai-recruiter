using AIRecruiter.Application.DTOs.SalaryInsights;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Pure local aggregation over disclosed job-posting salary ranges and accepted-offer
/// salaries — no external salary API, no paid data source. Below the sample-size privacy
/// threshold, a bucket is never shown with real numbers (see SalaryInsightDto.HasEnoughData).</summary>
public interface ISalaryInsightsService
{
    Task<IReadOnlyList<SalaryInsightDto>> GetInsightsAsync(SalaryInsightsQuery query, CancellationToken ct = default);

    /// <summary>Non-blocking advisory only — returns null when there isn't enough local data
    /// to compare against, or when the proposed range isn't a meaningful outlier.</summary>
    Task<string?> GetGuidanceForJobAsync(string title, string? city, string? state, bool isRemote, int? minExperienceYears, decimal? proposedMin, decimal? proposedMax, CancellationToken ct = default);
}
