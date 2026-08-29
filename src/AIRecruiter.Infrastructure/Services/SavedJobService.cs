using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.DTOs.SavedJobs;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class SavedJobService : ISavedJobService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public SavedJobService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task SaveAsync(int candidateUserId, int jobId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        var jobExists = await _db.JobPostings.AnyAsync(j => j.Id == jobId, ct);
        if (!jobExists)
        {
            throw new NotFoundException("Job posting not found.");
        }

        var alreadySaved = await _db.SavedJobs.AnyAsync(s => s.CandidateProfileId == profile.Id && s.JobPostingId == jobId, ct);
        if (alreadySaved)
        {
            // Idempotent: prevents duplicate saves at the API level, backed by the DB's
            // unique (CandidateProfileId, JobPostingId) index as a second line of defense.
            return;
        }

        _db.SavedJobs.Add(new SavedJob { CandidateProfileId = profile.Id, JobPostingId = jobId });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var stillDuplicate = await _db.SavedJobs.CountAsync(s => s.CandidateProfileId == profile.Id && s.JobPostingId == jobId, ct) > 0;
            if (stillDuplicate)
            {
                return;
            }
            throw;
        }

        await _auditLog.LogAsync(candidateUserId, "Candidate", "JobSaved", "JobPosting", jobId, null, ct);
    }

    public async Task UnsaveAsync(int candidateUserId, int jobId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        var saved = await _db.SavedJobs.FirstOrDefaultAsync(s => s.CandidateProfileId == profile.Id && s.JobPostingId == jobId, ct);
        if (saved is not null)
        {
            _db.SavedJobs.Remove(saved);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<JobPostingDto>> GetMySavedJobsAsync(int candidateUserId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        var jobs = await _db.SavedJobs
            .Include(s => s.JobPosting).ThenInclude(j => j.Company)
            .Where(s => s.CandidateProfileId == profile.Id)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => s.JobPosting)
            .ToListAsync(ct);

        return jobs.Select(JobPostingMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<SavedJobDto>> GetMySavedJobsWithDatesAsync(int candidateUserId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        var saved = await _db.SavedJobs
            .Include(s => s.JobPosting).ThenInclude(j => j.Company)
            .Where(s => s.CandidateProfileId == profile.Id)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);

        return saved.Select(s => new SavedJobDto(JobPostingMapper.ToDto(s.JobPosting), s.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<int>> GetMySavedJobIdsAsync(int candidateUserId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct);
        if (profile is null) return Array.Empty<int>();

        return await _db.SavedJobs.Where(s => s.CandidateProfileId == profile.Id).Select(s => s.JobPostingId).ToListAsync(ct);
    }

    private async Task<CandidateProfile> GetProfileAsync(int userId, CancellationToken ct)
    {
        return await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new NotFoundException("Complete your candidate profile first.");
    }

}
