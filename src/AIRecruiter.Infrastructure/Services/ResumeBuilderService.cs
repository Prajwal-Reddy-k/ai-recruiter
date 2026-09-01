using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class ResumeBuilderService : IResumeBuilderService
{
    private const int MaxShortText = 150;
    private const int MaxDescription = 2000;

    private readonly AppDbContext _db;

    public ResumeBuilderService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ResumeDto> GetMyResumeAsync(int userId, CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(userId, ct);
        return ToDto(profile);
    }

    public async Task<ResumeDto> UpsertSummaryLinksAsync(int userId, UpsertResumeSummaryRequest request, CancellationToken ct = default)
    {
        var errors = new Dictionary<string, string>();

        if (!string.IsNullOrEmpty(request.Summary) && request.Summary.Length > MaxDescription)
        {
            errors["summary"] = $"Summary must be {MaxDescription} characters or fewer.";
        }
        ValidateUrl(request.LinkedInUrl, "linkedInUrl", "LinkedIn", "linkedin.com", errors);
        ValidateUrl(request.GithubUrl, "githubUrl", "GitHub", "github.com", errors);
        ValidateUrl(request.PortfolioUrl, "portfolioUrl", "portfolio/website", null, errors);
        if (!string.IsNullOrEmpty(request.AchievementsText) && request.AchievementsText.Length > MaxDescription)
        {
            errors["achievementsText"] = $"Achievements must be {MaxDescription} characters or fewer.";
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Please fix the highlighted fields.", errors);
        }

        var profile = await GetOrCreateProfileAsync(userId, ct);
        profile.Summary = request.Summary?.Trim();
        if (!string.IsNullOrWhiteSpace(request.SkillsCsv))
        {
            profile.SkillsCsv = request.SkillsCsv.Trim();
        }
        profile.LinkedInUrl = request.LinkedInUrl?.Trim();
        profile.GithubUrl = request.GithubUrl?.Trim();
        profile.PortfolioUrl = request.PortfolioUrl?.Trim();
        profile.AchievementsText = request.AchievementsText?.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return ToDto(profile);
    }

    // --- Work experience ---

    public async Task<ResumeDto> AddExperienceAsync(int userId, UpsertWorkExperienceRequest request, CancellationToken ct = default)
    {
        ValidateExperience(request);
        var profile = await GetOrCreateProfileAsync(userId, ct);
        var nextOrder = profile.WorkExperiences.Count > 0 ? profile.WorkExperiences.Max(e => e.DisplayOrder) + 1 : 0;

        _db.CandidateWorkExperiences.Add(new CandidateWorkExperience
        {
            CandidateProfileId = profile.Id,
            Title = request.Title.Trim(),
            Company = request.Company.Trim(),
            Location = request.Location?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Description = request.Description?.Trim(),
            DisplayOrder = nextOrder,
        });
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> UpdateExperienceAsync(int userId, int experienceId, UpsertWorkExperienceRequest request, CancellationToken ct = default)
    {
        ValidateExperience(request);
        var entry = await GetOwnedAsync(_db.CandidateWorkExperiences, userId, experienceId, ct);
        entry.Title = request.Title.Trim();
        entry.Company = request.Company.Trim();
        entry.Location = request.Location?.Trim();
        entry.StartDate = request.StartDate;
        entry.EndDate = request.EndDate;
        entry.Description = request.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> DeleteExperienceAsync(int userId, int experienceId, CancellationToken ct = default)
    {
        var entry = await GetOwnedAsync(_db.CandidateWorkExperiences, userId, experienceId, ct);
        _db.CandidateWorkExperiences.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> ReorderExperienceAsync(int userId, ReorderRequest request, CancellationToken ct = default)
    {
        await ReorderAsync(_db.CandidateWorkExperiences, userId, request, ct);
        return await GetMyResumeAsync(userId, ct);
    }

    // --- Education ---

    public async Task<ResumeDto> AddEducationAsync(int userId, UpsertEducationEntryRequest request, CancellationToken ct = default)
    {
        ValidateEducation(request);
        var profile = await GetOrCreateProfileAsync(userId, ct);
        var nextOrder = profile.ResumeEducations.Count > 0 ? profile.ResumeEducations.Max(e => e.DisplayOrder) + 1 : 0;

        _db.CandidateEducations.Add(new CandidateEducation
        {
            CandidateProfileId = profile.Id,
            Institution = request.Institution.Trim(),
            Degree = request.Degree.Trim(),
            FieldOfStudy = request.FieldOfStudy?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            GradeOrGpa = request.GradeOrGpa?.Trim(),
            Description = request.Description?.Trim(),
            DisplayOrder = nextOrder,
        });
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> UpdateEducationAsync(int userId, int educationId, UpsertEducationEntryRequest request, CancellationToken ct = default)
    {
        ValidateEducation(request);
        var entry = await GetOwnedAsync(_db.CandidateEducations, userId, educationId, ct);
        entry.Institution = request.Institution.Trim();
        entry.Degree = request.Degree.Trim();
        entry.FieldOfStudy = request.FieldOfStudy?.Trim();
        entry.StartDate = request.StartDate;
        entry.EndDate = request.EndDate;
        entry.GradeOrGpa = request.GradeOrGpa?.Trim();
        entry.Description = request.Description?.Trim();
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> DeleteEducationAsync(int userId, int educationId, CancellationToken ct = default)
    {
        var entry = await GetOwnedAsync(_db.CandidateEducations, userId, educationId, ct);
        _db.CandidateEducations.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> ReorderEducationAsync(int userId, ReorderRequest request, CancellationToken ct = default)
    {
        await ReorderAsync(_db.CandidateEducations, userId, request, ct);
        return await GetMyResumeAsync(userId, ct);
    }

    // --- Certifications ---

    public async Task<ResumeDto> AddCertificationAsync(int userId, UpsertCertificationRequest request, CancellationToken ct = default)
    {
        ValidateCertification(request);
        var profile = await GetOrCreateProfileAsync(userId, ct);
        var nextOrder = profile.Certifications.Count > 0 ? profile.Certifications.Max(e => e.DisplayOrder) + 1 : 0;

        _db.CandidateCertifications.Add(new CandidateCertification
        {
            CandidateProfileId = profile.Id,
            Name = request.Name.Trim(),
            IssuingOrganization = request.IssuingOrganization?.Trim(),
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            CredentialUrl = request.CredentialUrl?.Trim(),
            DisplayOrder = nextOrder,
        });
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> UpdateCertificationAsync(int userId, int certificationId, UpsertCertificationRequest request, CancellationToken ct = default)
    {
        ValidateCertification(request);
        var entry = await GetOwnedAsync(_db.CandidateCertifications, userId, certificationId, ct);
        entry.Name = request.Name.Trim();
        entry.IssuingOrganization = request.IssuingOrganization?.Trim();
        entry.IssueDate = request.IssueDate;
        entry.ExpiryDate = request.ExpiryDate;
        entry.CredentialUrl = request.CredentialUrl?.Trim();
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> DeleteCertificationAsync(int userId, int certificationId, CancellationToken ct = default)
    {
        var entry = await GetOwnedAsync(_db.CandidateCertifications, userId, certificationId, ct);
        _db.CandidateCertifications.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> ReorderCertificationAsync(int userId, ReorderRequest request, CancellationToken ct = default)
    {
        await ReorderAsync(_db.CandidateCertifications, userId, request, ct);
        return await GetMyResumeAsync(userId, ct);
    }

    // --- Projects ---

    public async Task<ResumeDto> AddProjectAsync(int userId, UpsertProjectRequest request, CancellationToken ct = default)
    {
        ValidateProject(request);
        var profile = await GetOrCreateProfileAsync(userId, ct);
        var nextOrder = profile.Projects.Count > 0 ? profile.Projects.Max(e => e.DisplayOrder) + 1 : 0;

        _db.CandidateProjects.Add(new CandidateProject
        {
            CandidateProfileId = profile.Id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            ProjectUrl = request.ProjectUrl?.Trim(),
            TechnologiesCsv = request.TechnologiesCsv?.Trim(),
            DisplayOrder = nextOrder,
        });
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> UpdateProjectAsync(int userId, int projectId, UpsertProjectRequest request, CancellationToken ct = default)
    {
        ValidateProject(request);
        var entry = await GetOwnedAsync(_db.CandidateProjects, userId, projectId, ct);
        entry.Title = request.Title.Trim();
        entry.Description = request.Description?.Trim();
        entry.ProjectUrl = request.ProjectUrl?.Trim();
        entry.TechnologiesCsv = request.TechnologiesCsv?.Trim();
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> DeleteProjectAsync(int userId, int projectId, CancellationToken ct = default)
    {
        var entry = await GetOwnedAsync(_db.CandidateProjects, userId, projectId, ct);
        _db.CandidateProjects.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return await GetMyResumeAsync(userId, ct);
    }

    public async Task<ResumeDto> ReorderProjectAsync(int userId, ReorderRequest request, CancellationToken ct = default)
    {
        await ReorderAsync(_db.CandidateProjects, userId, request, ct);
        return await GetMyResumeAsync(userId, ct);
    }

    // --- Shared helpers ---

    private async Task<CandidateProfile> GetOrCreateProfileAsync(int userId, CancellationToken ct)
    {
        var profile = await _db.CandidateProfiles
            .Include(c => c.User)
            .Include(c => c.WorkExperiences)
            .Include(c => c.ResumeEducations)
            .Include(c => c.Certifications)
            .Include(c => c.Projects)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (profile is not null) return profile;

        profile = new CandidateProfile { UserId = userId };
        _db.CandidateProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(profile).Reference(p => p.User).LoadAsync(ct);
        return profile;
    }

    /// <summary>Loads a section entry and verifies it belongs to the caller's own candidate
    /// profile — never trusts the route id alone.</summary>
    private async Task<TEntity> GetOwnedAsync<TEntity>(DbSet<TEntity> set, int userId, int entityId, CancellationToken ct)
        where TEntity : class, IResumeSectionEntry
    {
        var entry = await set.FirstOrDefaultAsync(e => e.Id == entityId, ct)
            ?? throw new NotFoundException("Entry not found.");

        var ownerUserId = await _db.CandidateProfiles.Where(c => c.Id == entry.CandidateProfileId).Select(c => c.UserId).FirstOrDefaultAsync(ct);
        if (ownerUserId != userId)
        {
            throw new ForbiddenException("You do not have access to this entry.");
        }

        return entry;
    }

    private async Task ReorderAsync<TEntity>(DbSet<TEntity> set, int userId, ReorderRequest request, CancellationToken ct)
        where TEntity : class, IResumeSectionEntry
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new NotFoundException("Candidate profile not found.");

        var entries = await set.Where(e => e.CandidateProfileId == profile.Id).ToListAsync(ct);
        var entriesById = entries.ToDictionary(e => e.Id);

        if (request.OrderedIds.Count != entries.Count || request.OrderedIds.Any(id => !entriesById.ContainsKey(id)))
        {
            throw new ValidationException("The reorder list must contain exactly your existing entries.");
        }

        for (var i = 0; i < request.OrderedIds.Count; i++)
        {
            entriesById[request.OrderedIds[i]].DisplayOrder = i;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateExperience(UpsertWorkExperienceRequest r)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Trim().Length > MaxShortText) errors["title"] = $"Job title is required (up to {MaxShortText} characters).";
        if (string.IsNullOrWhiteSpace(r.Company) || r.Company.Trim().Length > MaxShortText) errors["company"] = $"Company is required (up to {MaxShortText} characters).";
        if (r.Location is { Length: > MaxShortText }) errors["location"] = $"Location must be {MaxShortText} characters or fewer.";
        if (r.Description is { Length: > MaxDescription }) errors["description"] = $"Description must be {MaxDescription} characters or fewer.";
        if (r.EndDate is not null && r.EndDate < r.StartDate) errors["endDate"] = "End date must be on or after the start date.";
        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
    }

    private static void ValidateEducation(UpsertEducationEntryRequest r)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(r.Institution) || r.Institution.Trim().Length > MaxShortText) errors["institution"] = $"Institution is required (up to {MaxShortText} characters).";
        if (string.IsNullOrWhiteSpace(r.Degree) || r.Degree.Trim().Length > MaxShortText) errors["degree"] = $"Degree is required (up to {MaxShortText} characters).";
        if (r.FieldOfStudy is { Length: > MaxShortText }) errors["fieldOfStudy"] = $"Field of study must be {MaxShortText} characters or fewer.";
        if (r.GradeOrGpa is { Length: > 50 }) errors["gradeOrGpa"] = "Grade/GPA must be 50 characters or fewer.";
        if (r.Description is { Length: > MaxDescription }) errors["description"] = $"Description must be {MaxDescription} characters or fewer.";
        if (r.StartDate is not null && r.EndDate is not null && r.EndDate < r.StartDate) errors["endDate"] = "End date must be on or after the start date.";
        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
    }

    private static void ValidateCertification(UpsertCertificationRequest r)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(r.Name) || r.Name.Trim().Length > MaxShortText) errors["name"] = $"Certification name is required (up to {MaxShortText} characters).";
        if (r.IssuingOrganization is { Length: > MaxShortText }) errors["issuingOrganization"] = $"Issuing organization must be {MaxShortText} characters or fewer.";
        if (r.IssueDate is not null && r.ExpiryDate is not null && r.ExpiryDate < r.IssueDate) errors["expiryDate"] = "Expiry date must be on or after the issue date.";
        ValidateUrl(r.CredentialUrl, "credentialUrl", "credential", null, errors);
        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
    }

    private static void ValidateProject(UpsertProjectRequest r)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Trim().Length > MaxShortText) errors["title"] = $"Project title is required (up to {MaxShortText} characters).";
        if (r.Description is { Length: > MaxDescription }) errors["description"] = $"Description must be {MaxDescription} characters or fewer.";
        ValidateUrl(r.ProjectUrl, "projectUrl", "project", null, errors);
        if (r.TechnologiesCsv is not null)
        {
            var items = r.TechnologiesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
            if (items.Count > 15) errors["technologiesCsv"] = "List at most 15 technologies.";
            else if (items.Any(s => s.Length > 60)) errors["technologiesCsv"] = "Each technology must be 60 characters or fewer.";
        }
        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
    }

    /// <summary>Same host-or-subdomain-safe URL validation as CandidateProfileValidator.</summary>
    private static void ValidateUrl(string? url, string field, string label, string? requiredDomainSubstring, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors[field] = $"Enter a valid {label} URL (starting with https://).";
            return;
        }

        if (requiredDomainSubstring is not null &&
            !(uri.Host.Equals(requiredDomainSubstring, StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith("." + requiredDomainSubstring, StringComparison.OrdinalIgnoreCase)))
        {
            errors[field] = $"Enter a valid {label} URL (should link to {requiredDomainSubstring}).";
        }
    }

    internal static ResumeDto ToDto(CandidateProfile p)
    {
        var strength = ProfileStrengthCalculator.Calculate(new ProfileStrengthInput(
            HasHeadline: !string.IsNullOrWhiteSpace(p.Headline),
            HasSummary: !string.IsNullOrWhiteSpace(p.Summary),
            HasSkills: (p.SkillsCsv ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries).Length >= 3,
            HasAvatar: !string.IsNullOrEmpty(p.AvatarStorageKey),
            HasResumeFile: !string.IsNullOrEmpty(p.ResumeStorageKey),
            HasWorkExperience: p.WorkExperiences.Count > 0,
            HasEducation: p.ResumeEducations.Count > 0 || !string.IsNullOrWhiteSpace(p.Education),
            HasLinks: !string.IsNullOrWhiteSpace(p.LinkedInUrl) || !string.IsNullOrWhiteSpace(p.GithubUrl) || !string.IsNullOrWhiteSpace(p.PortfolioUrl),
            HasPreferences: p.RemotePreference.HasValue || !string.IsNullOrWhiteSpace(p.PreferredJobTypesCsv) || !string.IsNullOrWhiteSpace(p.PreferredLocationsCsv)));

        return new ResumeDto(
            p.User?.FullName ?? string.Empty,
            p.Headline,
            p.Summary,
            p.SkillsCsv,
            p.LinkedInUrl,
            p.GithubUrl,
            p.PortfolioUrl,
            p.AchievementsText,
            p.WorkExperiences.OrderBy(e => e.DisplayOrder).Select(e => new WorkExperienceDto(e.Id, e.Title, e.Company, e.Location, e.StartDate, e.EndDate, e.Description, e.DisplayOrder)).ToList(),
            p.ResumeEducations.OrderBy(e => e.DisplayOrder).Select(e => new EducationEntryDto(e.Id, e.Institution, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.GradeOrGpa, e.Description, e.DisplayOrder)).ToList(),
            p.Certifications.OrderBy(e => e.DisplayOrder).Select(e => new CertificationDto(e.Id, e.Name, e.IssuingOrganization, e.IssueDate, e.ExpiryDate, e.CredentialUrl, e.DisplayOrder)).ToList(),
            p.Projects.OrderBy(e => e.DisplayOrder).Select(e => new ProjectDto(e.Id, e.Title, e.Description, e.ProjectUrl, e.TechnologiesCsv, e.DisplayOrder)).ToList(),
            strength);
    }
}
