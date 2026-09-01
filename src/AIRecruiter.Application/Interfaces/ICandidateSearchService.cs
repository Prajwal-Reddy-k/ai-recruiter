using AIRecruiter.Application.DTOs.Candidates;

namespace AIRecruiter.Application.Interfaces;

public interface ICandidateSearchService
{
    /// <summary>Scoped to every job posting owned by the caller's own company (derived
    /// server-side from the caller's RecruiterProfile — never trusts a client-supplied
    /// company id), one row per application.</summary>
    Task<IReadOnlyList<CandidateSearchResultDto>> SearchAsync(int recruiterUserId, CandidateSearchQuery query, CancellationToken ct = default);

    Task<CandidateSearchDetailDto> GetDetailAsync(int recruiterUserId, int candidateProfileId, CancellationToken ct = default);

    /// <summary>Only non-sensitive, already-authorized fields — never resume content,
    /// passwords, tokens, or applicants outside the caller's own company.</summary>
    Task<string> ExportCsvAsync(int recruiterUserId, CandidateSearchQuery query, CancellationToken ct = default);

    Task<(Stream Content, string FileName, string ContentType)> DownloadApplicantResumeAsync(int recruiterUserId, int applicationId, CancellationToken ct = default);

    /// <summary>Candidates discoverable ahead of applying anywhere — only
    /// ProfileVisibility.VisibleToRecruiters candidates, available company-agnostically to
    /// any authenticated Recruiter (not scoped to the caller's own company, since these
    /// candidates opted into being found generally). This is the one genuinely new
    /// visibility surface the platform adds; SearchAsync's applied-to-this-company gate is
    /// unchanged.</summary>
    Task<IReadOnlyList<DiscoverableCandidateDto>> GetDiscoverableCandidatesAsync(DiscoverCandidatesQuery query, CancellationToken ct = default);
}
