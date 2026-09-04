using AIRecruiter.Domain.Common;
using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Domain.Entities;

public class JobApplication : BaseEntity
{
    public int JobPostingId { get; set; }
    public JobPosting JobPosting { get; set; } = null!;

    public int CandidateProfileId { get; set; }
    public CandidateProfile CandidateProfile { get; set; } = null!;

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public string? CoverNote { get; set; }

    public decimal? MatchScore { get; set; }
    public string? MatchedSkillsCsv { get; set; }
    public string? MissingSkillsCsv { get; set; }
    public string? SuggestedImprovements { get; set; }
    public string? ScoringExplanation { get; set; }

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    public ICollection<ScreeningAnswer> ScreeningAnswers { get; set; } = new List<ScreeningAnswer>();
}
