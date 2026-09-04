using AIRecruiter.Application.DTOs.Companies;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Recruiter-side submission for the "Platform Verified" workflow — never a
/// government or legal verification, just an admin-reviewed badge. Only a company Owner can
/// submit, mirroring TeamService's exact owner-only check.</summary>
public class CompanyVerificationService : ICompanyVerificationService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public CompanyVerificationService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<CompanyVerificationStatusDto> SubmitAsync(int recruiterUserId, SubmitCompanyVerificationRequest request, CancellationToken ct = default)
    {
        var profile = await EnsureCallerIsOwnerAsync(recruiterUserId, ct);
        var company = await _db.Companies.FirstAsync(c => c.Id == profile.CompanyId, ct);

        if (company.VerificationStatus is CompanyVerificationStatus.Pending or CompanyVerificationStatus.Verified)
        {
            throw new ConflictException("VERIFICATION_ALREADY_ACTIVE", $"Your company's verification is already {company.VerificationStatus}.");
        }

        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(request.BusinessEmail) || !request.BusinessEmail.Contains('@'))
        {
            errors["businessEmail"] = "Enter a valid business email address.";
        }
        if (string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.State))
        {
            errors["city"] = "Provide your India headquarters city and state.";
        }
        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length < 20)
        {
            errors["description"] = "Provide a short company description (at least 20 characters).";
        }
        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);

        company.BusinessEmail = request.BusinessEmail.Trim();
        company.Website = request.Website?.Trim() ?? company.Website;
        company.City = request.City?.Trim() ?? company.City;
        company.State = request.State?.Trim() ?? company.State;
        company.Description = request.Description?.Trim() ?? company.Description;
        company.VerificationDocumentReference = request.VerificationDocumentReference?.Trim();
        company.VerificationStatus = CompanyVerificationStatus.Pending;
        company.VerificationSubmittedAtUtc = DateTime.UtcNow;
        company.VerificationNote = null;
        company.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "CompanyVerificationSubmitted", "Company", company.Id, new { company.Name }, ct);

        return ToDto(company);
    }

    public async Task<CompanyVerificationStatusDto> GetMyStatusAsync(int recruiterUserId, CancellationToken ct = default)
    {
        var profile = await _db.RecruiterProfiles.Include(r => r.Company).FirstOrDefaultAsync(r => r.UserId == recruiterUserId, ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");

        return ToDto(profile.Company);
    }

    private async Task<Domain.Entities.RecruiterProfile> EnsureCallerIsOwnerAsync(int ownerUserId, CancellationToken ct)
    {
        var profile = await _db.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == ownerUserId, ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");

        if (profile.CompanyRole != CompanyRole.Owner)
        {
            throw new ForbiddenException("Only a company owner can submit for verification.");
        }

        return profile;
    }

    private static CompanyVerificationStatusDto ToDto(Domain.Entities.Company c) =>
        new(c.VerificationStatus.ToString(), c.VerificationNote, c.VerificationSubmittedAtUtc, c.VerificationReviewedAtUtc);
}
