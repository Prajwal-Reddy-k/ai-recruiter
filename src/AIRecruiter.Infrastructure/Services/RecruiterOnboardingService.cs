using AIRecruiter.Application.DTOs.Recruiters;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class RecruiterOnboardingService : IRecruiterOnboardingService
{
    private readonly AppDbContext _db;
    private readonly IndiaLocationValidator _locationValidator;
    private readonly IAuditLogService _auditLog;

    public RecruiterOnboardingService(AppDbContext db, IndiaLocationValidator locationValidator, IAuditLogService auditLog)
    {
        _db = db;
        _locationValidator = locationValidator;
        _auditLog = auditLog;
    }

    public async Task<OnboardingStatusDto> GetStatusAsync(int userId, CancellationToken ct = default)
    {
        var profile = await _db.RecruiterProfiles
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.UserId == userId, ct);

        return ToDto(profile);
    }

    public async Task<OnboardingStatusDto> UpsertAsync(int userId, UpsertRecruiterOnboardingRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.City) || !string.IsNullOrWhiteSpace(request.State))
        {
            var (isValid, error) = _locationValidator.Validate(request.State, request.City, isRemote: false);
            if (!isValid)
            {
                throw new ValidationException(error!);
            }
        }

        var profile = await _db.RecruiterProfiles
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.UserId == userId, ct);

        var isNew = profile is null;

        if (profile is null)
        {
            var company = new Company { Name = request.CompanyName };
            profile = new RecruiterProfile
            {
                UserId = userId,
                Company = company,
                Designation = request.Designation,
            };

            _db.RecruiterProfiles.Add(profile);
        }

        profile.Company.Name = request.CompanyName;
        profile.Company.Website = request.Website;
        profile.Company.Industry = request.Industry;
        profile.Company.Description = request.Description;
        profile.Company.LogoUrl = request.LogoUrl;
        profile.Company.City = request.City;
        profile.Company.State = request.State;
        profile.Company.Size = request.Size;
        profile.Company.Benefits = request.Benefits;
        profile.Company.CultureHighlights = request.CultureHighlights;
        profile.Company.LinkedInUrl = request.LinkedInUrl;
        profile.Company.TwitterUrl = request.TwitterUrl;
        profile.Company.UpdatedAt = DateTime.UtcNow;
        profile.Designation = request.Designation;
        profile.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, "Recruiter", isNew ? "CompanyCreated" : "CompanyUpdated", "Company", profile.CompanyId, new { profile.Company.Name }, ct);

        return ToDto(profile);
    }

    private static OnboardingStatusDto ToDto(RecruiterProfile? profile)
    {
        if (profile is null)
        {
            return new OnboardingStatusDto(false, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        }

        var c = profile.Company;
        return new OnboardingStatusDto(
            true, profile.CompanyId, c.Name, profile.Designation,
            c.Website, c.Industry, c.Description, c.LogoUrl,
            c.City, c.State, c.Size, c.Benefits, c.CultureHighlights, c.LinkedInUrl, c.TwitterUrl);
    }
}
