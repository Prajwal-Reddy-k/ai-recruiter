using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Assessments;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.DTOs.PublicProfile;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class PublicProfileService : IPublicProfileService
{
    private readonly AppDbContext _db;

    public PublicProfileService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PublicProfilePreviewDto> GetMyPreviewAsync(int userId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles
            .Include(c => c.User)
            .Include(c => c.Projects)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new NotFoundException("Complete your candidate profile first.");

        var badges = await GetBadgesAsync(profile.Id, ct);
        var isPublic = profile.ProfileVisibility == ProfileVisibility.PublicShareable;
        var publicUrl = isPublic && profile.PublicProfileSlug is not null ? $"/talent/{profile.PublicProfileSlug}" : null;

        return new PublicProfilePreviewDto(isPublic, publicUrl, ToPublicDto(profile, badges));
    }

    public async Task<PublicCandidateProfileDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles
            .Include(c => c.User)
            .Include(c => c.Projects)
            .FirstOrDefaultAsync(c => c.PublicProfileSlug == slug, ct);

        if (profile is null || profile.ProfileVisibility != ProfileVisibility.PublicShareable)
        {
            throw new NotFoundException("This profile isn't available.");
        }

        var badges = await GetBadgesAsync(profile.Id, ct);
        return ToPublicDto(profile, badges);
    }

    private async Task<IReadOnlyList<RecruiterVisibleBadgeDto>> GetBadgesAsync(int candidateProfileId, CancellationToken ct)
    {
        return await _db.SkillAssessmentAttempts
            .Where(a => a.CandidateProfileId == candidateProfileId && a.IsVisibleToRecruiters && a.Status == Domain.Enums.AssessmentAttemptStatus.Completed)
            .OrderByDescending(a => a.PercentageScore)
            .Select(a => new RecruiterVisibleBadgeDto(a.Category.ToString(), a.PercentageScore!.Value, a.SubmittedAt!.Value))
            .ToListAsync(ct);
    }

    private static PublicCandidateProfileDto ToPublicDto(Domain.Entities.CandidateProfile p, IReadOnlyList<RecruiterVisibleBadgeDto> badges) => new(
        p.User.FullName,
        p.Headline,
        AvatarUrlFormatter.Format(p.Id, p.AvatarStorageKey),
        p.SkillsCsv,
        p.Summary,
        p.ExperienceSummary,
        p.TotalExperienceYears,
        p.Education,
        p.Projects.OrderBy(e => e.DisplayOrder).Select(e => new ProjectDto(e.Id, e.Title, e.Description, e.ProjectUrl, e.TechnologiesCsv, e.DisplayOrder)).ToList(),
        p.LinkedInUrl,
        p.GithubUrl,
        p.PortfolioUrl,
        badges);
}
