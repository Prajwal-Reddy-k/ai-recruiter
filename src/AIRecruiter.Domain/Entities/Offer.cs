using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class Offer : BaseEntity
{
    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public decimal OfferedSalary { get; set; }
    public SalaryType SalaryType { get; set; }
    public DateTime JoiningDate { get; set; }

    public string? WorkCity { get; set; }
    public string? WorkState { get; set; }
    public bool IsRemote { get; set; }

    public JobType EmploymentType { get; set; }
    public string? ProbationDetails { get; set; }
    public string? Benefits { get; set; }
    public DateTime ExpiryDateUtc { get; set; }
    public string? RecruiterMessage { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Draft;
    public DateTime? SentAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
    public string? CandidateResponseNote { get; set; }

    public ICollection<OfferStatusHistory> StatusHistory { get; set; } = new List<OfferStatusHistory>();
}
