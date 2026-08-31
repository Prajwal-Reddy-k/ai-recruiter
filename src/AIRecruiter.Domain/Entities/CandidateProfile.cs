using AIRecruiter.Domain.Common;

namespace AIRecruiter.Domain.Entities;

public class CandidateProfile : BaseEntity
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string? Headline { get; set; }
    public string? Summary { get; set; }
    public string? Education { get; set; }
    public int? GraduationYear { get; set; }
    public string? ExperienceSummary { get; set; }
    public int? TotalExperienceYears { get; set; }

    public string? City { get; set; }
    public string? State { get; set; }
    public string? Locality { get; set; }

    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
    public decimal? CurrentSalary { get; set; }
    public decimal? ExpectedSalary { get; set; }
    public string? SkillsCsv { get; set; }

    public string? Phone { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? GithubUrl { get; set; }
    public string? PortfolioUrl { get; set; }

    public string? ResumeStorageKey { get; set; }
    public string? ResumeOriginalFileName { get; set; }
    public string? ResumeContentType { get; set; }
    public long? ResumeSizeBytes { get; set; }
    public DateTime? ResumeUploadedAt { get; set; }
    public string? ResumeExtractedText { get; set; }

    public string? AvatarStorageKey { get; set; }
    public string? AvatarContentType { get; set; }
    public long? AvatarSizeBytes { get; set; }
    public DateTime? AvatarUploadedAt { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    public ICollection<JobAlert> JobAlerts { get; set; } = new List<JobAlert>();
}
