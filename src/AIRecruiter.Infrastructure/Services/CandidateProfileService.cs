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

    public CandidateProfileService(
        AppDbContext db,
        IResumeStorage resumeStorage,
        IResumeTextExtractorFactory extractorFactory,
        ResumeFileValidator validator,
        IOptions<ResumeStorageOptions> storageOptions,
        IndiaLocationValidator locationValidator)
    {
        _db = db;
        _resumeStorage = resumeStorage;
        _extractorFactory = extractorFactory;
        _validator = validator;
        _storageOptions = storageOptions.Value;
        _locationValidator = locationValidator;
    }

    public async Task<CandidateProfileDto> GetMyProfileAsync(int userId, CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(userId, ct);
        return ToDto(profile);
    }

    public async Task<CandidateProfileDto> UpsertMyProfileAsync(int userId, UpsertCandidateProfileRequest request, CancellationToken ct = default)
    {
        var profile = await GetOrCreateProfileAsync(userId, ct);

        profile.Headline = request.Headline;
        profile.Summary = request.Summary;
        profile.Education = request.Education;
        profile.ExperienceSummary = request.ExperienceSummary;
        profile.TotalExperienceYears = request.TotalExperienceYears;

        var (isValid, error) = _locationValidator.Validate(request.State, request.City, isRemote: false);
        if (!isValid && !(string.IsNullOrWhiteSpace(request.City) && string.IsNullOrWhiteSpace(request.State)))
        {
            throw new ValidationException(error!);
        }
        profile.City = request.City;
        profile.State = request.State;
        profile.Locality = request.Locality;

        profile.CurrentSalary = request.CurrentSalary;
        profile.ExpectedSalary = request.ExpectedSalary;
        profile.SkillsCsv = request.SkillsCsv;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(profile);
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
        p.ExperienceSummary,
        p.TotalExperienceYears,
        p.City,
        p.State,
        p.Locality,
        IndiaLocationFormatter.Format(p.City, p.State, isRemote: false),
        p.CurrentSalary,
        p.ExpectedSalary,
        p.SkillsCsv,
        p.ResumeOriginalFileName,
        p.ResumeSizeBytes,
        p.ResumeUploadedAt);
}
