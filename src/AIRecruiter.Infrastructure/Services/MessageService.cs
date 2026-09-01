using AIRecruiter.Application.DTOs.Messaging;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>A conversation is keyed by JobApplicationId (no separate Conversation table).
/// Access mirrors JobApplicationService's EnsureCanViewApplication convention: the owning
/// candidate, or the owning recruiter / a same-company Owner for that application's job.
/// Message bodies are always rendered as plain text by the frontend (React's default
/// escaping) — never via dangerouslySetInnerHTML — so no server-side HTML sanitization is
/// needed; this is documented, not incidental.</summary>
public class MessageService : IMessageService
{
    private const int MaxBodyLength = 2000;
    private const int MaxMessagesPerWindow = 20;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;
    private readonly IAuditLogService _auditLog;
    private readonly IIpRateLimiter _rateLimiter;

    public MessageService(AppDbContext db, INotificationService notifications, IAuditLogService auditLog, IIpRateLimiter rateLimiter)
    {
        _db = db;
        _notifications = notifications;
        _auditLog = auditLog;
        _rateLimiter = rateLimiter;
    }

    public async Task<MessageDto> SendMessageAsync(int userId, string role, int applicationId, string ipAddress, SendMessageRequest request, CancellationToken ct = default)
    {
        var body = (request.Body ?? string.Empty).Trim();
        if (body.Length == 0)
        {
            throw new ValidationException("Message cannot be empty.", new Dictionary<string, string> { ["body"] = "Message cannot be empty." });
        }
        if (body.Length > MaxBodyLength)
        {
            throw new ValidationException($"Message must be {MaxBodyLength} characters or fewer.",
                new Dictionary<string, string> { ["body"] = $"Message must be {MaxBodyLength} characters or fewer." });
        }

        if (!_rateLimiter.IsAllowed($"MessageSend:{userId}", MaxMessagesPerWindow, RateLimitWindow)
            || !_rateLimiter.IsAllowed($"MessageSend:ip:{ipAddress}", MaxMessagesPerWindow * 2, RateLimitWindow))
        {
            throw new RateLimitedException("You're sending messages too quickly. Please wait a moment and try again.", retryAfterSeconds: 60);
        }

        var application = await LoadApplicationAsync(applicationId, ct);
        await EnsureCanAccessAsync(application, userId, role, ct);

        var message = new Message
        {
            JobApplicationId = applicationId,
            SenderUserId = userId,
            SenderRole = role,
            Body = body,
        };
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);

        var recipientUserId = role == "Candidate" ? application.JobPosting.RecruiterProfile.UserId : application.CandidateProfile.UserId;
        var senderName = role == "Candidate" ? application.CandidateProfile.User.FullName : application.JobPosting.RecruiterProfile.User.FullName;

        await _notifications.NotifyAsync(
            recipientUserId,
            "MessageReceived",
            $"New message from {senderName} about {application.JobPosting.Title}.",
            "JobApplication", application.Id, ct);

        await _auditLog.LogAsync(userId, role, "MessageSent", "JobApplication", application.Id, null, ct);

        return ToDto(message, senderName);
    }

    public async Task<IReadOnlyList<MessageDto>> GetThreadAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await LoadApplicationAsync(applicationId, ct);
        await EnsureCanAccessAsync(application, userId, role, ct);

        var messages = await _db.Messages
            .Include(m => m.SenderUser)
            .Where(m => m.JobApplicationId == applicationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

        return messages.Select(m => ToDto(m, m.SenderUser.FullName)).ToList();
    }

    public async Task<IReadOnlyList<ConversationSummaryDto>> GetMyInboxAsync(int userId, string role, CancellationToken ct = default)
    {
        IQueryable<JobApplication> applications = role == "Candidate"
            ? _db.JobApplications.Where(a => a.CandidateProfile.UserId == userId)
            : _db.JobApplications.Where(a => a.JobPosting.RecruiterProfile.UserId == userId);

        // Only threads that actually have at least one message show up in the inbox.
        var withMessages = await applications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile).ThenInclude(r => r.User)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .Where(a => a.Messages.Any())
            .ToListAsync(ct);

        var summaries = new List<ConversationSummaryDto>();
        foreach (var application in withMessages)
        {
            var messages = await _db.Messages
                .Where(m => m.JobApplicationId == application.Id)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync(ct);

            var last = messages.First();
            var unread = messages.Count(m => !m.IsRead && m.SenderUserId != userId);
            var counterpartName = role == "Candidate"
                ? application.JobPosting.RecruiterProfile.User.FullName
                : application.CandidateProfile.User.FullName;

            summaries.Add(new ConversationSummaryDto(
                application.Id,
                application.JobPostingId,
                application.JobPosting.Title,
                application.JobPosting.Company.Name,
                counterpartName,
                last.Body,
                last.CreatedAt,
                unread));
        }

        return summaries.OrderByDescending(s => s.LastMessageAt).ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId, string role, CancellationToken ct = default)
    {
        IQueryable<Message> query = role == "Candidate"
            ? _db.Messages.Where(m => m.JobApplication.CandidateProfile.UserId == userId)
            : _db.Messages.Where(m => m.JobApplication.JobPosting.RecruiterProfile.UserId == userId);

        return await query.CountAsync(m => !m.IsRead && m.SenderUserId != userId, ct);
    }

    public async Task MarkThreadReadAsync(int userId, string role, int applicationId, CancellationToken ct = default)
    {
        var application = await LoadApplicationAsync(applicationId, ct);
        await EnsureCanAccessAsync(application, userId, role, ct);

        var unread = await _db.Messages
            .Where(m => m.JobApplicationId == applicationId && !m.IsRead && m.SenderUserId != userId)
            .ToListAsync(ct);

        foreach (var message in unread)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        if (unread.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<JobApplication> LoadApplicationAsync(int applicationId, CancellationToken ct)
    {
        return await _db.JobApplications
            .Include(a => a.JobPosting).ThenInclude(j => j.Company)
            .Include(a => a.JobPosting).ThenInclude(j => j.RecruiterProfile).ThenInclude(r => r.User)
            .Include(a => a.CandidateProfile).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(a => a.Id == applicationId, ct)
            ?? throw new NotFoundException("Application not found.");
    }

    private async Task EnsureCanAccessAsync(JobApplication application, int userId, string role, CancellationToken ct)
    {
        var isOwningCandidate = role == "Candidate" && application.CandidateProfile.UserId == userId;
        var isOwningRecruiter = role == "Recruiter" && await CompanyAccessHelper.IsOwningRecruiterOrCompanyOwnerAsync(
            _db, userId, application.JobPosting.RecruiterProfile.UserId, application.JobPosting.CompanyId, ct);

        if (!isOwningCandidate && !isOwningRecruiter)
        {
            throw new ForbiddenException("You do not have access to this conversation.");
        }
    }

    private static MessageDto ToDto(Message m, string senderName) => new(
        m.Id, m.JobApplicationId, m.SenderUserId, m.SenderRole, senderName, m.Body, m.CreatedAt, m.IsRead);
}
