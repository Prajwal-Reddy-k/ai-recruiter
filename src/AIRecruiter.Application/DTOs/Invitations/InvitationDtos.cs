namespace AIRecruiter.Application.DTOs.Invitations;

public record InvitationDto(
    int Id,
    int JobPostingId,
    string JobTitle,
    string CompanyName,
    int CandidateProfileId,
    string CandidateName,
    string InvitedByName,
    string? Message,
    string Status,
    DateTime SentAtUtc,
    DateTime? ViewedAtUtc,
    DateTime? RespondedAtUtc,
    DateTime ExpiresAtUtc);

public record InviteCandidateRequest(int JobPostingId, int CandidateProfileId, string? Message);
