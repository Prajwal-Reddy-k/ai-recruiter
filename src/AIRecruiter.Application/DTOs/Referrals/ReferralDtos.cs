namespace AIRecruiter.Application.DTOs.Referrals;

public record CreateReferralRequest(
    string ReferredName,
    string ReferredEmail,
    string? ReferredPhone,
    string? RelevantSkillsCsv,
    string? Note,
    int JobPostingId);

/// <summary>The raw token is included exactly once, at creation — it is never persisted or
/// returned again afterward.</summary>
public record CreateReferralResponse(int Id, string RawToken, DateTime TokenExpiresAtUtc);

public record ReferralDto(
    int Id,
    string ReferredName,
    string ReferredEmail,
    string JobTitle,
    string CompanyName,
    string Status,
    DateTime CreatedAt,
    DateTime? RegisteredAtUtc,
    DateTime? AppliedAtUtc);

public record ReferralTokenPreviewDto(string JobTitle, string CompanyName, DateTime TokenExpiresAtUtc);
