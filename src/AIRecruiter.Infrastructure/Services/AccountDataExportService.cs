using System.Text;
using AIRecruiter.Application.DTOs.Users;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class AccountDataExportService : IAccountDataExportService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public AccountDataExportService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public async Task<AccountExportDto> GetMyDataExportAsync(int userId, CancellationToken ct = default)
    {
        var export = await BuildExportAsync(userId, ct);
        await _auditLog.LogAsync(userId, export.Account.Role, "AccountDataExported", "User", userId, new { Format = "json" }, ct);
        return export;
    }

    private async Task<AccountExportDto> BuildExportAsync(int userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException("User not found.");

        var account = new AccountExportAccountDto(user.Id, user.FullName, user.Email, user.PhoneNumber, user.Role.ToString(), user.CreatedAt);

        var candidateProfile = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == userId, ct);
        var recruiterProfile = await _db.RecruiterProfiles.Include(r => r.Company).FirstOrDefaultAsync(r => r.UserId == userId, ct);

        AccountExportCandidateProfileDto? candidateDto = candidateProfile is null ? null : new(
            candidateProfile.Headline, candidateProfile.Summary, candidateProfile.Education, candidateProfile.GraduationYear,
            candidateProfile.ExperienceSummary, candidateProfile.TotalExperienceYears, candidateProfile.City, candidateProfile.State,
            candidateProfile.Locality, candidateProfile.SkillsCsv, candidateProfile.LinkedInUrl, candidateProfile.GithubUrl,
            candidateProfile.PortfolioUrl, candidateProfile.ResumeOriginalFileName, candidateProfile.AvailabilityStatus.ToString(),
            candidateProfile.PreferredJobTypesCsv, candidateProfile.PreferredLocationsCsv, candidateProfile.RemotePreference,
            candidateProfile.ExpectedSalaryMin, candidateProfile.ExpectedSalaryMax, candidateProfile.NoticePeriodDays,
            candidateProfile.ProfileVisibility.ToString());

        AccountExportRecruiterProfileDto? recruiterDto = recruiterProfile is null ? null :
            new(recruiterProfile.Company.Name, recruiterProfile.Designation, recruiterProfile.CompanyRole.ToString());

        IReadOnlyList<AccountExportApplicationDto> applications = candidateProfile is null ? Array.Empty<AccountExportApplicationDto>() :
            await _db.JobApplications
                .Where(a => a.CandidateProfileId == candidateProfile.Id)
                .Select(a => new AccountExportApplicationDto(a.JobPosting.Title, a.JobPosting.Company.Name, a.Status.ToString(), a.CreatedAt))
                .ToListAsync(ct);

        IReadOnlyList<AccountExportSavedJobDto> savedJobs = candidateProfile is null ? Array.Empty<AccountExportSavedJobDto>() :
            await _db.SavedJobs
                .Where(s => s.CandidateProfileId == candidateProfile.Id)
                .Select(s => new AccountExportSavedJobDto(s.JobPosting.Title, s.JobPosting.Company.Name, s.CreatedAt))
                .ToListAsync(ct);

        IReadOnlyList<AccountExportSavedSearchDto> savedSearches = candidateProfile is null ? Array.Empty<AccountExportSavedSearchDto>() :
            await _db.JobAlerts
                .Where(a => a.CandidateProfileId == candidateProfile.Id)
                .Select(a => new AccountExportSavedSearchDto(a.Name, a.Keyword, a.City, a.State, a.IsActive, a.CreatedAt))
                .ToListAsync(ct);

        IReadOnlyList<AccountExportFollowedCompanyDto> followedCompanies = candidateProfile is null ? Array.Empty<AccountExportFollowedCompanyDto>() :
            await _db.CompanyFollows
                .Where(f => f.CandidateProfileId == candidateProfile.Id)
                .Select(f => new AccountExportFollowedCompanyDto(f.Company.Name, f.CreatedAt))
                .ToListAsync(ct);

        IReadOnlyList<AccountExportReviewDto> reviews = candidateProfile is null ? Array.Empty<AccountExportReviewDto>() :
            await _db.CompanyReviews
                .Where(r => r.CandidateProfileId == candidateProfile.Id)
                .Select(r => new AccountExportReviewDto(r.Company.Name, r.OverallRating, r.Title, r.Status.ToString(), r.CreatedAt))
                .ToListAsync(ct);

        return new AccountExportDto(DateTime.UtcNow, account, candidateDto, recruiterDto, applications, savedJobs, savedSearches, followedCompanies, reviews);
    }

    public async Task<string> GetMyDataExportAsCsvAsync(int userId, CancellationToken ct = default)
    {
        var export = await BuildExportAsync(userId, ct);
        await _auditLog.LogAsync(userId, export.Account.Role, "AccountDataExported", "User", userId, new { Format = "csv" }, ct);

        var headers = new[] { "FullName", "Email", "PhoneNumber", "Role", "AccountCreatedAt", "Headline", "City", "State", "ProfileVisibility", "ApplicationCount", "SavedJobCount", "SavedSearchCount", "FollowedCompanyCount", "ReviewCount" };
        var values = new[]
        {
            Csv(export.Account.FullName), Csv(export.Account.Email), Csv(export.Account.PhoneNumber), Csv(export.Account.Role),
            Csv(export.Account.CreatedAt.ToString("u")), Csv(export.CandidateProfile?.Headline), Csv(export.CandidateProfile?.City),
            Csv(export.CandidateProfile?.State), Csv(export.CandidateProfile?.ProfileVisibility),
            export.Applications.Count.ToString(), export.SavedJobs.Count.ToString(), export.SavedSearches.Count.ToString(),
            export.FollowedCompanies.Count.ToString(), export.CompanyReviews.Count.ToString(),
        };

        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', headers));
        sb.AppendLine(string.Join(',', values));
        return sb.ToString();
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }
}
