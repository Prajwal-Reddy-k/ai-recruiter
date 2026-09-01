using System.Text.RegularExpressions;
using AIRecruiter.Application.DTOs.Feedback;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

/// <summary>Handles the public Help &amp; Support contact form — open to guests and logged-in
/// users alike, so validation and rate limiting happen here rather than relying on
/// [Authorize].</summary>
public class FeedbackService : IFeedbackService
{
    private const int MaxSubmissionsPerWindow = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);
    private const int MinMessageLength = 10;
    private const int MaxMessageLength = 2000;
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IIpRateLimiter _rateLimiter;
    private readonly IAuditLogService _auditLog;

    public FeedbackService(AppDbContext db, IIpRateLimiter rateLimiter, IAuditLogService auditLog)
    {
        _db = db;
        _rateLimiter = rateLimiter;
        _auditLog = auditLog;
    }

    public async Task SubmitFeedbackAsync(int? submittedByUserId, string ipAddress, SubmitFeedbackRequest request, CancellationToken ct = default)
    {
        if (!_rateLimiter.IsAllowed($"feedback:ip:{ipAddress}", MaxSubmissionsPerWindow, RateLimitWindow))
        {
            throw new RateLimitedException("Too many submissions from this location. Please try again later.", retryAfterSeconds: (int)RateLimitWindow.TotalSeconds);
        }

        var errors = new Dictionary<string, string>();

        var name = request.Name.Trim();
        if (name.Length is < 2 or > 100)
        {
            errors["name"] = "Enter your name (2-100 characters).";
        }

        var email = request.Email.Trim();
        if (!EmailRegex.IsMatch(email))
        {
            errors["email"] = "Enter a valid email address.";
        }

        var message = request.Message.Trim();
        if (message.Length < MinMessageLength || message.Length > MaxMessageLength)
        {
            errors["message"] = $"Message must be between {MinMessageLength} and {MaxMessageLength} characters.";
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Please fix the highlighted fields.", errors);
        }

        var feedback = new Feedback
        {
            Name = name,
            Email = email.ToLowerInvariant(),
            Category = request.Category,
            Message = message,
            SubmittedByUserId = submittedByUserId,
        };
        _db.Feedbacks.Add(feedback);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<FeedbackDto>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.Feedbacks
            .Include(f => f.SubmittedByUser)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new FeedbackDto(
                f.Id, f.Name, f.Email, f.Category.ToString(), f.Message, f.Status.ToString(),
                f.SubmittedByUser != null ? f.SubmittedByUser.FullName : null, f.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task SetStatusAsync(int adminUserId, int feedbackId, SetFeedbackStatusRequest request, CancellationToken ct = default)
    {
        var feedback = await _db.Feedbacks.FirstOrDefaultAsync(f => f.Id == feedbackId, ct)
            ?? throw new NotFoundException("Feedback submission not found.");

        feedback.Status = request.Status;
        await _db.SaveChangesAsync(ct);

        await _auditLog.LogAsync(adminUserId, "Admin", "FeedbackStatusChanged", "Feedback", feedback.Id, new { Status = feedback.Status.ToString() }, ct);
    }
}
