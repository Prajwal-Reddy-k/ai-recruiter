using AIRecruiter.Application.DTOs.SalaryInsights;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Zero-budget local aggregation — no paid salary API. Pools two data sources: open
/// job postings with a disclosed salary range (using the midpoint) and accepted offers (the
/// actual agreed salary, annualized). A bucket below MinSampleSize never returns real numbers —
/// see SalaryInsightDto.HasEnoughData.</summary>
public class SalaryInsightsService : ISalaryInsightsService
{
    private const int MinSampleSize = 5;

    /// <summary>How far below the local median a proposed minimum can be, or above the local
    /// max a proposed maximum can be, before advisory guidance is shown. Non-blocking.</summary>
    private const decimal LowOutlierFactor = 0.7m;
    private const decimal HighOutlierFactor = 1.5m;

    private readonly AppDbContext _db;

    public SalaryInsightsService(AppDbContext db)
    {
        _db = db;
    }

    private record DataPoint(string Title, int? MinExperienceYears, string? City, string? State, bool IsRemote, decimal Salary);

    private async Task<List<DataPoint>> CollectDataPointsAsync(CancellationToken ct)
    {
        var jobPoints = await _db.JobPostings
            .Where(j => j.Status != JobStatus.Draft && j.MinSalary != null && j.MaxSalary != null)
            .Select(j => new DataPoint(j.Title, j.MinExperienceYears, j.City, j.State, j.IsRemote, (j.MinSalary!.Value + j.MaxSalary!.Value) / 2))
            .ToListAsync(ct);

        var offerPoints = await _db.Offers
            .Where(o => o.Status == OfferStatus.Accepted)
            .Select(o => new
            {
                o.OfferedSalary,
                o.SalaryType,
                o.WorkCity,
                o.WorkState,
                o.IsRemote,
                Title = o.JobApplication.JobPosting.Title,
                MinExperienceYears = o.JobApplication.JobPosting.MinExperienceYears,
            })
            .ToListAsync(ct);

        var normalizedOfferPoints = offerPoints.Select(o => new DataPoint(
            o.Title, o.MinExperienceYears, o.WorkCity, o.WorkState, o.IsRemote,
            o.SalaryType == SalaryType.Monthly ? o.OfferedSalary * 12 : o.OfferedSalary));

        return jobPoints.Concat(normalizedOfferPoints).ToList();
    }

    public async Task<IReadOnlyList<SalaryInsightDto>> GetInsightsAsync(SalaryInsightsQuery query, CancellationToken ct = default)
    {
        var points = await CollectDataPointsAsync(ct);

        var grouped = points
            .Select(p => new { Point = p, Band = SalaryExperienceBand.From(p.MinExperienceYears) })
            .GroupBy(x => (x.Point.Title, x.Band, x.Point.City, x.Point.State, x.Point.IsRemote))
            .Select(g => ToDto(g.Key.Title, g.Key.Band, g.Key.City, g.Key.State, g.Key.IsRemote, g.Select(x => x.Point.Salary).ToList()))
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            grouped = grouped.Where(r => r.RoleTitle.Contains(query.Role, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(query.City))
        {
            grouped = grouped.Where(r => string.Equals(r.City, query.City, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(query.State))
        {
            grouped = grouped.Where(r => string.Equals(r.State, query.State, StringComparison.OrdinalIgnoreCase));
        }
        if (query.IsRemote.HasValue)
        {
            grouped = grouped.Where(r => r.IsRemote == query.IsRemote.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.ExperienceBand))
        {
            grouped = grouped.Where(r => string.Equals(r.ExperienceBand, query.ExperienceBand, StringComparison.OrdinalIgnoreCase));
        }

        return grouped.OrderByDescending(r => r.SampleCount).ToList();
    }

    public async Task<string?> GetGuidanceForJobAsync(string title, string? city, string? state, bool isRemote, int? minExperienceYears, decimal? proposedMin, decimal? proposedMax, CancellationToken ct = default)
    {
        if (!proposedMin.HasValue && !proposedMax.HasValue) return null;

        var points = await CollectDataPointsAsync(ct);
        var band = SalaryExperienceBand.From(minExperienceYears);

        var relevant = points
            .Where(p => string.Equals(p.Title, title, StringComparison.OrdinalIgnoreCase) && SalaryExperienceBand.From(p.MinExperienceYears) == band)
            .Where(p => isRemote ? p.IsRemote : string.Equals(p.City, city, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Salary)
            .ToList();

        if (relevant.Count < MinSampleSize) return null;

        var median = Median(relevant);
        var max = relevant.Max();

        if (proposedMin.HasValue && proposedMin.Value < median * LowOutlierFactor)
        {
            return $"This range starts notably below the local average (~₹{median:N0}/year) for similar roles in this area — this is guidance only, not a restriction.";
        }
        if (proposedMax.HasValue && proposedMax.Value > max * HighOutlierFactor)
        {
            return $"This range goes notably above what's typically seen locally (~₹{max:N0}/year) for similar roles — this is guidance only, not a restriction.";
        }
        return null;
    }

    private static SalaryInsightDto ToDto(string title, string band, string? city, string? state, bool isRemote, List<decimal> salaries)
    {
        var hasEnoughData = salaries.Count >= MinSampleSize;
        return new SalaryInsightDto(
            title, band, city, state, isRemote,
            hasEnoughData ? salaries.Min() : null,
            hasEnoughData ? Median(salaries) : null,
            hasEnoughData ? salaries.Max() : null,
            salaries.Count,
            hasEnoughData);
    }

    private static decimal Median(List<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
    }
}
