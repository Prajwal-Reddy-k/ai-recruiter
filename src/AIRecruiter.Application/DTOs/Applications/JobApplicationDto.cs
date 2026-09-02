using AIRecruiter.Domain.Enums;

namespace AIRecruiter.Application.DTOs.Applications;

public record JobApplicationDto(
    int Id,
    int JobPostingId,
    string JobTitle,
    string CompanyName,
    string? CandidateFullName,
    string? CandidateHeadline,
    string? CandidateSkillsCsv,
    string Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int? MatchScore,
    string? JobLocation = null,
    DateTime? NextInterviewAtUtc = null,
    string? CandidateAvatarUrl = null,
    int? CandidateProfileId = null);

public record UpdateApplicationStatusRequest(ApplicationStatus Status, string? Note);

public record StatusHistoryEntryDto(string? FromStatus, string ToStatus, string ChangedByName, DateTime ChangedAt, string? Note);

public record JobApplicationDetailDto(
    int Id,
    int JobPostingId,
    string JobTitle,
    string CompanyName,
    int CandidateProfileId,
    string CandidateFullName,
    string Status,
    string? CoverNote,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int? MatchScore,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> SuggestedImprovements,
    string? ScoringExplanation,
    IReadOnlyList<StatusHistoryEntryDto> StatusHistory,
    string NextAction);
