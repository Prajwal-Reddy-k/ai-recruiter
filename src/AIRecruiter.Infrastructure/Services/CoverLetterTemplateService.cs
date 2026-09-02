using AIRecruiter.Application.DTOs.CoverLetters;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class CoverLetterTemplateService : ICoverLetterTemplateService
{
    private const int MaxTitle = 100;
    private const int MaxSection = 1500;
    private const int MaxClosing = 500;

    private readonly AppDbContext _db;

    public CoverLetterTemplateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CoverLetterTemplateDto>> GetMyTemplatesAsync(int userId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (profile is null) return Array.Empty<CoverLetterTemplateDto>();

        return await _db.CoverLetterTemplates
            .Where(t => t.CandidateProfileId == profile.Id)
            .OrderBy(t => t.Title)
            .Select(t => ToDto(t))
            .ToListAsync(ct);
    }

    public async Task<CoverLetterTemplateDto> CreateAsync(int userId, UpsertCoverLetterTemplateRequest request, CancellationToken ct = default)
    {
        await Validate(request, userId, excludeId: null, ct);

        var profile = await GetOrCreateProfileAsync(userId, ct);
        var template = new CoverLetterTemplate
        {
            CandidateProfileId = profile.Id,
            Title = request.Title.Trim(),
            Introduction = request.Introduction?.Trim(),
            SkillsHighlights = request.SkillsHighlights?.Trim(),
            ProjectAchievements = request.ProjectAchievements?.Trim(),
            ClosingMessage = request.ClosingMessage?.Trim(),
        };
        _db.CoverLetterTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
        return ToDto(template);
    }

    public async Task<CoverLetterTemplateDto> UpdateAsync(int userId, int templateId, UpsertCoverLetterTemplateRequest request, CancellationToken ct = default)
    {
        var template = await GetOwnedAsync(userId, templateId, ct);
        await Validate(request, userId, excludeId: templateId, ct);

        template.Title = request.Title.Trim();
        template.Introduction = request.Introduction?.Trim();
        template.SkillsHighlights = request.SkillsHighlights?.Trim();
        template.ProjectAchievements = request.ProjectAchievements?.Trim();
        template.ClosingMessage = request.ClosingMessage?.Trim();
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToDto(template);
    }

    public async Task DeleteAsync(int userId, int templateId, CancellationToken ct = default)
    {
        var template = await GetOwnedAsync(userId, templateId, ct);
        _db.CoverLetterTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<CandidateProfile> GetOrCreateProfileAsync(int userId, CancellationToken ct)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (profile is not null) return profile;

        profile = new CandidateProfile { UserId = userId };
        _db.CandidateProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        return profile;
    }

    /// <summary>Loads a template and verifies it belongs to the caller's own candidate
    /// profile — never trusts the route id alone.</summary>
    private async Task<CoverLetterTemplate> GetOwnedAsync(int userId, int templateId, CancellationToken ct)
    {
        var template = await _db.CoverLetterTemplates.FirstOrDefaultAsync(t => t.Id == templateId, ct)
            ?? throw new NotFoundException("Template not found.");

        var ownerUserId = await _db.CandidateProfiles.Where(c => c.Id == template.CandidateProfileId).Select(c => c.UserId).FirstOrDefaultAsync(ct);
        if (ownerUserId != userId)
        {
            throw new ForbiddenException("You do not have access to this template.");
        }

        return template;
    }

    private async Task Validate(UpsertCoverLetterTemplateRequest r, int userId, int? excludeId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(r.Title) || r.Title.Trim().Length > MaxTitle) errors["title"] = $"Title is required (up to {MaxTitle} characters).";
        if (r.Introduction is { Length: > MaxSection }) errors["introduction"] = $"Introduction must be {MaxSection} characters or fewer.";
        if (r.SkillsHighlights is { Length: > MaxSection }) errors["skillsHighlights"] = $"Skills highlights must be {MaxSection} characters or fewer.";
        if (r.ProjectAchievements is { Length: > MaxSection }) errors["projectAchievements"] = $"Project achievements must be {MaxSection} characters or fewer.";
        if (r.ClosingMessage is { Length: > MaxClosing }) errors["closingMessage"] = $"Closing message must be {MaxClosing} characters or fewer.";

        if (errors.Count == 0 && !string.IsNullOrWhiteSpace(r.Title))
        {
            var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
            if (profile is not null)
            {
                var title = r.Title.Trim();
                var duplicate = await _db.CoverLetterTemplates.AnyAsync(
                    t => t.CandidateProfileId == profile.Id && t.Id != (excludeId ?? -1) && t.Title.ToLower() == title.ToLower(), ct);
                if (duplicate)
                {
                    throw new ConflictException("DUPLICATE_TEMPLATE_TITLE", "You already have a template with this title.");
                }
            }
        }

        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);
    }

    private static CoverLetterTemplateDto ToDto(CoverLetterTemplate t) => new(
        t.Id, t.Title, t.Introduction, t.SkillsHighlights, t.ProjectAchievements, t.ClosingMessage, t.CreatedAt, t.UpdatedAt);
}
