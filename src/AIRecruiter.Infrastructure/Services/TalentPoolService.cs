using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.TalentPools;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Talent pools are a company-wide resource (like JobTemplate/JobAssignment) —
/// any recruiter or owner at the company can manage any pool, not just its creator.
/// Deliberately has no candidate-facing surface anywhere in this class.</summary>
public class TalentPoolService : ITalentPoolService
{
    private const int MaxNameLength = 100;

    private readonly AppDbContext _db;

    public TalentPoolService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<TalentPoolDto> CreatePoolAsync(int recruiterUserId, CreateTalentPoolRequest request, CancellationToken ct = default)
    {
        ValidateName(request.Name);
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        var pool = new TalentPool { CompanyId = companyId, CreatedByUserId = recruiterUserId, Name = request.Name.Trim() };
        _db.TalentPools.Add(pool);
        await _db.SaveChangesAsync(ct);

        return new TalentPoolDto(pool.Id, pool.Name, 0, pool.CreatedAt);
    }

    public async Task<TalentPoolDto> RenamePoolAsync(int recruiterUserId, int poolId, RenameTalentPoolRequest request, CancellationToken ct = default)
    {
        ValidateName(request.Name);
        var pool = await GetOwnedPoolAsync(recruiterUserId, poolId, ct);
        pool.Name = request.Name.Trim();
        pool.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var count = await _db.TalentPoolCandidates.CountAsync(c => c.TalentPoolId == poolId, ct);
        return new TalentPoolDto(pool.Id, pool.Name, count, pool.CreatedAt);
    }

    public async Task DeletePoolAsync(int recruiterUserId, int poolId, CancellationToken ct = default)
    {
        var pool = await GetOwnedPoolAsync(recruiterUserId, poolId, ct);
        _db.TalentPools.Remove(pool);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<TalentPoolDto>> GetPoolsAsync(int recruiterUserId, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        return await _db.TalentPools
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Name)
            .Select(p => new TalentPoolDto(p.Id, p.Name, p.Candidates.Count, p.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TalentPoolCandidateDto>> GetPoolCandidatesAsync(int recruiterUserId, int poolId, CancellationToken ct = default)
    {
        var pool = await GetOwnedPoolAsync(recruiterUserId, poolId, ct);

        var members = await _db.TalentPoolCandidates
            .Where(c => c.TalentPoolId == pool.Id)
            .Include(c => c.CandidateProfile).ThenInclude(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

        var candidateIds = members.Select(m => m.CandidateProfileId).ToList();

        var latestApplications = await _db.JobApplications
            .Where(a => candidateIds.Contains(a.CandidateProfileId) && a.JobPosting.CompanyId == pool.CompanyId)
            .Include(a => a.JobPosting)
            .GroupBy(a => a.CandidateProfileId)
            .Select(g => g.OrderByDescending(a => a.CreatedAt).First())
            .ToListAsync(ct);
        var latestByCandidate = latestApplications.ToDictionary(a => a.CandidateProfileId);

        return members.Select(m =>
        {
            var c = m.CandidateProfile;
            latestByCandidate.TryGetValue(m.CandidateProfileId, out var latestApp);
            return new TalentPoolCandidateDto(
                c.Id, c.User.FullName, c.Headline, c.SkillsCsv, c.TotalExperienceYears,
                IndiaLocationFormatter.Format(c.City, c.State, isRemote: false),
                AvatarUrlFormatter.Format(c.Id, c.AvatarStorageKey),
                latestApp?.JobPosting.Title, latestApp?.Status.ToString(),
                latestApp?.MatchScore.HasValue == true ? (int)latestApp.MatchScore!.Value : null,
                m.Notes, m.TagsCsv, m.CreatedAt);
        }).ToList();
    }

    public async Task AddCandidateAsync(int recruiterUserId, int poolId, AddCandidateToPoolRequest request, CancellationToken ct = default)
    {
        var pool = await GetOwnedPoolAsync(recruiterUserId, poolId, ct);

        var candidate = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.Id == request.CandidateProfileId, ct)
            ?? throw new NotFoundException("Candidate not found.");

        // Same privacy predicate InvitationService.InviteAsync already enforces — a pool
        // must never become a way to see a candidate who hasn't opted into visibility.
        var isVisible = candidate.ProfileVisibility == ProfileVisibility.VisibleToRecruiters
            || candidate.ProfileVisibility == ProfileVisibility.PublicShareable
            || await _db.JobApplications.AnyAsync(a => a.CandidateProfileId == candidate.Id && a.JobPosting.CompanyId == pool.CompanyId, ct);
        if (!isVisible)
        {
            throw new ForbiddenException("This candidate is not visible to you.");
        }

        var alreadyInPool = await _db.TalentPoolCandidates.AnyAsync(c => c.TalentPoolId == poolId && c.CandidateProfileId == request.CandidateProfileId, ct);
        if (alreadyInPool)
        {
            throw new ConflictException("ALREADY_IN_POOL", "This candidate is already in this pool.");
        }

        _db.TalentPoolCandidates.Add(new TalentPoolCandidate
        {
            TalentPoolId = poolId,
            CandidateProfileId = request.CandidateProfileId,
            AddedByUserId = recruiterUserId,
            Notes = request.Notes?.Trim(),
            TagsCsv = request.TagsCsv?.Trim(),
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var stillDuplicate = await _db.TalentPoolCandidates.AnyAsync(c => c.TalentPoolId == poolId && c.CandidateProfileId == request.CandidateProfileId, ct);
            if (stillDuplicate) throw new ConflictException("ALREADY_IN_POOL", "This candidate is already in this pool.");
            throw;
        }
    }

    public async Task RemoveCandidateAsync(int recruiterUserId, int poolId, int candidateProfileId, CancellationToken ct = default)
    {
        await GetOwnedPoolAsync(recruiterUserId, poolId, ct);

        var member = await _db.TalentPoolCandidates.FirstOrDefaultAsync(c => c.TalentPoolId == poolId && c.CandidateProfileId == candidateProfileId, ct)
            ?? throw new NotFoundException("This candidate is not in this pool.");

        _db.TalentPoolCandidates.Remove(member);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateCandidateNotesAsync(int recruiterUserId, int poolId, int candidateProfileId, UpdatePoolCandidateNotesRequest request, CancellationToken ct = default)
    {
        await GetOwnedPoolAsync(recruiterUserId, poolId, ct);

        var member = await _db.TalentPoolCandidates.FirstOrDefaultAsync(c => c.TalentPoolId == poolId && c.CandidateProfileId == candidateProfileId, ct)
            ?? throw new NotFoundException("This candidate is not in this pool.");

        member.Notes = request.Notes?.Trim();
        member.TagsCsv = request.TagsCsv?.Trim();
        member.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<int> GetCallerCompanyIdAsync(int recruiterUserId, CancellationToken ct)
    {
        var companyId = await _db.RecruiterProfiles
            .Where(r => r.UserId == recruiterUserId)
            .Select(r => (int?)r.CompanyId)
            .FirstOrDefaultAsync(ct);

        return companyId ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");
    }

    /// <summary>Loads a pool and verifies it belongs to the caller's own company — pools are
    /// company-shared, so any recruiter/owner at that company (not just the creator) passes.</summary>
    private async Task<TalentPool> GetOwnedPoolAsync(int recruiterUserId, int poolId, CancellationToken ct)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        var pool = await _db.TalentPools.FirstOrDefaultAsync(p => p.Id == poolId, ct)
            ?? throw new NotFoundException("Talent pool not found.");

        if (pool.CompanyId != companyId)
        {
            throw new ForbiddenException("You do not have access to this talent pool.");
        }

        return pool;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
        {
            throw new ValidationException("Please fix the highlighted fields.", new Dictionary<string, string>
            {
                ["name"] = $"Pool name is required (up to {MaxNameLength} characters).",
            });
        }
    }
}
