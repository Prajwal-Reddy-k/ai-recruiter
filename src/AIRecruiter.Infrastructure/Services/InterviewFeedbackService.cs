using System.Text.Json;
using AIRecruiter.Application.DTOs.InterviewFeedback;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Structured recruiter interview feedback, never exposed to candidates. A draft can
/// be freely edited; once submitted, any further edit first writes a snapshot to
/// InterviewFeedbackEditHistory — that snapshot-before-overwrite is the "clearly recorded
/// edit history" requirement, applied without a full versioning UI.</summary>
public class InterviewFeedbackService : IInterviewFeedbackService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;

    public InterviewFeedbackService(AppDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    public Task<InterviewFeedbackDto> SaveDraftAsync(int userId, int interviewId, UpsertInterviewFeedbackRequest request, CancellationToken ct = default) =>
        UpsertAsync(userId, interviewId, request, submit: false, ct);

    public Task<InterviewFeedbackDto> SubmitAsync(int userId, int interviewId, UpsertInterviewFeedbackRequest request, CancellationToken ct = default) =>
        UpsertAsync(userId, interviewId, request, submit: true, ct);

    private async Task<InterviewFeedbackDto> UpsertAsync(int userId, int interviewId, UpsertInterviewFeedbackRequest request, bool submit, CancellationToken ct)
    {
        ValidateScores(request);

        var (interview, recruiterProfile) = await LoadForSubmitAsync(userId, interviewId, ct);

        var feedback = await _db.InterviewFeedbacks
            .Include(f => f.RecruiterProfile).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(f => f.InterviewId == interviewId && f.RecruiterProfileId == recruiterProfile.Id, ct);

        if (feedback is null)
        {
            feedback = new InterviewFeedback
            {
                InterviewId = interviewId,
                RecruiterProfileId = recruiterProfile.Id,
                RecruiterProfile = recruiterProfile,
            };
            _db.InterviewFeedbacks.Add(feedback);
        }
        else if (!feedback.IsDraft)
        {
            // Already submitted — record what it looked like before this edit.
            _db.InterviewFeedbackEditHistories.Add(new InterviewFeedbackEditHistory
            {
                InterviewFeedbackId = feedback.Id,
                EditedByUserId = userId,
                PreviousValuesJson = JsonSerializer.Serialize(new
                {
                    feedback.TechnicalScore,
                    feedback.CommunicationScore,
                    feedback.ProblemSolvingScore,
                    feedback.CultureFitScore,
                    Recommendation = feedback.Recommendation.ToString(),
                    feedback.Strengths,
                    feedback.Concerns,
                    feedback.PrivateNotes,
                }),
            });
        }

        feedback.TechnicalScore = request.TechnicalScore;
        feedback.CommunicationScore = request.CommunicationScore;
        feedback.ProblemSolvingScore = request.ProblemSolvingScore;
        feedback.CultureFitScore = request.CultureFitScore;
        feedback.Recommendation = request.Recommendation;
        feedback.Strengths = request.Strengths;
        feedback.Concerns = request.Concerns;
        feedback.PrivateNotes = request.PrivateNotes;
        feedback.UpdatedAt = DateTime.UtcNow;

        var wasAlreadySubmitted = !feedback.IsDraft && feedback.SubmittedAtUtc is not null && feedback.Id != 0 && submit;
        if (submit)
        {
            feedback.IsDraft = false;
            feedback.SubmittedAtUtc ??= DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(userId, "Recruiter",
            submit ? (wasAlreadySubmitted ? "InterviewFeedbackEdited" : "InterviewFeedbackSubmitted") : "InterviewFeedbackDraftSaved",
            "Interview", interviewId, new { interview.JobApplication.JobPosting.Title }, ct);

        return ToDto(feedback, userId);
    }

    public async Task<InterviewFeedbackDto?> GetMyFeedbackAsync(int userId, int interviewId, CancellationToken ct = default)
    {
        var (_, recruiterProfile) = await LoadForSubmitAsync(userId, interviewId, ct);

        var feedback = await _db.InterviewFeedbacks
            .Include(f => f.RecruiterProfile).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(f => f.InterviewId == interviewId && f.RecruiterProfileId == recruiterProfile.Id, ct);

        return feedback is null ? null : ToDto(feedback, userId);
    }

    public async Task<InterviewFeedbackSummaryDto> GetSummaryAsync(int userId, int interviewId, CancellationToken ct = default)
    {
        var interview = await _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting)
            .FirstOrDefaultAsync(i => i.Id == interviewId, ct)
            ?? throw new NotFoundException("Interview not found.");

        var job = interview.JobApplication.JobPosting;
        var recruiterProfile = await _db.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == userId, ct);

        // IsOwningRecruiterAsync already covers "owns the job OR is the company Owner";
        // IsAssignedAsync additionally covers a HiringManager/Interviewer explicitly
        // assigned to this job or interview.
        var isJobOwnerOrCompanyOwner = await IsOwningRecruiterAsync(userId, job, ct);
        var isAssigned = recruiterProfile is not null && await IsAssignedAsync(recruiterProfile.Id, job.Id, interviewId, ct);

        if (!isJobOwnerOrCompanyOwner && !isAssigned)
        {
            throw new ForbiddenException("You do not have access to this interview's feedback.");
        }

        var scorecards = await _db.InterviewFeedbacks
            .Include(f => f.RecruiterProfile).ThenInclude(r => r.User)
            .Where(f => f.InterviewId == interviewId && !f.IsDraft)
            .ToListAsync(ct);

        double? Avg(Func<InterviewFeedback, int> selector) =>
            scorecards.Count == 0 ? null : scorecards.Average(f => selector(f));

        return new InterviewFeedbackSummaryDto(
            interviewId,
            scorecards.Select(f => ToDto(f, userId)).ToList(),
            Avg(f => f.TechnicalScore),
            Avg(f => f.CommunicationScore),
            Avg(f => f.ProblemSolvingScore),
            Avg(f => f.CultureFitScore));
    }

    private async Task<(Interview Interview, RecruiterProfile RecruiterProfile)> LoadForSubmitAsync(int userId, int interviewId, CancellationToken ct)
    {
        var interview = await _db.Interviews
            .Include(i => i.JobApplication).ThenInclude(a => a.JobPosting)
            .FirstOrDefaultAsync(i => i.Id == interviewId, ct)
            ?? throw new NotFoundException("Interview not found.");

        var recruiterProfile = await _db.RecruiterProfiles.Include(r => r.User).FirstOrDefaultAsync(r => r.UserId == userId, ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");

        var job = interview.JobApplication.JobPosting;
        if (recruiterProfile.CompanyId != job.CompanyId)
        {
            throw new ForbiddenException("You do not have access to this interview.");
        }

        var isOwner = await IsOwningRecruiterAsync(userId, job, ct);
        var isAssigned = await IsAssignedAsync(recruiterProfile.Id, job.Id, interviewId, ct);

        if (!isOwner && !isAssigned)
        {
            throw new ForbiddenException("Only the owning recruiter or an assigned interviewer can submit feedback for this interview.");
        }

        return (interview, recruiterProfile);
    }

    private async Task<bool> IsOwningRecruiterAsync(int userId, JobPosting job, CancellationToken ct)
    {
        var owningRecruiterUserId = await _db.RecruiterProfiles
            .Where(r => r.Id == job.RecruiterProfileId)
            .Select(r => r.UserId)
            .FirstOrDefaultAsync(ct);

        return await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(_db, userId, owningRecruiterUserId, job.CompanyId, ct);
    }

    private async Task<bool> IsAssignedAsync(int recruiterProfileId, int jobId, int interviewId, CancellationToken ct)
    {
        var jobAssigned = await _db.JobAssignments.AnyAsync(a => a.JobPostingId == jobId && a.RecruiterProfileId == recruiterProfileId, ct);
        if (jobAssigned) return true;

        return await _db.InterviewAssignments.AnyAsync(a => a.InterviewId == interviewId && a.RecruiterProfileId == recruiterProfileId, ct);
    }

    private static void ValidateScores(UpsertInterviewFeedbackRequest request)
    {
        var errors = new Dictionary<string, string>();
        void CheckScore(string field, int value)
        {
            if (value < 1 || value > 5)
            {
                errors[field] = "Score must be between 1 and 5.";
            }
        }

        CheckScore("technicalScore", request.TechnicalScore);
        CheckScore("communicationScore", request.CommunicationScore);
        CheckScore("problemSolvingScore", request.ProblemSolvingScore);
        CheckScore("cultureFitScore", request.CultureFitScore);

        if (errors.Count > 0)
        {
            throw new ValidationException("Scores must be between 1 and 5.", errors);
        }
    }

    private static InterviewFeedbackDto ToDto(InterviewFeedback f, int callerUserId) => new(
        f.Id,
        f.InterviewId,
        f.RecruiterProfileId,
        f.RecruiterProfile.User.FullName,
        f.TechnicalScore,
        f.CommunicationScore,
        f.ProblemSolvingScore,
        f.CultureFitScore,
        f.Recommendation.ToString(),
        f.Strengths,
        f.Concerns,
        f.PrivateNotes,
        f.IsDraft,
        f.SubmittedAtUtc,
        f.CreatedAt,
        f.UpdatedAt,
        CanEdit: f.RecruiterProfile.UserId == callerUserId,
        IsMine: f.RecruiterProfile.UserId == callerUserId);
}
