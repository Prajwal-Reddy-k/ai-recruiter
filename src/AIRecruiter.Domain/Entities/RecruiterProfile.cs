using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class RecruiterProfile : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string? Designation { get; set; }

    public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
}
