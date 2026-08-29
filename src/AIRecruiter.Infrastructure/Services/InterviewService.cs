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
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _auditLog;

    public InterviewService(AppDbContext db, INotificationService notifications, IAuditLogService auditLog)
    {
        _db = db;
        _notifications = notifications;
        _auditLog = auditLog;
    }

    public async Task<InterviewDto> ProposeAsync(int recruiterUserId, int applicationId, ProposeInterviewRequest request, CancellationToken ct = default)
    {
        var application = await LoadApplicationAsync(applicationId, ct);

        if (application.JobPosting.RecruiterProfile.UserId != recruiterUserId)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }

        if (request.Slots.Count == 0)
        {
            throw new ValidationException("Propose at least one interview slot.");
        }

        var now = DateTime.UtcNow;
        var seenStarts = new HashSet<DateTime>();
        foreach (var slot in request.Slots)
        {
            if (slot.StartUtc <= now)
            {
                throw new ValidationException("Interview slots must be in the future.");
            }
            if (slot.EndUtc <= slot.StartUtc)
            {
                throw new ValidationException("Each slot's end time must be after its start time.");
            }
            if (!seenStarts.Add(slot.StartUtc))
            {
                throw new ValidationException("Duplicate slot start times are not allowed.");
            }
        }

        var interview = new Interview
        {
            JobApplicationId = applicationId,
            Status = InterviewStatus.Proposed,
            CreatedByUserId = recruiterUserId,
            Slots = request.Slots.Select(s => new InterviewSlot { StartUtc = s.StartUtc, EndUtc = s.EndUtc }).ToList(),
        };

        _db.Interviews.Add(interview);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            application.CandidateProfile.UserId,
            "InterviewProposed",
            $"You have {request.Slots.Count} proposed interview time(s) for {application.JobPosting.Title}.",
            "Interview", interview.Id, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "InterviewProposed", "Interview", interview.Id, new { application.JobPosting.Title }, ct);

        return ToDto(interview, application);
    }

    public async Task<InterviewDto> RespondAsync(int candidateUserId, int interviewId, RespondInterviewRequest request, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .Include(i => i.Slots)
            .Include(i => i.JobApplication).ThenInclude(a => a.CandidateProfile)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(i => i.Id == interviewId, ct)
            ?? throw new NotFoundException("Interview not found.");

        if (interview.JobApplication.CandidateProfile.UserId != candidateUserId)
        {
            throw new ForbiddenException("You do not have access to this interview.");
        }

        if (request.AcceptedSlotId.HasValue)
        {
            var slot = interview.Slots.FirstOrDefault(s => s.Id == request.AcceptedSlotId.Value)
                ?? throw new ValidationException("That slot does not belong to this interview.");

            foreach (var s in interview.Slots) s.IsSelected = s.Id == slot.Id;
            interview.Status = InterviewStatus.Scheduled;

            var application = interview.JobApplication;
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
                    Note = "Candidate accepted an interview slot.",
                });
            }

            await _notifications.NotifyAsync(
                interview.JobApplication.JobPosting.RecruiterProfile.UserId,
                "InterviewScheduled",
                $"{interview.JobApplication.CandidateProfile.User?.FullName ?? "A candidate"} accepted an interview slot for {interview.JobApplication.JobPosting.Title}.",
                "Interview", interview.Id, ct);
        }
        else
        {
            interview.Status = InterviewStatus.Cancelled;
            interview.DeclineNote = request.DeclineNote;

            await _notifications.NotifyAsync(
                interview.JobApplication.JobPosting.RecruiterProfile.UserId,
                "InterviewDeclined",
                $"The candidate declined the proposed interview for {interview.JobApplication.JobPosting.Title}.",
                "Interview", interview.Id, ct);
        }

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(candidateUserId, "Candidate", "InterviewResponded", "Interview", interview.Id, new { Status = interview.Status.ToString() }, ct);

        return ToDto(interview, interview.JobApplication);
    }

    public async Task<IReadOnlyList<InterviewDto>> GetForApplicationAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await LoadApplicationAsync(applicationId, ct);
        EnsureCanView(application, userId, role);

        var interviews = await _db.Interviews
            .Include(i => i.Slots)
            .Where(i => i.JobApplicationId == applicationId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

        return interviews.Select(i => ToDto(i, application)).ToList();
    }

    public async Task<IReadOnlyList<UpcomingInterviewDto>> GetUpcomingAsync(int userId, string role, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var query = _db.Interviews
            .Include(i => i.Slots)
            .Include(i => i.JobApplication).ThenInclude(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .Where(i => i.Status == InterviewStatus.Scheduled);

        query = role == "Recruiter"
            ? query.Where(i => i.JobApplication.JobPosting.RecruiterProfile.UserId == userId)
            : query.Where(i => i.JobApplication.CandidateProfile.UserId == userId);

        var interviews = await query.ToListAsync(ct);

        return interviews
            .Select(i => (Interview: i, Slot: i.Slots.FirstOrDefault(s => s.IsSelected)))
            .Where(x => x.Slot is not null && x.Slot.StartUtc > now)
            .OrderBy(x => x.Slot!.StartUtc)
            .Select(x => new UpcomingInterviewDto(
                x.Interview.Id,
                x.Interview.JobApplicationId,
                x.Interview.JobApplication.JobPosting.Title,
                x.Interview.JobApplication.JobPosting.Company.Name,
                x.Interview.JobApplication.CandidateProfile.User.FullName,
                x.Slot!.StartUtc,
                x.Slot.EndUtc))
            .ToList();
    }

    public async Task<(string IcsContent, string FileName)> GetIcsAsync(int userId, string role, int interviewId, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .Include(i => i.Slots)
            .Include(i => i.JobApplication).ThenInclude(a => a.CandidateProfile)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile)
            .FirstOrDefaultAsync(i => i.Id == interviewId, ct)
            ?? throw new NotFoundException("Interview not found.");

        EnsureCanView(interview.JobApplication, userId, role);

        var slot = interview.Slots.FirstOrDefault(s => s.IsSelected)
            ?? throw new ValidationException("This interview has no confirmed time slot yet.");

        var job = interview.JobApplication.JobPosting;
        var ics = IcsCalendarBuilder.BuildEvent(
            $"interview-{interview.Id}",
            $"Interview: {job.Title} at {job.Company.Name}",
            "AI-assisted interview scheduled via AI Recruiter.",
            slot.StartUtc,
            slot.EndUtc);

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

    private static void EnsureCanView(JobApplication application, int userId, string role)
    {
        var isOwningCandidate = role == "Candidate" && application.CandidateProfile.UserId == userId;
        var isOwningRecruiter = role == "Recruiter" && application.JobPosting.RecruiterProfile.UserId == userId;

        if (!isOwningCandidate && !isOwningRecruiter)
        {
            throw new ForbiddenException("You do not have access to this application.");
        }
    }

    private static InterviewDto ToDto(Interview interview, JobApplication application) => new(
        interview.Id,
        interview.JobApplicationId,
        application.JobPosting.Title,
        application.JobPosting.Company.Name,
        application.CandidateProfile.User?.FullName ?? string.Empty,
        interview.Status.ToString(),
        interview.DeclineNote,
        interview.CreatedAt,
        interview.Slots.Select(s => new InterviewSlotDto(s.Id, s.StartUtc, s.EndUtc, s.IsSelected)).ToList());
}
