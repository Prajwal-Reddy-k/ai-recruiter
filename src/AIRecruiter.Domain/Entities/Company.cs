using AIRecruiter.Domain.Common;

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

    public ICollection<JobPosting> JobPostings { get; set; } = new List<JobPosting>();
    public ICollection<RecruiterProfile> Recruiters { get; set; } = new List<RecruiterProfile>();
    public ICollection<JobTemplate> JobTemplates { get; set; } = new List<JobTemplate>();
}
