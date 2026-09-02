using AIRecruiter.Application.DTOs.Invitations;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Recruiter invites a candidate to apply — never creates a JobApplication itself
/// (accepting just takes the candidate to the job so they apply normally, keeping the
/// existing apply flow — resume matching, status history — completely untouched). Privacy is
/// enforced at invite time: a candidate must be either VisibleToRecruiters (discoverable) or
/// already an applicant at the caller's company (VisibleAfterApplying); Private candidates
/// can never be invited.</summary>
public class InvitationService : IInvitationService
{
    private const int MaxMessageLength = 2000;
    private const int MaxInvitesPerWindow = 20;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(14);
    private static readonly InvitationStatus[] ActiveStatuses = { InvitationStatus.Sent, InvitationStatus.Viewed };

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _auditLog;
    private readonly IIpRateLimiter _rateLimiter;

    public InvitationService(AppDbContext db, INotificationService notifications, IAuditLogService auditLog, IIpRateLimiter rateLimiter)
    {
        _db = db;
        _notifications = notifications;
        _auditLog = auditLog;
        _rateLimiter = rateLimiter;
    }

    public async Task<InvitationDto> InviteAsync(int recruiterUserId, string ipAddress, InviteCandidateRequest request, CancellationToken ct = default)
    {
        if (request.Message is not null && request.Message.Length > MaxMessageLength)
        {
            throw new ValidationException($"Message must be {MaxMessageLength} characters or fewer.",
                new Dictionary<string, string> { ["message"] = $"Message must be {MaxMessageLength} characters or fewer." });
        }

        if (!_rateLimiter.IsAllowed($"InviteCandidate:{recruiterUserId}", MaxInvitesPerWindow, RateLimitWindow)
            || !_rateLimiter.IsAllowed($"InviteCandidate:ip:{ipAddress}", MaxInvitesPerWindow * 2, RateLimitWindow))
        {
            throw new RateLimitedException("You're sending invitations too quickly. Please wait a moment and try again.", retryAfterSeconds: 60);
        }

        var recruiterProfile = await _db.RecruiterProfiles.Include(r => r.User).FirstOrDefaultAsync(r => r.UserId == recruiterUserId, ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");

        var job = await _db.JobPostings.Include(j => j.Company).FirstOrDefaultAsync(j => j.Id == request.JobPostingId, ct)
            ?? throw new NotFoundException("Job posting not found.");

        if (job.CompanyId != recruiterProfile.CompanyId)
        {
            throw new ForbiddenException("You do not have access to this job posting.");
        }
        if (job.Status != JobStatus.Open || job.ModerationStatus != ModerationStatus.Approved)
        {
            throw new ConflictException("JOB_NOT_OPEN", "You can only invite candidates to an open, approved job.");
        }

        var candidate = await _db.CandidateProfiles.Include(c => c.User).FirstOrDefaultAsync(c => c.Id == request.CandidateProfileId, ct)
            ?? throw new NotFoundException("Candidate not found.");

        var isVisible = candidate.ProfileVisibility == ProfileVisibility.VisibleToRecruiters
            || candidate.ProfileVisibility == ProfileVisibility.PublicShareable
            || await _db.JobApplications.AnyAsync(a => a.CandidateProfileId == candidate.Id && a.JobPosting.CompanyId == recruiterProfile.CompanyId, ct);

        if (!isVisible)
        {
            throw new ForbiddenException("This candidate is not visible to you.");
        }

        var hasActiveInvite = await _db.Invitations.AnyAsync(
            i => i.JobPostingId == job.Id && i.CandidateProfileId == candidate.Id && ActiveStatuses.Contains(i.Status), ct);
        if (hasActiveInvite)
        {
            throw new ConflictException("INVITATION_ALREADY_ACTIVE", "This candidate already has an active invitation for this job.");
        }

        var invitation = new Invitation
        {
            JobPostingId = job.Id,
            CandidateProfileId = candidate.Id,
            InvitedByUserId = recruiterUserId,
            Message = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            Status = InvitationStatus.Sent,
            SentAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(InvitationLifetime),
        };
        _db.Invitations.Add(invitation);
        await _db.SaveChangesAsync(ct);

        await _notifications.NotifyAsync(
            candidate.UserId,
            "InvitedToApply",
            $"{recruiterProfile.User.FullName} invited you to apply for {job.Title} at {job.Company.Name}.",
            "Invitation", invitation.Id, ct);

        await _auditLog.LogAsync(recruiterUserId, "Recruiter", "InvitationSent", "Invitation", invitation.Id, new { job.Title, CandidateProfileId = candidate.Id }, ct);

        return ToDto(invitation, job, recruiterProfile.User.FullName, candidate.User.FullName);
    }

    public async Task<IReadOnlyList<InvitationDto>> GetMyInvitationsAsync(int candidateUserId, CancellationToken ct = default)
    {
        var candidate = await _db.CandidateProfiles.FirstOrDefaultAsync(c => c.UserId == candidateUserId, ct);
        if (candidate is null) return Array.Empty<InvitationDto>();

        var invitations = await LoadInvitationsAsync(ct)
            .Where(i => i.CandidateProfileId == candidate.Id)
            .OrderByDescending(i => i.SentAtUtc)
            .ToListAsync(ct);

        return invitations.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<InvitationDto>> GetSentInvitationsAsync(int recruiterUserId, int? jobPostingId, CancellationToken ct = default)
    {
        var recruiterProfile = await _db.RecruiterProfiles.FirstOrDefaultAsync(r => r.UserId == recruiterUserId, ct)
            ?? throw new ConflictException("NOT_ONBOARDED", "Complete company onboarding first.");

        var query = LoadInvitationsAsync(ct).Where(i => i.JobPosting.CompanyId == recruiterProfile.CompanyId);
        if (jobPostingId.HasValue)
        {
            query = query.Where(i => i.JobPostingId == jobPostingId.Value);
        }

        var invitations = await query.OrderByDescending(i => i.SentAtUtc).ToListAsync(ct);
        return invitations.Select(ToDto).ToList();
    }

    public async Task<InvitationDto> MarkViewedAsync(int candidateUserId, int invitationId, CancellationToken ct = default)
    {
        var invitation = await LoadOwnedInvitationAsync(candidateUserId, invitationId, ct);

        if (invitation.Status == InvitationStatus.Sent)
        {
            invitation.Status = InvitationStatus.Viewed;
            invitation.ViewedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return ToDto(invitation);
    }

    public async Task<InvitationDto> RespondAsync(int candidateUserId, int invitationId, bool accept, CancellationToken ct = default)
    {
        var invitation = await LoadOwnedInvitationAsync(candidateUserId, invitationId, ct);

        if (invitation.Status is InvitationStatus.Accepted or InvitationStatus.Declined or InvitationStatus.Expired)
        {
            throw new ConflictException("INVITATION_ALREADY_RESOLVED", $"This invitation is already {invitation.Status}.");
        }

        invitation.Status = accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        invitation.RespondedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(candidateUserId, "Candidate", accept ? "InvitationAccepted" : "InvitationDeclined", "Invitation", invitation.Id, null, ct);

        return ToDto(invitation);
    }

    public async Task DismissAsync(int candidateUserId, int invitationId, CancellationToken ct = default)
    {
        // Dismiss is a lightweight "I've seen this" action — it does not add a distinct
        // status (the enum intentionally covers only real lifecycle states), it just ensures
        // the invitation is marked Viewed so it stops surfacing as needing attention.
        await MarkViewedAsync(candidateUserId, invitationId, ct);
    }

    private IQueryable<Invitation> LoadInvitationsAsync(CancellationToken ct) => _db.Invitations
        .Include(i => i.JobPosting).ThenInclude(j => j.Company)
        .Include(i => i.CandidateProfile).ThenInclude(c => c.User)
        .Include(i => i.InvitedByUser)
        .AsQueryable();

    private async Task<Invitation> LoadOwnedInvitationAsync(int candidateUserId, int invitationId, CancellationToken ct)
    {
        var invitation = await LoadInvitationsAsync(ct).FirstOrDefaultAsync(i => i.Id == invitationId, ct)
            ?? throw new NotFoundException("Invitation not found.");

        if (invitation.CandidateProfile.UserId != candidateUserId)
        {
            throw new ForbiddenException("You do not have access to this invitation.");
        }

        return invitation;
    }

    private static InvitationDto ToDto(Invitation i) => new(
        i.Id, i.JobPostingId, i.JobPosting.Title, i.JobPosting.Company.Name,
        i.CandidateProfileId, i.CandidateProfile.User.FullName, i.InvitedByUser.FullName,
        i.Message, i.Status.ToString(), i.SentAtUtc, i.ViewedAtUtc, i.RespondedAtUtc, i.ExpiresAtUtc);

    private static InvitationDto ToDto(Invitation i, JobPosting job, string invitedByName, string candidateName) => new(
        i.Id, i.JobPostingId, job.Title, job.Company.Name,
        i.CandidateProfileId, candidateName, invitedByName,
        i.Message, i.Status.ToString(), i.SentAtUtc, i.ViewedAtUtc, i.RespondedAtUtc, i.ExpiresAtUtc);
}
