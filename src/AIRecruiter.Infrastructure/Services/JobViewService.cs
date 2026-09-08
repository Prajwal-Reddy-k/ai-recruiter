using AIRecruiter.Application.DTOs.Jobs;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class JobViewService : IJobViewService
{
    private const int MaxHistoryItems = 10;

    private readonly AppDbContext _db;

    public JobViewService(AppDbContext db)
    {
        _db = db;
    }

    public async Task RecordViewAsync(int candidateUserId, int jobPostingId, CancellationToken ct = default)
    {
        var profileId = await _db.CandidateProfiles
            .Where(c => c.UserId == candidateUserId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);
        if (profileId is null) return;

        var jobExists = await _db.JobPostings.AnyAsync(j => j.Id == jobPostingId, ct);
        if (!jobExists) return;

        var existing = await _db.JobViews
            .FirstOrDefaultAsync(v => v.CandidateProfileId == profileId.Value && v.JobPostingId == jobPostingId, ct);

        if (existing is not null)
        {
            existing.ViewedAt = DateTime.UtcNow;
        }
        else
        {
            _db.JobViews.Add(new Domain.Entities.JobView
            {
                CandidateProfileId = profileId.Value,
                JobPostingId = jobPostingId,
                ViewedAt = DateTime.UtcNow,
            });
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<JobPostingDto>> GetRecentlyViewedAsync(int candidateUserId, CancellationToken ct = default)
    {
        var profileId = await _db.CandidateProfiles
            .Where(c => c.UserId == candidateUserId)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(ct);
        if (profileId is null) return Array.Empty<JobPostingDto>();

        var jobs = await _db.JobViews
            .Where(v => v.CandidateProfileId == profileId.Value)
            .OrderByDescending(v => v.ViewedAt)
            .Take(MaxHistoryItems)
            .Include(v => v.JobPosting).ThenInclude(j => j.Company)
            .Include(v => v.JobPosting).ThenInclude(j => j.ScreeningQuestions).ThenInclude(q => q.Options)
            .Select(v => v.JobPosting)
            .ToListAsync(ct);

        return jobs.Select(j => JobPostingMapper.ToDto(j)).ToList();
    }
}
