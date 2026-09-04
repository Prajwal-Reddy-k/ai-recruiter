using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

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

    public AvailabilityStatus AvailabilityStatus { get; set; } = AvailabilityStatus.OpenToOpportunities;
    public string? PreferredJobTypesCsv { get; set; }
    public string? PreferredLocationsCsv { get; set; }
    public bool? RemotePreference { get; set; }
    public decimal? ExpectedSalaryMin { get; set; }
    public decimal? ExpectedSalaryMax { get; set; }
    public int? NoticePeriodDays { get; set; }
    public string? PreferredRolesCsv { get; set; }

    /// <summary>Defaults to VisibleAfterApplying — exactly matching the platform's behavior
    /// before this field existed, so no existing candidate becomes newly exposed or newly
    /// hidden by the migration that adds this column.</summary>
    public ProfileVisibility ProfileVisibility { get; set; } = ProfileVisibility.VisibleAfterApplying;

    /// <summary>Newline-delimited free-text bullet points for the resume builder — short
    /// items without dates or reordering needs, so a delimited-text field (matching the
    /// existing SkillsCsv convention) is used instead of a fifth child table.</summary>
    public string? AchievementsText { get; set; }

    /// <summary>Set once, the first time ProfileVisibility transitions to PublicShareable, and
    /// never regenerated afterward — so toggling visibility off and back on keeps the same
    /// public link working. Null until first enabled.</summary>
    public string? PublicProfileSlug { get; set; }

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    public ICollection<SavedJob> SavedJobs { get; set; } = new List<SavedJob>();
    public ICollection<JobAlert> JobAlerts { get; set; } = new List<JobAlert>();
    public ICollection<CandidateWorkExperience> WorkExperiences { get; set; } = new List<CandidateWorkExperience>();
    public ICollection<CandidateEducation> ResumeEducations { get; set; } = new List<CandidateEducation>();
    public ICollection<CandidateCertification> Certifications { get; set; } = new List<CandidateCertification>();
    public ICollection<CandidateProject> Projects { get; set; } = new List<CandidateProject>();
    public ICollection<CoverLetterTemplate> CoverLetterTemplates { get; set; } = new List<CoverLetterTemplate>();
    public ICollection<SkillAssessmentAttempt> AssessmentAttempts { get; set; } = new List<SkillAssessmentAttempt>();
    public ICollection<CareerGoal> CareerGoals { get; set; } = new List<CareerGoal>();
    public ICollection<CompanyFollow> FollowedCompanies { get; set; } = new List<CompanyFollow>();
    public ICollection<CompanyReview> CompanyReviews { get; set; } = new List<CompanyReview>();
}
