using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

/// <summary>A recruiter's invitation for a candidate to apply to one specific open job.
/// Distinct from JobApplication — accepting an invitation does not itself create an
/// application; it just takes the candidate to the job so they can apply normally, keeping
/// the existing apply flow (resume matching, status history, etc.) completely untouched.</summary>
public class Invitation : BaseEntity
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public int InvitedByUserId { get; set; }
    public User InvitedByUser { get; set; } = null!;

    public string? Message { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Sent;

    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ViewedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
