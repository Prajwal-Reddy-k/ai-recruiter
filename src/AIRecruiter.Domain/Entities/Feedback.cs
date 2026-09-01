using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>A support/contact-form submission. SubmittedByUserId is nullable — guests can
/// submit feedback too — and is captured only when the caller happens to be authenticated.</summary>
public class Feedback : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public FeedbackCategory Category { get; set; }
    public string Message { get; set; } = string.Empty;
    public FeedbackStatus Status { get; set; } = FeedbackStatus.New;

    public int? SubmittedByUserId { get; set; }
    public User? SubmittedByUser { get; set; }
}
