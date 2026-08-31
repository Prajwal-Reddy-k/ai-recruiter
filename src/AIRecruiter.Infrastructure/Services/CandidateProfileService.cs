using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AIRecruiter.Infrastructure.Services;

public class CandidateProfileService : ICandidateProfileService
{
    private readonly AppDbContext _db;
    private readonly IResumeStorage _resumeStorage;
    private readonly IResumeTextExtractorFactory _extractorFactory;
    private readonly ResumeFileValidator _validator;
    private readonly ResumeStorageOptions _storageOptions;
    private readonly IndiaLocationValidator _locationValidator;
    private readonly CandidateProfileValidator _profileValidator;
    private readonly ImageFileValidator _imageValidator;
    private readonly IAvatarImageProcessor _avatarProcessor;
    private readonly AvatarOptions _avatarOptions;

    public CandidateProfileService(
        AppDbContext db,
        IResumeStorage resumeStorage,
        IResumeTextExtractorFactory extractorFactory,
        ResumeFileValidator validator,
        IOptions<ResumeStorageOptions> storageOptions,
        IndiaLocationValidator locationValidator,
        CandidateProfileValidator profileValidator,
        ImageFileValidator imageValidator,
        IAvatarImageProcessor avatarProcessor,
        IOptions<AvatarOptions> avatarOptions)
    {
        _db = db;
        _resumeStorage = resumeStorage;
        _extractorFactory = extractorFactory;
        _validator = validator;
        _storageOptions = storageOptions.Value;
        _locationValidator = locationValidator;
        _profileValidator = profileValidator;
        _imageValidator = imageValidator;
        _avatarProcessor = avatarProcessor;
        _avatarOptions = avatarOptions.Value;
    }

    public async Task<CandidateProfileDto> GetMyProfileAsync(int userId, CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(userId, ct);
        return ToDto(profile);
    }

    public async Task<CandidateProfileDto> UpsertMyProfileAsync(int userId, UpsertCandidateProfileRequest request, CancellationToken ct = default)
    {
        var validation = _profileValidator.Validate(request);

        var (locationValid, locationError) = _locationValidator.Validate(request.State, request.City, isRemote: false);
        var fieldErrors = validation.Errors;
        if (!locationValid && !(string.IsNullOrWhiteSpace(request.City) && string.IsNullOrWhiteSpace(request.State)))
        {
            fieldErrors = new Dictionary<string, string>(validation.Errors) { ["location"] = locationError! };
        }

        if (fieldErrors.Count > 0)
        {
            throw new ValidationException("Please fix the highlighted fields.", fieldErrors);
        }

        var profile = await GetOrCreateProfileAsync(userId, ct);

        profile.Headline = request.Headline?.Trim();
        profile.Summary = request.Summary?.Trim();
        profile.Education = request.Education?.Trim();
        profile.GraduationYear = request.GraduationYear;
        profile.ExperienceSummary = request.ExperienceSummary?.Trim();
        profile.TotalExperienceYears = request.TotalExperienceYears;

        profile.City = request.City;
        profile.State = request.State;
        profile.Locality = request.Locality;

        profile.CurrentSalary = request.CurrentSalary;
        profile.ExpectedSalary = request.ExpectedSalary;
        profile.SkillsCsv = NormalizeSkillsCsv(request.SkillsCsv);
        profile.Phone = validation.NormalizedPhone;
        profile.LinkedInUrl = request.LinkedInUrl?.Trim();
        profile.GithubUrl = request.GithubUrl?.Trim();
        profile.PortfolioUrl = request.PortfolioUrl?.Trim();
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(profile);
    }

    /// <summary>Trims and deduplicates (case-insensitive) the already-validated skills list
    /// before storage, so the persisted value matches what was validated.</summary>
    private static string? NormalizeSkillsCsv(string? skillsCsv)
    {
        if (string.IsNullOrWhiteSpace(skillsCsv)) return null;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var raw in skillsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var skill = raw.Trim();
            if (skill.Length > 0 && seen.Add(skill))
            {
                result.Add(skill);
            }
        }
        return result.Count > 0 ? string.Join(", ", result) : null;
    }

    public async Task<CandidateProfileDto> UploadResumeAsync(int userId, Stream content, string originalFileName, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        var header = new byte[8];
        var read = await content.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var validation = _validator.Validate(originalFileName, contentType, sizeBytes, header[..read], _storageOptions.MaxSizeBytes);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Error!);
        }

        var profile = await GetOrCreateProfileAsync(userId, ct);

        // Extract text before persisting the file reference, so a corrupt/unparseable
        // upload never gets stored as the candidate's resume.
        string extractedText;
        if (content.CanSeek)
        {
            content.Position = 0;
        }
        try
        {
            var extractor = _extractorFactory.GetExtractor(contentType);
            extractedText = await extractor.ExtractTextAsync(content, ct);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new ValidationException("The file could not be read as a valid PDF or DOCX document.");
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var previousKey = profile.ResumeStorageKey;
        var result = await _resumeStorage.SaveAsync(profile.Id, content, originalFileName, contentType, ct);

        profile.ResumeStorageKey = result.StorageKey;
        profile.ResumeOriginalFileName = originalFileName;
        profile.ResumeContentType = contentType;
        profile.ResumeSizeBytes = result.SizeBytes;
        profile.ResumeUploadedAt = DateTime.UtcNow;
        profile.ResumeExtractedText = extractedText;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrEmpty(previousKey) && previousKey != result.StorageKey)
        {
            await _resumeStorage.DeleteAsync(previousKey, ct);
        }

        return ToDto(profile);
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadOwnResumeAsync(int userId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new NotFoundException("Candidate profile not found.");

        return await OpenResumeAsync(profile, ct);
    }

    internal async Task<(Stream Content, string FileName, string ContentType)> OpenResumeAsync(CandidateProfile profile, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(profile.ResumeStorageKey))
        {
            throw new NotFoundException("No resume has been uploaded yet.");
        }

        var stream = await _resumeStorage.OpenReadAsync(profile.ResumeStorageKey, ct);
        return (stream, profile.ResumeOriginalFileName ?? "resume", profile.ResumeContentType ?? "application/octet-stream");
    }

    public async Task<CandidateProfileDto> UploadAvatarAsync(int userId, Stream content, string originalFileName, string contentType, long sizeBytes, CancellationToken ct = default)
    {
        var header = new byte[12];
        var read = await content.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var validation = _imageValidator.Validate(originalFileName, contentType, sizeBytes, header[..read], _avatarOptions.MaxSizeBytes);
        if (!validation.IsValid)
        {
            throw new ValidationException(validation.Error!);
        }

        var profile = await GetOrCreateProfileAsync(userId, ct);

        AvatarProcessResult processed;
        try
        {
            processed = await _avatarProcessor.ProcessAsync(content, ct);
        }
        catch (Exception ex) when (ex is not AppException)
        {
            throw new ValidationException("The image could not be processed. Please try a different file.");
        }

        using (processed.Content)
        {
            var previousKey = profile.AvatarStorageKey;
            var result = await _resumeStorage.SaveAsync(profile.Id, processed.Content, $"avatar{processed.Extension}", processed.ContentType, ct);

            profile.AvatarStorageKey = result.StorageKey;
            profile.AvatarContentType = processed.ContentType;
            profile.AvatarSizeBytes = result.SizeBytes;
            profile.AvatarUploadedAt = DateTime.UtcNow;
            profile.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            if (!string.IsNullOrEmpty(previousKey) && previousKey != result.StorageKey)
            {
                await _resumeStorage.DeleteAsync(previousKey, ct);
            }
        }

        return ToDto(profile);
    }

    public async Task<CandidateProfileDto> RemoveAvatarAsync(int userId, CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(userId, ct);

        if (!string.IsNullOrEmpty(profile.AvatarStorageKey))
        {
            await _resumeStorage.DeleteAsync(profile.AvatarStorageKey, ct);
        }

        profile.AvatarStorageKey = null;
        profile.AvatarContentType = null;
        profile.AvatarSizeBytes = null;
        profile.AvatarUploadedAt = null;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(profile);
    }

    /// <summary>Serves an avatar by candidate profile id — deliberately not ownership-scoped,
    /// since profile photos are shown to other users (recruiters, navbar, dashboards) and are
    /// not sensitive in the way a resume is. Returns 404 (via NotFoundException) if the
    /// profile has no avatar, letting the frontend fall back to an initials avatar.</summary>
    public async Task<(Stream Content, string ContentType)> OpenAvatarAsync(int candidateProfileId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.Id == candidateProfileId, ct)
            ?? throw new NotFoundException("No avatar found.");

        if (string.IsNullOrEmpty(profile.AvatarStorageKey))
        {
            throw new NotFoundException("No avatar found.");
        }

        var stream = await _resumeStorage.OpenReadAsync(profile.AvatarStorageKey, ct);
        return (stream, profile.AvatarContentType ?? "image/jpeg");
    }

    private async Task<CandidateProfile> GetOrCreateProfileAsync(int userId, CancellationToken ct)
    {
        var profile = await _db.CandidateProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.UserId == userId, ct);
        if (profile is not null)
        {
            return profile;
        }

        profile = new CandidateProfile { UserId = userId };
        _db.CandidateProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);
        await _db.Entry(profile).Reference(p => p.User).LoadAsync(ct);
        return profile;
    }

    private static CandidateProfileDto ToDto(CandidateProfile p) => new(
        p.Id,
        p.User?.FullName ?? string.Empty,
        p.Headline,
        p.Summary,
        p.Education,
        p.GraduationYear,
        p.ExperienceSummary,
        p.TotalExperienceYears,
        p.City,
        p.State,
        p.Locality,
        IndiaLocationFormatter.Format(p.City, p.State, isRemote: false),
        p.CurrentSalary,
        p.ExpectedSalary,
        p.SkillsCsv,
        p.Phone,
        p.LinkedInUrl,
        p.GithubUrl,
        p.PortfolioUrl,
        p.ResumeOriginalFileName,
        p.ResumeSizeBytes,
        p.ResumeUploadedAt,
        AvatarUrlFormatter.Format(p.Id, p.AvatarStorageKey));
}
