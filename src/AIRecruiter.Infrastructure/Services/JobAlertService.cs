using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.SavedJobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

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

        var alert = new JobAlert
        {
            CandidateProfileId = profile.Id,
            SkillsCsv = request.SkillsCsv,
            State = request.State,
            City = request.City,
            IsRemote = request.IsRemote,
            JobType = request.JobType,
            MinExperienceYears = request.MinExperienceYears,
            IsActive = request.IsActive,
        };
        _db.JobAlerts.Add(alert);
        await _db.SaveChangesAsync(ct);

        return await ToDtoAsync(alert, ct);
    }

    public async Task<JobAlertDto> UpdateAsync(int candidateUserId, int alertId, UpsertJobAlertRequest request, CancellationToken ct = default)
    {
        var alert = await GetOwnedAlertAsync(candidateUserId, alertId, ct);

        alert.SkillsCsv = request.SkillsCsv;
        alert.State = request.State;
        alert.City = request.City;
        alert.IsRemote = request.IsRemote;
        alert.JobType = request.JobType;
        alert.MinExperienceYears = request.MinExperienceYears;
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

    public async Task<IReadOnlyList<JobPostingDto>> GetMatchingJobsAsync(int candidateUserId, int limit = 10, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct);
        if (profile is null) return Array.Empty<JobPostingDto>();

        var activeAlerts = await _db.JobAlerts
            .Where(a => a.CandidateProfileId == profile.Id && a.IsActive)
            .ToListAsync(ct);

        if (activeAlerts.Count == 0) return Array.Empty<JobPostingDto>();

        var matchedJobs = new Dictionary<int, JobPosting>();
        foreach (var alert in activeAlerts)
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
            .Select(JobPostingMapper.ToDto)
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

    /// <summary>Live-computed matching — no background job/scheduler needed. The skills
    /// substring match is applied after materializing the DB-translatable filters, since
    /// "any of these skills is a substring of this job's CSV" isn't reliably translatable
    /// across providers.</summary>
    private async Task<List<JobPosting>> MatchesForAlertAsync(JobAlert alert, CancellationToken ct)
    {
        var query = _db.JobPostings.Where(j => j.Status == JobStatus.Open && j.ModerationStatus == ModerationStatus.Approved);

        if (!string.IsNullOrWhiteSpace(alert.City)) query = query.Where(j => j.City == alert.City);
        if (!string.IsNullOrWhiteSpace(alert.State)) query = query.Where(j => j.State == alert.State);
        if (alert.IsRemote.HasValue) query = query.Where(j => j.IsRemote == alert.IsRemote.Value);
        if (alert.JobType.HasValue) query = query.Where(j => j.JobType == alert.JobType.Value);
        if (alert.MinExperienceYears.HasValue)
        {
            query = query.Where(j => j.MinExperienceYears == null || j.MinExperienceYears <= alert.MinExperienceYears.Value);
        }

        var jobs = await query.Include(j => j.Company).OrderByDescending(j => j.CreatedAt).ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(alert.SkillsCsv))
        {
            var skills = alert.SkillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            jobs = jobs.Where(j => j.RequiredSkillsCsv != null && skills.Any(s => j.RequiredSkillsCsv.Contains(s, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        return jobs;
    }

    private async Task<JobAlertDto> ToDtoAsync(JobAlert a, CancellationToken ct)
    {
        var allMatches = a.IsActive ? await MatchesForAlertAsync(a, ct) : new List<JobPosting>();

        return new(
            a.Id, a.SkillsCsv, a.State, a.City, a.IsRemote, a.JobType?.ToString(), a.MinExperienceYears,
            a.IsActive, allMatches.Count, allMatches.Take(3).Select(JobPostingMapper.ToDto).ToList(), a.CreatedAt);
    }
}
