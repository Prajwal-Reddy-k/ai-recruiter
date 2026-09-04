using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Website { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? Size { get; set; }
    public string? Benefits { get; set; }
    public string? CultureHighlights { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? TwitterUrl { get; set; }

    /// <summary>"Platform Verified" only — never a government or legal verification. See
    /// CompanyVerificationService/AdminService for the submit/review workflow.</summary>
    public string? BusinessEmail { get; set; }
    public CompanyVerificationStatus VerificationStatus { get; set; } = CompanyVerificationStatus.NotSubmitted;
    public string? VerificationNote { get; set; }
    public string? VerificationDocumentReference { get; set; }
    public DateTime? VerificationSubmittedAtUtc { get; set; }
    public DateTime? VerificationReviewedAtUtc { get; set; }
    public int? VerificationReviewedByUserId { get; set; }
    public User? VerificationReviewedByUser { get; set; }

    public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    public ICollection<RecruiterProfile> Recruiters { get; set; } = new List<RecruiterProfile>();
    public ICollection<JobTemplate> JobTemplates { get; set; } = new List<JobTemplate>();
    public ICollection<TalentPool> TalentPools { get; set; } = new List<TalentPool>();
    public ICollection<CompanyFollow> Followers { get; set; } = new List<CompanyFollow>();
    public ICollection<CompanyReview> Reviews { get; set; } = new List<CompanyReview>();
}
