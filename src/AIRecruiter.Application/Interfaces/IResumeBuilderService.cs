using AIRecruiter.Application.DTOs.Candidates;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Structured, in-app resume builder — additive to CandidateProfileService's
/// existing free-text fields and uploaded-resume-file flow, never replacing either.</summary>
public interface IResumeBuilderService
{
    Task<ResumeDto> GetMyResumeAsync(int userId, CancellationToken ct = default);
    Task<ResumeDto> UpsertSummaryLinksAsync(int userId, UpsertResumeSummaryRequest request, CancellationToken ct = default);

    Task<ResumeDto> AddExperienceAsync(int userId, UpsertWorkExperienceRequest request, CancellationToken ct = default);
    Task<ResumeDto> UpdateExperienceAsync(int userId, int experienceId, UpsertWorkExperienceRequest request, CancellationToken ct = default);
    Task<ResumeDto> DeleteExperienceAsync(int userId, int experienceId, CancellationToken ct = default);
    Task<ResumeDto> ReorderExperienceAsync(int userId, ReorderRequest request, CancellationToken ct = default);

    Task<ResumeDto> AddEducationAsync(int userId, UpsertEducationEntryRequest request, CancellationToken ct = default);
    Task<ResumeDto> UpdateEducationAsync(int userId, int educationId, UpsertEducationEntryRequest request, CancellationToken ct = default);
    Task<ResumeDto> DeleteEducationAsync(int userId, int educationId, CancellationToken ct = default);
    Task<ResumeDto> ReorderEducationAsync(int userId, ReorderRequest request, CancellationToken ct = default);

    Task<ResumeDto> AddCertificationAsync(int userId, UpsertCertificationRequest request, CancellationToken ct = default);
    Task<ResumeDto> UpdateCertificationAsync(int userId, int certificationId, UpsertCertificationRequest request, CancellationToken ct = default);
    Task<ResumeDto> DeleteCertificationAsync(int userId, int certificationId, CancellationToken ct = default);
    Task<ResumeDto> ReorderCertificationAsync(int userId, ReorderRequest request, CancellationToken ct = default);

    Task<ResumeDto> AddProjectAsync(int userId, UpsertProjectRequest request, CancellationToken ct = default);
    Task<ResumeDto> UpdateProjectAsync(int userId, int projectId, UpsertProjectRequest request, CancellationToken ct = default);
    Task<ResumeDto> DeleteProjectAsync(int userId, int projectId, CancellationToken ct = default);
    Task<ResumeDto> ReorderProjectAsync(int userId, ReorderRequest request, CancellationToken ct = default);
}
