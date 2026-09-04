using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Candidate-follows-a-company, mirroring SavedJobService's exact idempotent-save
/// pattern. No method here ever returns a company's follower list — that's enforced by
/// omission, matching the talent-pool candidate-invisibility guarantee from a prior batch.</summary>
public class FollowService : IFollowService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public FollowService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task FollowAsync(int candidateUserId, int companyId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        var companyExists = await _db.Companies.AnyAsync(c => c.Id == companyId, ct);
        if (!companyExists)
        {
            throw new NotFoundException("Company not found.");
        }

        var alreadyFollowing = await _db.CompanyFollows.AnyAsync(f => f.CandidateProfileId == profile.Id && f.CompanyId == companyId, ct);
        if (alreadyFollowing)
        {
            return;
        }

        _db.CompanyFollows.Add(new CompanyFollow { CandidateProfileId = profile.Id, CompanyId = companyId });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var stillDuplicate = await _db.CompanyFollows.CountAsync(f => f.CandidateProfileId == profile.Id && f.CompanyId == companyId, ct) > 0;
            if (stillDuplicate) return;
            throw;
        }

        await _auditLog.LogAsync(candidateUserId, "Candidate", "CompanyFollowed", "Company", companyId, null, ct);
    }

    public async Task UnfollowAsync(int candidateUserId, int companyId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        var follow = await _db.CompanyFollows.FirstOrDefaultAsync(f => f.CandidateProfileId == profile.Id && f.CompanyId == companyId, ct);
        if (follow is not null)
        {
            _db.CompanyFollows.Remove(follow);
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task<IReadOnlyList<FollowedCompanyDto>> GetMyFollowedCompaniesAsync(int candidateUserId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        return await _db.CompanyFollows
            .Include(f => f.Company)
            .Where(f => f.CandidateProfileId == profile.Id)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FollowedCompanyDto(f.CompanyId, f.Company.Name, f.Company.LogoUrl, f.Company.Industry, f.NotifyOnNewJob, f.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task UpdateNotifyPreferenceAsync(int candidateUserId, int companyId, UpdateFollowNotifyRequest request, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(candidateUserId, ct);

        var follow = await _db.CompanyFollows.FirstOrDefaultAsync(f => f.CandidateProfileId == profile.Id && f.CompanyId == companyId, ct)
            ?? throw new NotFoundException("You are not following this company.");

        follow.NotifyOnNewJob = request.NotifyOnNewJob;
        follow.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CandidateProfile> GetProfileAsync(int userId, CancellationToken ct)
    {
        return await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new NotFoundException("Complete your candidate profile first.");
    }
}
