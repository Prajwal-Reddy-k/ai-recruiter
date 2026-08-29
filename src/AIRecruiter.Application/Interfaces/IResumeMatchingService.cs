using AIRecruiter.Application.DTOs.Matching;

namespace AIRecruiter.Application.Interfaces;

/// <summary>
/// Deterministic, explainable, fully local resume-to-job matching. Not an external AI
/// service and not an automated hiring decision — decision support only.
/// </summary>
public interface IResumeMatchingService
{
    ResumeMatchResult CalculateMatch(string resumeText, JobMatchInput job, CandidateMatchInput candidate);
}
