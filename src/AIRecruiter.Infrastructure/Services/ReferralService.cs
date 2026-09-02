using System.Security.Cryptography;
using System.Text;
using AIRecruiter.Application.DTOs.Referrals;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>No-email employee referral workflow. Token generation/hashing mirrors
/// AuthService's exact password-reset-token convention (CSPRNG + SHA256, raw value
/// returned once, only the hash persisted).</summary>
public class ReferralService : IReferralService
{
    private const int MaxRequestsPerUser = 20;
    private const int MaxRequestsPerIp = 40;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(5);
    private const int TokenValidityDays = 30;

    private readonly AppDbContext _db;
    private readonly IIpRateLimiter _rateLimiter;

    public ReferralService(AppDbContext db, IIpRateLimiter rateLimiter)
    {
        _db = db;
        _rateLimiter = rateLimiter;
    }

    public async Task<CreateReferralResponse> CreateAsync(int referrerUserId, string ipAddress, CreateReferralRequest request, CancellationToken ct = default)
    {
        if (!_rateLimiter.IsAllowed($"CreateReferral:{referrerUserId}", MaxRequestsPerUser, RateLimitWindow)
            || !_rateLimiter.IsAllowed($"CreateReferral:ip:{ipAddress}", MaxRequestsPerIp, RateLimitWindow))
        {
            throw new RateLimitedException("You're creating referrals too quickly. Please wait a moment and try again.", retryAfterSeconds: 60);
        }

        var errors = new Dictionary<string, string>();
        if (string.IsNullOrWhiteSpace(request.ReferredName) || request.ReferredName.Trim().Length > 200)
        {
            errors["referredName"] = "Name is required (up to 200 characters).";
        }
        var email = request.ReferredEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Length > 256)
        {
            errors["referredEmail"] = "Enter a valid email address.";
        }
        if (request.Note is { Length: > 2000 })
        {
            errors["note"] = "Note must be 2000 characters or fewer.";
        }
        if (errors.Count > 0) throw new ValidationException("Please fix the highlighted fields.", errors);

        var job = await _db.JobPostings.FirstOrDefaultAsync(j => j.Id == request.JobPostingId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (job.Status != JobStatus.Open || job.ModerationStatus != ModerationStatus.Approved)
        {
            throw new ConflictException("JOB_NOT_OPEN", "This job is not currently accepting referrals.");
        }

        var duplicate = await _db.Referrals.AnyAsync(r => r.ReferredEmail == email && r.JobPostingId == request.JobPostingId, ct);
        if (duplicate)
        {
            throw new ConflictException("REFERRAL_ALREADY_EXISTS", "This person has already been referred for this job.");
        }

        var rawToken = GenerateReferralToken();
        var referral = new Referral
        {
            ReferrerUserId = referrerUserId,
            JobPostingId = request.JobPostingId,
            ReferredName = request.ReferredName.Trim(),
            ReferredEmail = email,
            ReferredPhone = request.ReferredPhone?.Trim(),
            RelevantSkillsCsv = request.RelevantSkillsCsv?.Trim(),
            Note = request.Note?.Trim(),
            TokenHash = HashToken(rawToken),
            TokenExpiresAtUtc = DateTime.UtcNow.AddDays(TokenValidityDays),
            Status = ReferralStatus.Invited,
        };
        _db.Referrals.Add(referral);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            var stillDuplicate = await _db.Referrals.AnyAsync(r => r.ReferredEmail == email && r.JobPostingId == request.JobPostingId && r.Id != referral.Id, ct);
            if (stillDuplicate) throw new ConflictException("REFERRAL_ALREADY_EXISTS", "This person has already been referred for this job.");
            throw;
        }

        return new CreateReferralResponse(referral.Id, rawToken, referral.TokenExpiresAtUtc);
    }

    public async Task<IReadOnlyList<ReferralDto>> GetMyReferralsAsync(int referrerUserId, CancellationToken ct = default)
    {
        var referrals = await _db.Referrals
            .Where(r => r.ReferrerUserId == referrerUserId)
            .Include(r => r.JobPosting).ThenInclude(j => j.Company)
            .Include(r => r.JobApplication)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        // Only privacy-safe fields projected — never the referred person's full profile.
        return referrals.Select(r => new ReferralDto(
            r.Id, r.ReferredName, r.ReferredEmail, r.JobPosting.Title, r.JobPosting.Company.Name,
            DeriveDisplayStatus(r, r.JobApplication), r.CreatedAt, r.RegisteredAtUtc, r.AppliedAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<ReferralDto>> GetCompanyReferralsAsync(int recruiterUserId, CancellationToken ct = default)
    {
        var companyId = await _db.RecruiterProfiles
            .Where(r => r.UserId == recruiterUserId)
            .Select(r => (int?)r.CompanyId)
            .FirstOrDefaultAsync(ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");

        var referrals = await _db.Referrals
            .Where(r => r.JobPosting.CompanyId == companyId)
            .Include(r => r.JobPosting).ThenInclude(j => j.Company)
            .Include(r => r.JobApplication)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return referrals.Select(r => new ReferralDto(
            r.Id, r.ReferredName, r.ReferredEmail, r.JobPosting.Title, r.JobPosting.Company.Name,
            DeriveDisplayStatus(r, r.JobApplication), r.CreatedAt, r.RegisteredAtUtc, r.AppliedAtUtc)).ToList();
    }

    public async Task<ReferralTokenPreviewDto> ResolveTokenAsync(string rawToken, CancellationToken ct = default)
    {
        var hash = HashToken(rawToken);
        var referral = await _db.Referrals.Include(r => r.JobPosting).ThenInclude(j => j.Company)
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);

        if (referral is null || referral.TokenExpiresAtUtc < DateTime.UtcNow)
        {
            // Generic message either way — never distinguish "expired" from "never existed".
            throw new NotFoundException("This referral link is invalid or has expired.");
        }

        return new ReferralTokenPreviewDto(referral.JobPosting.Title, referral.JobPosting.Company.Name, referral.TokenExpiresAtUtc);
    }

    /// <summary>Interviewing/Hired/NotSelected are derived from the linked application's
    /// status, never separately tracked — a single source of truth.</summary>
    private static string DeriveDisplayStatus(Referral r, JobApplication? app)
    {
        if (r.Status != ReferralStatus.Applied || app is null)
        {
            return r.Status.ToString();
        }

        return app.Status switch
        {
            ApplicationStatus.Hired => "Hired",
            ApplicationStatus.Rejected or ApplicationStatus.Withdrawn => "NotSelected",
            ApplicationStatus.InterviewScheduled or ApplicationStatus.InterviewCompleted => "Interviewing",
            _ => "Applied",
        };
    }

    /// <summary>URL-safe (base64url, no padding) so the raw token can sit directly in a
    /// path segment (GET api/referrals/token/{token}) and a query string without encoding —
    /// plain Base64's '+'/'/' characters would otherwise corrupt both.</summary>
    private static string GenerateReferralToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
