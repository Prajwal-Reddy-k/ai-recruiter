using AIRecruiter.Application.Common;
using AIRecruiter.Application.DTOs.Applications;
using AIRecruiter.Application.DTOs.Matching;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class JobApplicationService : IJobApplicationService
{
    /// <summary>Statuses a candidate can no longer withdraw from — mirrors the frontend's
    /// existing TERMINAL_STATUSES set, but enforced here too since a UI-only check is not a
    /// real authorization boundary.</summary>
    private static readonly ApplicationStatus[] FinalStatuses =
        { ApplicationStatus.Hired, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn };

    private const int MaxCoverNoteLength = 4000;

    private readonly AppDbContext _db;
    private readonly IResumeMatchingService _matchingService;
    private readonly CandidateProfileService _candidateProfileService;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _auditLog;

    public JobApplicationService(
        AppDbContext db,
        IResumeMatchingService matchingService,
        CandidateProfileService candidateProfileService,
        INotificationService notifications,
        IAuditLogService auditLog)
    {
        _db = db;
        _matchingService = matchingService;
        _candidateProfileService = candidateProfileService;
        _notifications = notifications;
        _auditLog = auditLog;
    }

    public async Task<JobApplicationDto> ApplyAsync(int candidateUserId, int jobPostingId, string? coverNote, CancellationToken ct = default)
    {
        if (coverNote is { Length: > MaxCoverNoteLength })
        {
            throw new ValidationException("Please fix the highlighted fields.", new Dictionary<string, string>
            {
                ["coverNote"] = $"Cover letter must be {MaxCoverNoteLength} characters or fewer.",
            });
        }

        var candidateProfile = await _db.CandidateProfiles.Include(c => c.User)
            .FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct)
            ?? throw new NotFoundException("Complete your candidate profile before applying.");

        var job = await _db.JobPostings.Include(j => j.Company).Include(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(j => j.Id == jobPostingId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (job.Status != JobStatus.Open)
        {
            throw new ConflictException("JOB_CLOSED", "This job is no longer accepting applications.");
        }

        if (job.ModerationStatus != ModerationStatus.Approved)
        {
            throw new ConflictException("JOB_CLOSED", "This job is no longer accepting applications.");
        }

        if (job.ApplicationDeadlineUtc.HasValue && job.ApplicationDeadlineUtc.Value < DateTime.UtcNow)
        {
            throw new ConflictException("JOB_EXPIRED", "The application deadline for this job has passed.");
        }

        var alreadyApplied = await _db.JobApplications.AnyAsync(
            a => a.JobPostingId == jobPostingId && a.CandidateProfileId == candidateProfile.Id, ct);
        if (alreadyApplied)
        {
            throw new ConflictException("ALREADY_APPLIED", "You have already applied to this job.");
        }

        var application = new JobApplication
        {
            JobPostingId = job.Id,
            CandidateProfileId = candidateProfile.Id,
            CoverNote = coverNote?.Trim(),
            Status = ApplicationStatus.Applied,
        };

        if (!string.IsNullOrWhiteSpace(candidateProfile.ResumeExtractedText))
        {
            var jobInput = new JobMatchInput(job.Title, job.Description, job.RequiredSkillsCsv, job.MinExperienceYears);
            var candidateInput = new CandidateMatchInput(candidateProfile.TotalExperienceYears, candidateProfile.Education);

            var match = _matchingService.CalculateMatch(candidateProfile.ResumeExtractedText, jobInput, candidateInput);

            application.MatchScore = match.OverallScore;
            application.MatchedSkillsCsv = string.Join(", ", match.MatchedSkills);
            application.MissingSkillsCsv = string.Join(", ", match.MissingSkills);
            application.SuggestedImprovements = string.Join(" | ", match.SuggestedImprovements);
            application.ScoringExplanation = match.Explanation;
        }
        else
        {
            application.ScoringExplanation = "No resume was on file at the time of application, so a match score could not be calculated.";
        }

        _db.JobApplications.Add(application);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Unique index (JobPostingId, CandidateProfileId) catches a race between the
            // pre-check above and this insert.
            var stillDuplicate = await _db.JobApplications.AnyAsync(
                a => a.JobPostingId == jobPostingId && a.CandidateProfileId == candidateProfile.Id && a.Id != application.Id, ct);
            if (stillDuplicate)
            {
                throw new ConflictException("ALREADY_APPLIED", "You have already applied to this job.");
            }
            throw;
        }

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            FromStatus = null,
            ToStatus = ApplicationStatus.Applied,
            ChangedByUserId = candidateUserId,
        });
        await _db.SaveChangesAsync(ct);

        var referral = await _db.Referrals.FirstOrDefaultAsync(
            r => r.RegisteredUserId == candidateUserId && r.JobPostingId == jobPostingId && r.Status == ReferralStatus.Registered, ct);
        if (referral is not null)
        {
            referral.JobApplicationId = application.Id;
            referral.AppliedAtUtc = DateTime.UtcNow;
            referral.Status = ReferralStatus.Applied;
            await _db.SaveChangesAsync(ct);
            await _notifications.NotifyAsync(
                referral.ReferrerUserId, "ReferralApplied",
                $"{candidateProfile.User.FullName} (your referral) applied to {job.Title}.", "Referral", referral.Id, ct);
        }

        await _notifications.NotifyAsync(
            job.RecruiterProfile.UserId,
            "ApplicationReceived",
            $"{candidateProfile.User.FullName} applied to {job.Title}.",
            "JobApplication", application.Id, ct);

        await _auditLog.LogAsync(candidateUserId, "Candidate", "ApplicationSubmitted", "JobApplication", application.Id, new { job.Title }, ct);

        return ToDto(application, job.Title, job.Company.Name);
    }

    public async Task<IReadOnlyList<JobApplicationDto>> GetMyApplicationsAsync(int candidateUserId, CancellationToken ct = default)
    {
        var applications = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.CandidateProfile)
            .Where(a => a.CandidateProfile.UserId == candidateUserId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        var applicationIds = applications.Select(a => a.Id).ToList();
        var activeInterviewStatuses = new[] { InterviewStatus.Proposed, InterviewStatus.Scheduled };
        var nextInterviewByApp = await _db.Interviews
            .Where(i => applicationIds.Contains(i.JobApplicationId) && activeInterviewStatuses.Contains(i.Status))
            .GroupBy(i => i.JobApplicationId)
            .Select(g => new { JobApplicationId = g.Key, NextStart = g.Min(i => i.ScheduledStartUtc) })
            .ToDictionaryAsync(x => x.JobApplicationId, x => (DateTime?)x.NextStart, ct);

        return applications
            .Select(a => ToDto(
                a, a.JobPosting.Title, a.JobPosting.Company.Name,
                jobLocation: IndiaLocationFormatter.Format(a.JobPosting.City, a.JobPosting.State, a.JobPosting.IsRemote),
                nextInterviewAtUtc: nextInterviewByApp.GetValueOrDefault(a.Id)))
            .ToList();
    }

    public async Task<JobApplicationDetailDto> GetApplicationDetailAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(a => a.StatusHistory).ThenInclude(h => h.ChangedByUser)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        await EnsureCanViewApplicationAsync(application, userId, role, ct);

        return ToDetailDto(application);
    }

    public async Task<IReadOnlyList<JobApplicationDto>> GetApplicationsForJobAsync(int recruiterUserId, int jobPostingId, CancellationToken ct = default)
    {
        var job = await _db.JobPostings.Include(j => j.RecruiterProfile).Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == jobPostingId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, job.RecruiterProfile.UserId, job.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this job's applicants.");
        }

        var applications = await _db.JobApplications
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Where(a => a.JobPostingId == jobPostingId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return applications.Select(a => ToDto(a, job.Title, job.Company.Name, a.CandidateProfile.User.FullName)).ToList();
    }

    public async Task<(Stream Content, string FileName, string ContentType)> DownloadApplicantResumeAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        await EnsureCanViewApplicationAsync(application, userId, role, ct);

        return await _candidateProfileService.OpenResumeAsync(application.CandidateProfile, ct);
    }

    public async Task<JobApplicationDto> UpdateStatusAsync(int recruiterUserId, int applicationId, ApplicationStatus newStatus, string? note = null, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        var previousStatus = application.Status;
        application.Status = newStatus;
        application.UpdatedAt = DateTime.UtcNow;

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            FromStatus = previousStatus,
            ToStatus = newStatus,
            ChangedByUserId = recruiterUserId,
            Note = note,
        });

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.CandidateProfile.UserId,
            "ApplicationStatusChanged",
            $"Your application for {application.JobPosting.Title} is now: {newStatus}.",
            "JobApplication", application.Id, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "ApplicationStatusChanged", "JobApplication", application.Id, new { From = previousStatus.ToString(), To = newStatus.ToString() }, ct);

        return ToDto(application, application.JobPosting.Title, application.JobPosting.Company.Name, application.CandidateProfile.User.FullName);
    }

    public async Task<JobApplicationDto> WithdrawAsync(int candidateUserId, int applicationId, CancellationToken ct = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(a => a.CandidateProfile)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");

        if (application.CandidateProfile.UserId != candidateUserId)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        if (FinalStatuses.Contains(application.Status))
        {
            throw new ConflictException("APPLICATION_FINAL", $"This application is already {application.Status} and can no longer be withdrawn.");
        }

        var previousStatus = application.Status;
        application.Status = ApplicationStatus.Withdrawn;
        application.UpdatedAt = DateTime.UtcNow;

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            JobApplicationId = application.Id,
            FromStatus = previousStatus,
            ToStatus = ApplicationStatus.Withdrawn,
            ChangedByUserId = candidateUserId,
        });

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.JobPosting.RecruiterProfile.UserId,
            "ApplicationWithdrawn",
            $"An applicant withdrew their application for {application.JobPosting.Title}.",
            "JobApplication", application.Id, ct);

        return ToDto(application, application.JobPosting.Title, application.JobPosting.Company.Name);
    }

    private async Task EnsureCanViewApplicationAsync(JobApplication application, int userId, string role, CancellationToken ct)
    {
        var isOwningCandidate = role == "Candidate" && application.CandidateProfile.UserId == userId;
        var isOwningRecruiter = role == "Recruiter" && await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(
            _db, userId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct);

        if (!isOwningCandidate && !isOwningRecruiter)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }
    }

    private static JobApplicationDto ToDto(
        JobApplication a, string jobTitle, string companyName, string? candidateFullName = null,
        string? jobLocation = null, DateTime? nextInterviewAtUtc = null) => new(
        a.Id,
        a.JobPostingId,
        jobTitle,
        companyName,
        candidateFullName,
        a.CandidateProfile?.Headline,
        a.CandidateProfile?.SkillsCsv,
        a.Status.ToString(),
        a.CreatedAt,
        a.UpdatedAt,
        a.MatchScore.HasValue ? (int)a.MatchScore.Value : null,
        jobLocation,
        nextInterviewAtUtc,
        a.CandidateProfile is null ? null : AvatarUrlFormatter.Format(a.CandidateProfile.Id, a.CandidateProfile.AvatarStorageKey),
        a.CandidateProfileId);

    private static JobApplicationDetailDto ToDetailDto(JobApplication a) => new(
        a.Id,
        a.JobPostingId,
        a.JobPosting.Title,
        a.JobPosting.Company.Name,
        a.CandidateProfileId,
        a.CandidateProfile.User.FullName,
        a.Status.ToString(),
        a.CoverNote,
        a.CreatedAt,
        a.UpdatedAt,
        a.MatchScore.HasValue ? (int)a.MatchScore.Value : null,
        SplitCsv(a.MatchedSkillsCsv),
        SplitCsv(a.MissingSkillsCsv),
        SplitList(a.SuggestedImprovements),
        a.ScoringExplanation,
        a.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new StatusHistoryEntryDto(
                h.FromStatus?.ToString(),
                h.ToStatus.ToString(),
                h.ChangedByUser?.FullName ?? "System",
                h.ChangedAt,
                h.Note))
            .ToList(),
        ComputeNextAction(a.Status));

    /// <summary>Plain-language "what happens next" guidance per status — static, no AI.</summary>
    private static string ComputeNextAction(ApplicationStatus status) => status switch
    {
        ApplicationStatus.Applied => "Awaiting recruiter review.",
        ApplicationStatus.Screening => "Your application is being screened.",
        ApplicationStatus.Shortlisted => "You've been shortlisted — an interview may be scheduled soon.",
        ApplicationStatus.InterviewScheduled => "Interview scheduled — check your email/notifications for details.",
        ApplicationStatus.InterviewCompleted => "Interview completed — awaiting a decision.",
        ApplicationStatus.Offer => "Offer extended — respond soon.",
        ApplicationStatus.Hired => "Congratulations — you were hired for this role.",
        ApplicationStatus.Rejected => "This application was not successful.",
        ApplicationStatus.Withdrawn => "You withdrew this application.",
        _ => "No action needed right now.",
    };

    private static IReadOnlyList<string> SplitCsv(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? Array.Empty<string>()
            : csv.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IReadOnlyList<string> SplitList(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(" | ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
