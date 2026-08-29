using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class JobReport : BaseEntity
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public int ReportedByUserId { get; set; }
    public User ReportedByUser { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public ReportStatus Status { get; set; } = ReportStatus.Pending;

    public int? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ResolutionNote { get; set; }
}
