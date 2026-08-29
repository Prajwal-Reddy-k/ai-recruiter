using AIRecruiter.Application.DTOs.Candidates;

namespace AIRecruiter.Application.Interfaces;

public interface ICandidateProfileService
{
    Task<CandidateProfileDto> GetMyProfileAsync(int userId, CancellationToken ct = default);
    Task<CandidateProfileDto> UpsertMyProfileAsync(int userId, UpsertCandidateProfileRequest request, CancellationToken ct = default);
    Task<CandidateProfileDto> UploadResumeAsync(int userId, Stream content, string originalFileName, string contentType, long sizeBytes, CancellationToken ct = default);
    Task<(Stream Content, string FileName, string ContentType)> DownloadOwnResumeAsync(int userId, CancellationToken ct = default);
}
