using AIRecruiter.Application.DTOs.Analytics;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Matching;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _db;

    public AnalyticsService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RecruiterAnalyticsDto> GetRecruiterAnalyticsAsync(int recruiterUserId, CancellationToken ct = default)
    {
        var jobs = await _db.JobPostings
            .Where(j => j.RecruiterProfile.UserId == recruiterUserId)
            .ToListAsync(ct);

        var jobIds = jobs.Select(j => j.Id).ToList();

        var applications = await _db.JobApplications
            .Include(a => a.CandidateProfile)
            .Include(a => a.JobPosting)
            .Where(a => jobIds.Contains(a.JobPostingId))
            .ToListAsync(ct);

        var applicationsPerJob = jobs
            .Select(j => new NamedCountDto(j.Title, applications.Count(a => a.JobPostingId == j.Id)))
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        var hiringFunnel = Enum.GetValues<ApplicationStatus>()
            .Select(status => new NamedCountDto(status.ToString(), applications.Count(a => a.Status == status)))
            .ToList();

        var applicationsByCity = applications
            .GroupBy(a => a.JobPosting.IsRemote ? "Remote — India" : (a.JobPosting.City ?? "Unspecified"))
            .Select(g => new NamedCountDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var skillCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var app in applications)
        {
            foreach (var skill in SkillTaxonomy.ParseCsv(app.CandidateProfile.SkillsCsv))
            {
                skillCounts[skill] = skillCounts.GetValueOrDefault(skill, 0) + 1;
            }
        }
        var topSkills = skillCounts
            .OrderByDescending(kv => kv.Value)
            .Take(10)
            .Select(kv => new NamedCountDto(kv.Key, kv.Value))
            .ToList();

        var viewsVsApplications = jobs
            .Select(j => new JobViewsVsApplicationsDto(j.Id, j.Title, j.ViewCount, applications.Count(a => a.JobPostingId == j.Id), j.ShareCount))
            .OrderByDescending(x => x.ViewCount)
            .ToList();

        return new RecruiterAnalyticsDto(
            ActiveJobs: jobs.Count(j => j.Status == JobStatus.Open),
            TotalApplications: applications.Count,
            ShortlistedCandidates: applications.Count(a => a.Status == ApplicationStatus.Shortlisted),
            InterviewsScheduled: applications.Count(a => a.Status == ApplicationStatus.InterviewScheduled),
            OffersMade: applications.Count(a => a.Status == ApplicationStatus.Offer || a.Status == ApplicationStatus.Hired),
            ApplicationsPerJob: applicationsPerJob,
            HiringFunnel: hiringFunnel,
            ApplicationsByCity: applicationsByCity,
            TopCandidateSkills: topSkills,
            ViewsVsApplications: viewsVsApplications);
    }
}
