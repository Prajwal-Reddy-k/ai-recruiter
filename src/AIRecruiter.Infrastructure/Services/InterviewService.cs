using AIRecruiter.Application.DTOs.Interviews;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Interviews;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class InterviewService : IInterviewService
{
    private static readonly InterviewStatus[] ActiveStatuses = { InterviewStatus.Proposed, InterviewStatus.Scheduled };

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _auditLog;

    public InterviewService(AppDbContext db, INotificationService notifications, IAuditLogService auditLog)
    {
        _db = db;
        _notifications = notifications;
        _auditLog = auditLog;
    }

    public async Task<InterviewDto> ScheduleAsync(int recruiterUserId, int applicationId, ScheduleInterviewRequest request, CancellationToken ct = default)
    {
        var application = await LoadApplicationAsync(applicationId, ct);

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        ValidateTimes(request.StartUtc, request.EndUtc);
        await EnsureNoOverlapAsync(application.CandidateProfileId, recruiterUserId, request.StartUtc, request.EndUtc, excludeInterviewId: null, ct);

        var interview = new Interview
        {
            JobApplicationId = applicationId,
            ScheduledStartUtc = request.StartUtc,
            ScheduledEndUtc = request.EndUtc,
            Type = request.Type,
            Location = request.Location,
            RecruiterNote = request.RecruiterNote,
            Status = InterviewStatus.Proposed,
            CreatedByUserId = recruiterUserId,
        };

        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.CandidateProfile.UserId,
            "InterviewProposed",
            $"You have a proposed interview for {application.JobPosting.Title}.",
            "Interview", interview.Id, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "InterviewScheduled", "Interview", interview.Id, new { application.JobPosting.Title }, ct);

        return ToDto(interview, application, recruiterUserId);
    }

    public async Task<InterviewDto> RescheduleAsync(int recruiterUserId, int interviewId, RescheduleInterviewRequest request, CancellationToken ct = default)
    {
        var (interview, application) = await LoadInterviewAsync(interviewId, ct);

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this interview.");
        }

        if (interview.Status is InterviewStatus.Completed or InterviewStatus.Cancelled or InterviewStatus.Declined)
        {
            throw new ConflictException("INVALID_INTERVIEW_STATE", $"An interview that is {interview.Status} can no longer be rescheduled.");
        }

        ValidateTimes(request.StartUtc, request.EndUtc);
        await EnsureNoOverlapAsync(application.CandidateProfileId, recruiterUserId, request.StartUtc, request.EndUtc, excludeInterviewId: interview.Id, ct);

        interview.ScheduledStartUtc = request.StartUtc;
        interview.ScheduledEndUtc = request.EndUtc;
        if (request.Type.HasValue) interview.Type = request.Type.Value;
        if (request.Location is not null) interview.Location = request.Location;
        if (request.RecruiterNote is not null) interview.RecruiterNote = request.RecruiterNote;
        // A rescheduled time is a new proposal — the candidate needs to reconfirm it even
        // if they had already accepted the previous time.
        interview.Status = InterviewStatus.Proposed;
        interview.CandidateResponseNote = null;
        interview.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.CandidateProfile.UserId,
            "InterviewRescheduled",
            $"Your interview for {application.JobPosting.Title} was rescheduled — please confirm the new time.",
            "Interview", interview.Id, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "InterviewRescheduled", "Interview", interview.Id, new { application.JobPosting.Title }, ct);

        return ToDto(interview, application, recruiterUserId);
    }

    public async Task<InterviewDto> CancelAsync(int recruiterUserId, int interviewId, CancellationToken ct = default)
    {
        var (interview, application) = await LoadInterviewAsync(interviewId, ct);

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this interview.");
        }

        if (interview.Status is InterviewStatus.Completed or InterviewStatus.Cancelled or InterviewStatus.Declined)
        {
            throw new ConflictException("INVALID_INTERVIEW_STATE", $"An interview that is {interview.Status} can no longer be cancelled.");
        }

        interview.Status = InterviewStatus.Cancelled;
        interview.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.CandidateProfile.UserId,
            "InterviewCancelled",
            $"Your interview for {application.JobPosting.Title} was cancelled.",
            "Interview", interview.Id, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "InterviewCancelled", "Interview", interview.Id, new { application.JobPosting.Title }, ct);

        return ToDto(interview, application, recruiterUserId);
    }

    public async Task<InterviewDto> CompleteAsync(int recruiterUserId, int interviewId, CancellationToken ct = default)
    {
        var (interview, application) = await LoadInterviewAsync(interviewId, ct);

        if (!await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, recruiterUserId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct))
        {
            throw new ForbiddenException("You do not have access to this interview.");
        }

        if (interview.Status != InterviewStatus.Scheduled)
        {
            throw new ConflictException("INVALID_INTERVIEW_STATE", "Only a confirmed (Scheduled) interview can be marked completed.");
        }

        interview.Status = InterviewStatus.Completed;
        interview.UpdatedAt = DateTime.UtcNow;

        if (application.Status is ApplicationStatus.InterviewScheduled)
        {
            var from = application.Status;
            application.Status = ApplicationStatus.InterviewCompleted;
            application.UpdatedAt = DateTime.UtcNow;
            _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                JobApplicationId = application.Id,
                FromStatus = from,
                ToStatus = ApplicationStatus.InterviewCompleted,
                ChangedByUserId = recruiterUserId,
                Note = "Interview marked completed.",
            });
        }

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "InterviewCompleted", "Interview", interview.Id, new { application.JobPosting.Title }, ct);

        return ToDto(interview, application, recruiterUserId);
    }

    public async Task<InterviewDto> AcceptAsync(int candidateUserId, int interviewId, RespondInterviewRequest request, CancellationToken ct = default)
    {
        var (interview, application) = await LoadInterviewAsync(interviewId, ct);

        if (application.CandidateProfile.UserId != candidateUserId)
        {
            throw new ForbiddenException("You do not have access to this interview.");
        }

        if (interview.Status != InterviewStatus.Proposed)
        {
            throw new ConflictException("INVALID_INTERVIEW_STATE", "Only a proposed interview can be accepted.");
        }

        interview.Status = InterviewStatus.Scheduled;
        interview.CandidateResponseNote = request.ResponseNote;
        interview.UpdatedAt = DateTime.UtcNow;

        if (application.Status is ApplicationStatus.Applied or ApplicationStatus.Screening or ApplicationStatus.Shortlisted)
        {
            var from = application.Status;
            application.Status = ApplicationStatus.InterviewScheduled;
            application.UpdatedAt = DateTime.UtcNow;
            _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                JobApplicationId = application.Id,
                FromStatus = from,
                ToStatus = ApplicationStatus.InterviewScheduled,
                ChangedByUserId = candidateUserId,
                Note = "Candidate accepted the proposed interview.",
            });
        }

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.JobPosting.RecruiterProfile.UserId,
            "InterviewAccepted",
            $"{application.CandidateProfile.User?.FullName ?? "A candidate"} accepted the interview for {application.JobPosting.Title}.",
            "Interview", interview.Id, ct);

        await _auditLog.LogAsync(candidateUserId, "Candidate", "InterviewAccepted", "Interview", interview.Id, null, ct);

        return ToDto(interview, application, candidateUserId);
    }

    public async Task<InterviewDto> DeclineAsync(int candidateUserId, int interviewId, RespondInterviewRequest request, CancellationToken ct = default)
    {
        var (interview, application) = await LoadInterviewAsync(interviewId, ct);

        if (application.CandidateProfile.UserId != candidateUserId)
        {
            throw new ForbiddenException("You do not have access to this interview.");
        }

        if (interview.Status is not (InterviewStatus.Proposed or InterviewStatus.Scheduled))
        {
            throw new ConflictException("INVALID_INTERVIEW_STATE", "This interview can no longer be declined.");
        }

        interview.Status = InterviewStatus.Declined;
        interview.CandidateResponseNote = request.ResponseNote;
        interview.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.JobPosting.RecruiterProfile.UserId,
            "InterviewDeclined",
            $"The candidate declined the interview for {application.JobPosting.Title}.",
            "Interview", interview.Id, ct);

        await _auditLog.LogAsync(candidateUserId, "Candidate", "InterviewDeclined", "Interview", interview.Id, null, ct);

        return ToDto(interview, application, candidateUserId);
    }

    public async Task<IReadOnlyList<InterviewDto>> GetForApplicationAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await LoadApplicationAsync(applicationId, ct);
        EnsureCanView(application, userId, role);

        var interviews = await _db.Interviews
            .Where(i => i.JobApplicationId == applicationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return interviews.Select(i => ToDto(i, application, userId)).ToList();
    }

    public async Task<IReadOnlyList<InterviewDto>> GetMyInterviewsAsync(int userId, string role, string? statusFilter, CancellationToken ct = default)
    {
        InterviewStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<InterviewStatus>(statusFilter, ignoreCase: true, out var parsed))
        {
            parsedStatus = parsed;
        }

        var query = _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Include(i => i.JobApplication).ThenInclude(a => a.CandidateProfile).ThenInclude(c => c.User)
            .AsQueryable();

        if (role == "Recruiter")
        {
            // Company id is always derived server-side from the caller's own
            // RecruiterProfile — never accepted from the client — matching the same
            // company-wide scoping convention used by CandidateSearchService.
            var callerCompanyId = await _db.RecruiterProfiles
                .Where(r => r.UserId == userId)
                .Select(r => (int?)r.CompanyId)
                .FirstOrDefaultAsync(ct);

            query = callerCompanyId is null
                ? query.Where(_ => false)
                : query.Where(i => i.JobApplication.JobPosting.CompanyId == callerCompanyId.Value);
        }
        else
        {
            query = query.Where(i => i.JobApplication.CandidateProfile.UserId == userId);
        }

        if (parsedStatus.HasValue)
        {
            query = query.Where(i => i.Status == parsedStatus.Value);
        }

        var interviews = await query.OrderByDescending(i => i.ScheduledStartUtc).ToListAsync(ct);

        return interviews.Select(i => ToDto(i, i.JobApplication, userId)).ToList();
    }

    public async Task<IReadOnlyList<UpcomingInterviewDto>> GetUpcomingAsync(int userId, string role, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var query = _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Where(i => i.Status == InterviewStatus.Scheduled && i.ScheduledStartUtc > now);

        query = role == "Recruiter"
            ? query.Where(i => i.JobApplication.JobPosting.RecruiterProfile.UserId == userId)
            : query.Where(i => i.JobApplication.CandidateProfile.UserId == userId);

        var interviews = await query.OrderBy(i => i.ScheduledStartUtc).ToListAsync(ct);

        return interviews.Select(i => new UpcomingInterviewDto(
            i.Id,
            i.JobApplicationId,
            i.JobApplication.JobPosting.Title,
            i.JobApplication.JobPosting.Company.Name,
            i.JobApplication.CandidateProfile.User.FullName,
            i.ScheduledStartUtc,
            i.ScheduledEndUtc)).ToList();
    }

    public async Task<(string IcsContent, string FileName)> GetIcsAsync(int userId, string role, int interviewId, CancellationToken ct = default)
    {
        var (interview, application) = await LoadInterviewAsync(interviewId, ct);
        EnsureCanView(application, userId, role);

        if (interview.Status != InterviewStatus.Scheduled)
        {
            throw new ValidationException("This interview has no confirmed time slot yet.");
        }

        var job = application.JobPosting;
        var locationLine = string.IsNullOrWhiteSpace(interview.Location) ? "" : $"\n{DescribeType(interview.Type)}: {interview.Location}";
        var ics = IcsCalendarBuilder.BuildEvent(
            $"interview-{interview.Id}",
            $"Interview: {job.Title} at {job.Company.Name}",
            $"{DescribeType(interview.Type)} interview scheduled via AI Recruiter.{locationLine}",
            interview.ScheduledStartUtc,
            interview.ScheduledEndUtc);

        return (ics, $"interview-{interview.Id}.ics");
    }

    private async Task<JobApplication> LoadApplicationAsync(int applicationId, CancellationToken ct)
    {
        return await _db.JobApplications
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");
    }

    private async Task<(Interview Interview, JobApplication Application)> LoadInterviewAsync(int interviewId, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(i => i.Id == interviewId, ct)
            ?? throw new NotFoundException("Interview not found.");

        return (interview, interview.JobApplication);
    }

    private static void EnsureCanView(JobApplication application, int userId, string role)
    {
        var isOwningCandidate = role == "Candidate" && application.CandidateProfile.UserId == userId;
        var isOwningRecruiter = role == "Recruiter" && application.JobPosting.RecruiterProfile.UserId == userId;

        if (!isOwningCandidate && !isOwningRecruiter)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }
    }

    private static void ValidateTimes(DateTime startUtc, DateTime endUtc)
    {
        if (startUtc <= DateTime.UtcNow)
        {
            throw new ValidationException("The interview start time must be in the future.");
        }
        if (endUtc <= startUtc)
        {
            throw new ValidationException("The interview end time must be after the start time.");
        }
    }

    /// <summary>Prevents double-booking the same candidate or the same recruiter into two
    /// overlapping interviews. Only Proposed/Scheduled interviews count — a cancelled or
    /// declined interview no longer occupies the calendar.</summary>
    private async Task EnsureNoOverlapAsync(int candidateProfileId, int recruiterUserId, DateTime startUtc, DateTime endUtc, int? excludeInterviewId, CancellationToken ct)
    {
        var overlapping = await _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Where(i =>
                ActiveStatuses.Contains(i.Status) &&
                (excludeInterviewId == null || i.Id != excludeInterviewId.Value) &&
                i.ScheduledStartUtc < endUtc && startUtc < i.ScheduledEndUtc &&
                (i.JobApplication.CandidateProfileId == candidateProfileId || i.JobApplication.JobPosting.RecruiterProfile.UserId == recruiterUserId))
            .AnyAsync(ct);

        if (overlapping)
        {
            throw new ConflictException("INTERVIEW_OVERLAP", "This time overlaps with another interview already on the calendar for this candidate or recruiter.");
        }
    }

    private static string DescribeType(InterviewType type) => type switch
    {
        InterviewType.Online => "Online",
        InterviewType.Phone => "Phone",
        InterviewType.InPerson => "In Person",
        _ => type.ToString(),
    };

    private static InterviewDto ToDto(Interview interview, JobApplication application, int callerUserId)
    {
        var canManage = application.JobPosting.RecruiterProfile.UserId == callerUserId;

        return new InterviewDto(
            interview.Id,
            interview.JobApplicationId,
            application.JobPostingId,
            application.JobPosting.Title,
            application.JobPosting.CompanyId,
            application.JobPosting.Company.Name,
            application.CandidateProfileId,
            application.CandidateProfile.User?.FullName ?? string.Empty,
            interview.ScheduledStartUtc,
            interview.ScheduledEndUtc,
            interview.Type.ToString(),
            interview.Location,
            interview.RecruiterNote,
            interview.CandidateResponseNote,
            interview.Status.ToString(),
            canManage,
            interview.CreatedAt,
            interview.UpdatedAt);
    }
}
