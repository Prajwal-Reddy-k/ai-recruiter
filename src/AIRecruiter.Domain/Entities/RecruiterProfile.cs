using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class RecruiterProfile : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string? Designation { get; set; }
    public CompanyRole CompanyRole { get; set; } = CompanyRole.Owner;

    public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    public ICollection<JobAssignment> JobAssignments { get; set; } = new List<JobAssignment>();
    public ICollection<InterviewAssignment> InterviewAssignments { get; set; } = new List<InterviewAssignment>();
}
