using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.SavedJobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Public-facing name is "Saved Search" (see frontend) — this entity/service started
/// as a simple job alert and was extended in place rather than duplicated into a parallel
/// concept, per the "advanced saved searches" feature spec.</summary>
public class JobAlertService : IJobAlertService
{
    private readonly AppDbContext _db;

    public JobAlertService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<JobAlertDto> CreateAsync(int candidateUserId, UpsertJobAlertRequest request, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct)
            ?? throw new NotFoundException("Complete your candidate profile first.");

        var existing = await _db.JobAlerts.Where(a => a.CandidateProfileId == profile.Id).ToListAsync(ct);
        if (existing.Any(a => IsDuplicateConfiguration(a, request)))
        {
            throw new ConflictException("DUPLICATE_SAVED_SEARCH", "You already have a saved search with this exact configuration.");
        }

        var alert = new JobAlert
        {
            CandidateProfileId = profile.Id,
            Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
            Keyword = string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword.Trim(),
            SkillsCsv = request.SkillsCsv,
            State = request.State,
            City = request.City,
            IsRemote = request.IsRemote,
            JobType = request.JobType,
            MinExperienceYears = request.MinExperienceYears,
            MinSalary = request.MinSalary,
            MaxSalary = request.MaxSalary,
            SortOption = string.IsNullOrWhiteSpace(request.SortOption) ? null : request.SortOption.Trim(),
            IsActive = request.IsActive,
        };
        _db.JobAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(alert, ct);
    }

    public async Task<JobAlertDto> UpdateAsync(int candidateUserId, int alertId, UpsertJobAlertRequest request, CancellationToken ct = default)
    {
        var alert = await GetOwnedAlertAsync(candidateUserId, alertId, ct);

        var others = await _db.JobAlerts.Where(a => a.CandidateProfileId == alert.CandidateProfileId && a.Id != alertId).ToListAsync(ct);
        if (others.Any(a => IsDuplicateConfiguration(a, request)))
        {
            throw new ConflictException("DUPLICATE_SAVED_SEARCH", "You already have a saved search with this exact configuration.");
        }

        alert.Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim();
        alert.Keyword = string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword.Trim();
        alert.SkillsCsv = request.SkillsCsv;
        alert.State = request.State;
        alert.City = request.City;
        alert.IsRemote = request.IsRemote;
        alert.JobType = request.JobType;
        alert.MinExperienceYears = request.MinExperienceYears;
        alert.MinSalary = request.MinSalary;
        alert.MaxSalary = request.MaxSalary;
        alert.SortOption = string.IsNullOrWhiteSpace(request.SortOption) ? null : request.SortOption.Trim();
        alert.IsActive = request.IsActive;
        alert.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(alert, ct);
    }

    public async Task<JobAlertDto> SetActiveAsync(int candidateUserId, int alertId, bool isActive, CancellationToken ct = default)
    {
        var alert = await GetOwnedAlertAsync(candidateUserId, alertId, ct);
        alert.IsActive = isActive;
        alert.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(alert, ct);
    }

    public async Task<IReadOnlyList<JobAlertDto>> GetMyAlertsAsync(int candidateUserId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct);
        if (profile is null) return Array.Empty<JobAlertDto>();

        var alerts = await _db.JobAlerts
            .Where(a => a.CandidateProfileId == profile.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var results = new List<JobAlertDto>();
        foreach (var alert in alerts)
        {
            results.Add(await ToDtoAsync(alert, ct));
        }
        return results;
    }

    public async Task DeleteAsync(int candidateUserId, int alertId, CancellationToken ct = default)
    {
        var alert = await GetOwnedAlertAsync(candidateUserId, alertId, ct);
        _db.JobAlerts.Remove(alert);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<JobAlertDto> DuplicateAsync(int candidateUserId, int alertId, CancellationToken ct = default)
    {
        var source = await GetOwnedAlertAsync(candidateUserId, alertId, ct);

        var copy = new JobAlert
        {
            CandidateProfileId = source.CandidateProfileId,
            Name = string.IsNullOrWhiteSpace(source.Name) ? "Copy" : $"{source.Name} (copy)",
            Keyword = source.Keyword,
            SkillsCsv = source.SkillsCsv,
            State = source.State,
            City = source.City,
            IsRemote = source.IsRemote,
            JobType = source.JobType,
            MinExperienceYears = source.MinExperienceYears,
            MinSalary = source.MinSalary,
            MaxSalary = source.MaxSalary,
            SortOption = source.SortOption,
            IsActive = source.IsActive,
            IsDefault = false,
        };
        _db.JobAlerts.Add(copy);
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(copy, ct);
    }

    public async Task<JobAlertDto> SetDefaultAsync(int candidateUserId, int alertId, CancellationToken ct = default)
    {
        var alert = await GetOwnedAlertAsync(candidateUserId, alertId, ct);

        var others = await _db.JobAlerts
            .Where(a => a.CandidateProfileId == alert.CandidateProfileId && a.Id != alertId && a.IsDefault)
            .ToListAsync(ct);
        foreach (var other in others)
        {
            other.IsDefault = false;
        }

        alert.IsDefault = true;
        alert.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(alert, ct);
    }

    public async Task<IReadOnlyList<JobPostingDto>> GetMatchingJobsAsync(int candidateUserId, int limit = 10, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct);
        if (profile is null) return Array.Empty<JobPostingDto>();

        var activeAlerts = await _db.JobAlerts
            .Where(a => a.CandidateProfileId == profile.Id && a.IsActive)
            .ToListAsync(ct);

        // Prefer the candidate's default saved search when one exists — falls back to "any
        // active alert" (today's behavior) when none is marked default.
        var defaultAlert = activeAlerts.FirstOrDefault(a => a.IsDefault);
        var alertsToMatch = defaultAlert is not null ? new List<JobAlert> { defaultAlert } : activeAlerts;

        if (alertsToMatch.Count == 0) return Array.Empty<JobPostingDto>();

        var matchedJobs = new Dictionary<int, JobPosting>();
        foreach (var alert in alertsToMatch)
        {
            var matches = (await MatchesForAlertAsync(alert, ct)).Take(limit);
            foreach (var job in matches)
            {
                matchedJobs.TryAdd(job.Id, job);
            }
        }

        return matchedJobs.Values
            .OrderByDescending(j => j.CreatedAt)
            .Take(limit)
            .Select(j => JobPostingMapper.ToDto(j))
            .ToList();
    }

    private async Task<JobAlert> GetOwnedAlertAsync(int candidateUserId, int alertId, CancellationToken ct)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct)
            ?? throw new NotFoundException("Complete your candidate profile first.");

        var alert = await _db.JobAlerts.FirstOrDefaultAsync(a => a.Id == alertId, ct)
            ?? throw new NotFoundException("Alert not found.");

        if (alert.CandidateProfileId != profile.Id)
        {
            throw new ForbiddenException("You do not have access to this alert.");
        }

        return alert;
    }

    private static bool IsDuplicateConfiguration(JobAlert a, UpsertJobAlertRequest r) =>
        string.Equals(a.Keyword, NormalizeOrNull(r.Keyword), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.SkillsCsv, r.SkillsCsv, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.State, r.State, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a.City, r.City, StringComparison.OrdinalIgnoreCase) &&
        a.IsRemote == r.IsRemote &&
        a.JobType == r.JobType &&
        a.MinExperienceYears == r.MinExperienceYears &&
        a.MinSalary == r.MinSalary &&
        a.MaxSalary == r.MaxSalary &&
        string.Equals(a.SortOption, NormalizeOrNull(r.SortOption), StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Live-computed matching — no background job/scheduler needed. Filters
    /// DB-translatable predicates first, then applies the shared JobAlertMatcher predicate
    /// in-memory (the same one JobPostingService's publish-notify hook uses in the opposite
    /// direction) so both directions can never drift apart.</summary>
    private async Task<List<JobPosting>> MatchesForAlertAsync(JobAlert alert, CancellationToken ct)
    {
        var query = _db.JobPostings.Where(j => j.Status == JobStatus.Open && j.ModerationStatus == ModerationStatus.Approved);

        if (!string.IsNullOrWhiteSpace(alert.City)) query = query.Where(j => j.City == alert.City);
        if (!string.IsNullOrWhiteSpace(alert.State)) query = query.Where(j => j.State == alert.State);
        if (alert.IsRemote.HasValue) query = query.Where(j => j.IsRemote == alert.IsRemote.Value);
        if (alert.JobType.HasValue) query = query.Where(j => j.JobType == alert.JobType.Value);

        var jobs = await query.Include(j => j.Company).OrderByDescending(j => j.CreatedAt).ToListAsync(ct);

        return jobs.Where(j => JobAlertMatcher.Matches(alert, j)).ToList();
    }

    private async Task<JobAlertDto> ToDtoAsync(JobAlert a, CancellationToken ct)
    {
        var allMatches = a.IsActive ? await MatchesForAlertAsync(a, ct) : new List<JobPosting>();

        return new(
            a.Id, a.SkillsCsv, a.State, a.City, a.IsRemote, a.JobType?.ToString(), a.MinExperienceYears,
            a.IsActive, allMatches.Count, allMatches.Take(3).Select(j => JobPostingMapper.ToDto(j)).ToList(), a.CreatedAt,
            a.Name, a.Keyword, a.MinSalary, a.MaxSalary, a.SortOption, a.IsDefault);
    }
}
