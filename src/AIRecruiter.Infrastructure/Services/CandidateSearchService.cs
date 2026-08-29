using System.Text;
using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Company-wide applicant search for recruiters — deliberately scoped to every
/// job posting owned by the caller's company (not just the caller's own postings), so
/// colleagues at the same company can discover each other's applicants. The company id is
/// always derived from the caller's own RecruiterProfile, never accepted from the client.</summary>
public class CandidateSearchService : ICandidateSearchService
{
    private readonly AppDbContext _db;
    private readonly CandidateProfileService _candidateProfileService;
    private readonly IAuditLogService _auditLog;

    public CandidateSearchService(AppDbContext db, CandidateProfileService candidateProfileService, IAuditLogService auditLog)
    {
        _db = db;
        _candidateProfileService = candidateProfileService;
        _auditLog = auditLog;
    }

    public async Task<IReadOnlyList<CandidateSearchResultDto>> SearchAsync(int recruiterUserId, CandidateSearchQuery query, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        var applications = await LoadFilteredApplicationsAsync(companyId, query, ct);

        var ordered = query.Sort switch
        {
            CandidateSortOption.HighestMatchScore => applications.OrderByDescending(a => a.MatchScore ?? -1),
            CandidateSortOption.ExperienceDesc => applications.OrderByDescending(a => a.CandidateProfile.TotalExperienceYears ?? -1),
            CandidateSortOption.NameAlphabetical => applications.OrderBy(a => a.CandidateProfile.User.FullName, StringComparer.OrdinalIgnoreCase),
            _ => applications.OrderByDescending(a => a.CreatedAt),
        };

        return ordered.Select(a => ToResultDto(a, recruiterUserId)).ToList();
    }

    public async Task<CandidateSearchDetailDto> GetDetailAsync(int recruiterUserId, int candidateProfileId, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        var candidate = await _db.CandidateProfiles
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == candidateProfileId, ct)
            ?? throw new NotFoundException("Candidate not found.");

        var applications = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.Interviews).ThenInclude(i => i.Slots)
            .Where(a => a.CandidateProfileId == candidateProfileId && a.JobPosting.CompanyId == companyId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        if (applications.Count == 0)
        {
            // Either the candidate never applied to this company, or they exist but the
            // caller has no visibility into them — both cases are "not found" from here.
            throw new NotFoundException("Candidate not found.");
        }

        var appDtos = applications.Select(a => new CandidateApplicationSummaryDto(
            a.Id,
            a.JobPostingId,
            a.JobPosting.Title,
            a.Status.ToString(),
            a.CreatedAt,
            a.MatchScore.HasValue ? (int)a.MatchScore.Value : null,
            !string.IsNullOrEmpty(candidate.ResumeStorageKey),
            a.JobPosting.RecruiterProfile.UserId == recruiterUserId,
            a.Interviews.Select(i => new InterviewSummaryDto(
                i.Id,
                i.Status.ToString(),
                i.Slots.Where(s => s.IsSelected).Select(s => (DateTime?)s.StartUtc).FirstOrDefault()))
                .ToList()))
            .ToList();

        return new CandidateSearchDetailDto(
            candidate.Id,
            candidate.User.FullName,
            candidate.Headline,
            candidate.Summary,
            candidate.Education,
            candidate.ExperienceSummary,
            candidate.TotalExperienceYears,
            IndiaLocationFormatter.Format(candidate.City, candidate.State, isRemote: false),
            candidate.SkillsCsv,
            appDtos);
    }

    public async Task<string> ExportCsvAsync(int recruiterUserId, CandidateSearchQuery query, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);
        var applications = (await LoadFilteredApplicationsAsync(companyId, query, ct)).OrderByDescending(a => a.CreatedAt).ToList();

        var sb = new StringBuilder();
        sb.AppendLine("Name,Headline,Skills,City,State,ExperienceYears,Education,ApplicationStatus,AppliedDate,MatchScore,JobTitle");

        foreach (var a in applications)
        {
            var c = a.CandidateProfile;
            sb.AppendLine(string.Join(",", new[]
            {
                CsvField(c.User.FullName),
                CsvField(c.Headline),
                CsvField(c.SkillsCsv),
                CsvField(c.City),
                CsvField(c.State),
                CsvField(c.TotalExperienceYears?.ToString() ?? ""),
                CsvField(c.Education),
                CsvField(a.Status.ToString()),
                CsvField(a.CreatedAt.ToString("yyyy-MM-dd")),
                CsvField(a.MatchScore.HasValue ? ((int)a.MatchScore.Value).ToString() : ""),
                CsvField(a.JobPosting.Title),
            }));
        }

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "CandidatesExported", "Company", companyId, new { Count = applications.Count }, ct);

        return sb.ToString();
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadApplicantResumeAsync(int recruiterUserId, int applicationId, CancellationToken ct = default)
    {
        var companyId = await GetCallerCompanyIdAsync(recruiterUserId, ct);

        var application = await _db.JobApplications
            .Include(a => a.JobPosting)
            .Include(a => a.CandidateProfile)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        if (application.JobPosting.CompanyId != companyId)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        return await _candidateProfileService.OpenResumeAsync(application.CandidateProfile, ct);
    }

    private async Task<int> GetCallerCompanyIdAsync(int recruiterUserId, CancellationToken ct)
    {
        var companyId = await _db.RecruiterProfiles
            .Where(r => r.UserId == recruiterUserId)
            .Select(r => (int?)r.CompanyId)
            .FirstOrDefaultAsync(ct);

        return companyId ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");
    }

    /// <summary>Applies every DB-translatable filter, materializes, then applies the
    /// skills substring match in memory (an "any of these is a substring" match isn't
    /// reliably translatable across EF providers).</summary>
    private async Task<List<JobApplication>> LoadFilteredApplicationsAsync(int companyId, CandidateSearchQuery query, CancellationToken ct)
    {
        var applications = BuildFilteredQuery(companyId, query);
        var results = await applications.ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(query.Skills))
        {
            var skills = query.Skills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            results = results.Where(a => a.CandidateProfile.SkillsCsv != null && skills.Any(s => a.CandidateProfile.SkillsCsv.Contains(s, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        return results;
    }

    private IQueryable<JobApplication> BuildFilteredQuery(int companyId, CandidateSearchQuery query)
    {
        var applications = _db.JobApplications
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Where(a => a.JobPosting.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            applications = applications.Where(a => a.CandidateProfile.City == query.City);
        }
        if (!string.IsNullOrWhiteSpace(query.State))
        {
            applications = applications.Where(a => a.CandidateProfile.State == query.State);
        }
        if (query.MinExperienceYears.HasValue)
        {
            applications = applications.Where(a => a.CandidateProfile.TotalExperienceYears != null && a.CandidateProfile.TotalExperienceYears >= query.MinExperienceYears.Value);
        }
        if (query.MaxExperienceYears.HasValue)
        {
            applications = applications.Where(a => a.CandidateProfile.TotalExperienceYears != null && a.CandidateProfile.TotalExperienceYears <= query.MaxExperienceYears.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Education))
        {
            applications = applications.Where(a => a.CandidateProfile.Education != null && a.CandidateProfile.Education.Contains(query.Education));
        }
        if (query.Status.HasValue)
        {
            applications = applications.Where(a => a.Status == query.Status.Value);
        }
        if (query.MinMatchScore.HasValue)
        {
            applications = applications.Where(a => a.MatchScore != null && a.MatchScore >= query.MinMatchScore.Value);
        }
        if (query.MaxMatchScore.HasValue)
        {
            applications = applications.Where(a => a.MatchScore != null && a.MatchScore <= query.MaxMatchScore.Value);
        }

        return applications;
    }

    private static CandidateSearchResultDto ToResultDto(JobApplication a, int recruiterUserId) => new(
        a.Id,
        a.CandidateProfileId,
        a.CandidateProfile.User.FullName,
        a.CandidateProfile.Headline,
        a.CandidateProfile.SkillsCsv,
        a.CandidateProfile.City,
        a.CandidateProfile.State,
        a.CandidateProfile.TotalExperienceYears,
        a.CandidateProfile.Education,
        a.Status.ToString(),
        a.JobPostingId,
        a.JobPosting.Title,
        a.CreatedAt,
        a.MatchScore.HasValue ? (int)a.MatchScore.Value : null,
        a.JobPosting.RecruiterProfile.UserId == recruiterUserId);

    private static string CsvField(string? value)
    {
        value ??= string.Empty;
        var needsQuoting = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        var escaped = value.Replace("\"", "\"\"");
        return needsQuoting ? $"\"{escaped}\"" : escaped;
    }
}
