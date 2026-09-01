using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Application.DTOs.Candidates;
using AIRecruiter.Application.DTOs.Dashboard;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Matching;
using AIRecruiter.Application.Validation;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Mapping;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly IRecruiterOnboardingService _onboardingService;
    private readonly ISavedJobService _savedJobService;
    private readonly IInterviewService _interviewService;
    private readonly IJobAlertService _jobAlertService;

    public DashboardService(AppDbContext db, IRecruiterOnboardingService onboardingService, ISavedJobService savedJobService, IInterviewService interviewService, IJobAlertService jobAlertService)
    {
        _db = db;
        _onboardingService = onboardingService;
        _savedJobService = savedJobService;
        _interviewService = interviewService;
        _jobAlertService = jobAlertService;
    }

    public async Task<CandidateDashboardDto> GetCandidateDashboardAsync(int userId, CancellationToken ct = default)
    {
        var profile = await _db.CandidateProfiles
            .Include(c => c.WorkExperiences)
            .Include(c => c.ResumeEducations)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct)
            ?? throw new Application.Exceptions.NotFoundException("Candidate profile not found.");

        var applications = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Where(a => a.CandidateProfileId == profile.Id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var underReviewStatuses = new[] { ApplicationStatus.Screening, ApplicationStatus.InterviewScheduled, ApplicationStatus.InterviewCompleted };
        var summary = new ApplicationStatusSummaryDto(
            applications.Count(a => a.Status == ApplicationStatus.Applied),
            applications.Count(a => underReviewStatuses.Contains(a.Status)),
            applications.Count(a => a.Status == ApplicationStatus.Shortlisted),
            applications.Count(a => a.Status == ApplicationStatus.Rejected));

        var recentApplications = applications.Take(5).Select(ToApplicationDto).ToList();

        var openJobs = await _db.JobPostings
            .Include(j => j.Company)
            .Where(j => j.Status == JobStatus.Open && j.ModerationStatus == ModerationStatus.Approved)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

        var candidateSkills = SkillTaxonomy.ParseCsv(profile.SkillsCsv)
            .Select(SkillTaxonomy.Normalize)
            .ToHashSet();

        var preferredJobTypes = SkillTaxonomy.ParseCsv(profile.PreferredJobTypesCsv).Select(SkillTaxonomy.Normalize).ToHashSet();
        var preferredLocations = SkillTaxonomy.ParseCsv(profile.PreferredLocationsCsv).Select(SkillTaxonomy.Normalize).ToHashSet();
        var preferredRoles = SkillTaxonomy.ParseCsv(profile.PreferredRolesCsv).Select(SkillTaxonomy.Normalize).ToHashSet();
        var hasPreferences = preferredJobTypes.Count > 0 || preferredLocations.Count > 0 || preferredRoles.Count > 0 || profile.RemotePreference.HasValue;

        var recommended = candidateSkills.Count == 0 && !hasPreferences
            ? openJobs.Take(3).ToList()
            : openJobs
                .Select(j => (Job: j, Score: ScoreJobForCandidate(j, candidateSkills, preferredJobTypes, preferredLocations, preferredRoles, profile.RemotePreference)))
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Job.CreatedAt)
                .Take(3)
                .Select(x => x.Job)
                .ToList();

        var missingSkills = openJobs
            .SelectMany(j => SkillTaxonomy.ParseCsv(j.RequiredSkillsCsv))
            .Where(s => !candidateSkills.Contains(SkillTaxonomy.Normalize(s)))
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(5)
            .ToList();

        // "Recently viewed" has no tracking concept in the domain — present a couple of open
        // jobs so the section isn't empty, clearly flagged as sample data. Saved jobs are real.
        var recommendedIds = recommended.Select(j => j.Id).ToHashSet();
        var remaining = openJobs.Where(j => !recommendedIds.Contains(j.Id)).ToList();
        var recentlyViewed = remaining.Count > 0 ? remaining.Take(2).ToList() : openJobs.Take(2).ToList();

        var savedJobs = await _savedJobService.GetMySavedJobsAsync(userId, ct);
        var alertCount = await _db.JobAlerts.CountAsync(a => a.CandidateProfile.UserId == userId, ct);
        var alertMatches = await _jobAlertService.GetMatchingJobsAsync(userId, 6, ct);
        var upcomingInterviews = await _interviewService.GetUpcomingAsync(userId, "Candidate", ct);
        var pendingInvitationCount = await _db.Invitations.CountAsync(
            i => i.CandidateProfileId == profile.Id && (i.Status == InvitationStatus.Sent || i.Status == InvitationStatus.Viewed), ct);

        var strength = ProfileStrengthCalculator.Calculate(BuildStrengthInput(profile));
        var nextBestActions = BuildNextBestActions(strength, recommended.Count > 0, upcomingInterviews, pendingInvitationCount);

        return new CandidateDashboardDto(
            strength.Score,
            summary,
            recentApplications,
            recommended.Select(JobPostingMapper.ToDto).ToList(),
            missingSkills,
            new DemoJobsSectionDto(recentlyViewed.Select(JobPostingMapper.ToDto).ToList(), true),
            savedJobs,
            alertCount,
            alertMatches,
            upcomingInterviews,
            nextBestActions);
    }

    /// <summary>Skill-overlap count (the original, still-dominant scoring signal) plus a
    /// small additive bonus for matching the candidate's stated preferences — a location or
    /// remote-preference match, a job-type match, and a preferred-role title match. Never
    /// overrides skill relevance, just tie-breaks toward jobs that also fit how/where/what
    /// role the candidate wants.</summary>
    private static int ScoreJobForCandidate(
        JobPosting job, HashSet<string> candidateSkills, HashSet<string> preferredJobTypes,
        HashSet<string> preferredLocations, HashSet<string> preferredRoles, bool? remotePreference)
    {
        var score = SkillTaxonomy.ParseCsv(job.RequiredSkillsCsv).Count(s => candidateSkills.Contains(SkillTaxonomy.Normalize(s)));

        if (remotePreference == true && job.IsRemote)
        {
            score += 1;
        }
        else if (preferredLocations.Count > 0 &&
            ((job.City is not null && preferredLocations.Contains(SkillTaxonomy.Normalize(job.City))) ||
             (job.State is not null && preferredLocations.Contains(SkillTaxonomy.Normalize(job.State)))))
        {
            score += 1;
        }

        if (preferredJobTypes.Contains(SkillTaxonomy.Normalize(job.JobType.ToString())))
        {
            score += 1;
        }

        if (preferredRoles.Count > 0)
        {
            var normalizedTitle = SkillTaxonomy.Normalize(job.Title);
            if (preferredRoles.Any(r => normalizedTitle.Contains(r) || r.Contains(normalizedTitle)))
            {
                score += 1;
            }
        }

        return score;
    }

    private static ProfileStrengthInput BuildStrengthInput(CandidateProfile p) => new(
        HasHeadline: !string.IsNullOrWhiteSpace(p.Headline),
        HasSummary: !string.IsNullOrWhiteSpace(p.Summary),
        HasSkills: SkillTaxonomy.ParseCsv(p.SkillsCsv).Count >= 3,
        HasAvatar: !string.IsNullOrEmpty(p.AvatarStorageKey),
        HasResumeFile: !string.IsNullOrEmpty(p.ResumeStorageKey),
        HasWorkExperience: p.WorkExperiences.Count > 0,
        HasEducation: p.ResumeEducations.Count > 0 || !string.IsNullOrWhiteSpace(p.Education),
        HasLinks: !string.IsNullOrWhiteSpace(p.LinkedInUrl) || !string.IsNullOrWhiteSpace(p.GithubUrl) || !string.IsNullOrWhiteSpace(p.PortfolioUrl),
        HasPreferences: p.RemotePreference.HasValue || !string.IsNullOrWhiteSpace(p.PreferredJobTypesCsv) || !string.IsNullOrWhiteSpace(p.PreferredLocationsCsv));

    /// <summary>Capped, priority-ordered list derived entirely from data already gathered in
    /// GetCandidateDashboardAsync — no extra queries beyond the one cheap invitation count.</summary>
    private static List<NextBestActionDto> BuildNextBestActions(
        ProfileStrengthResult strength, bool hasRecommendedJobs,
        IReadOnlyList<Application.DTOs.Interviews.UpcomingInterviewDto> upcomingInterviews, int pendingInvitationCount)
    {
        var actions = new List<NextBestActionDto>();

        if (strength.Score < 100 && strength.MissingItems.Count > 0)
        {
            var top = strength.MissingItems[0];
            actions.Add(new NextBestActionDto(top.Label, top.Tip, top.LinkPath));
        }

        if (pendingInvitationCount > 0)
        {
            actions.Add(new NextBestActionDto(
                "Review recruiter invitation",
                $"You have {pendingInvitationCount} pending invitation{(pendingInvitationCount == 1 ? "" : "s")} to apply.",
                "/candidate/dashboard"));
        }

        if (upcomingInterviews.Count > 0)
        {
            actions.Add(new NextBestActionDto(
                "Respond to your interview",
                "You have an upcoming interview — confirm the details.",
                "/interviews"));
        }

        if (hasRecommendedJobs)
        {
            actions.Add(new NextBestActionDto(
                "Apply to recommended jobs",
                "We've found open roles that match your skills and preferences.",
                "/jobs"));
        }

        return actions.Take(4).ToList();
    }

    public async Task<RecruiterDashboardDto> GetRecruiterDashboardAsync(int userId, CancellationToken ct = default)
    {
        var onboardingStatus = await _onboardingService.GetStatusAsync(userId, ct);

        var jobs = await _db.JobPostings
            .Include(j => j.Company)
            .Include(j => j.RecruiterProfile)
            .Where(j => j.RecruiterProfile.UserId == userId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

        var jobIds = jobs.Select(j => j.Id).ToList();

        var applicantCounts = await _db.JobApplications
            .Where(a => jobIds.Contains(a.JobPostingId))
            .GroupBy(a => a.JobPostingId)
            .Select(g => new { JobPostingId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var applicantCountByJob = applicantCounts.ToDictionary(x => x.JobPostingId, x => x.Count);

        var recentApplications = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Where(a => jobIds.Contains(a.JobPostingId))
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        var jobPerformance = jobs.Select(j => new JobPerformanceDto(
            j.Id,
            j.Title,
            j.Status.ToString(),
            applicantCountByJob.GetValueOrDefault(j.Id, 0),
            j.ViewCount,
            j.CreatedAt)).ToList();

        var upcomingInterviews = await _interviewService.GetUpcomingAsync(userId, "Recruiter", ct);

        return new RecruiterDashboardDto(
            onboardingStatus,
            jobs.Count(j => j.Status == JobStatus.Open),
            applicantCounts.Sum(x => x.Count),
            recentApplications.Select(ToApplicationDto).ToList(),
            jobPerformance,
            upcomingInterviews);
    }

    private static JobApplicationDto ToApplicationDto(JobApplication a) => new(
        a.Id,
        a.JobPostingId,
        a.JobPosting.Title,
        a.JobPosting.Company.Name,
        a.CandidateProfile?.User?.FullName,
        a.CandidateProfile?.Headline,
        a.CandidateProfile?.SkillsCsv,
        a.Status.ToString(),
        a.CreatedAt,
        a.UpdatedAt,
        a.MatchScore.HasValue ? (int)a.MatchScore.Value : null);
}
