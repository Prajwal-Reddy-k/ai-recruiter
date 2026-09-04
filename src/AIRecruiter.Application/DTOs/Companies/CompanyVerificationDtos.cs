namespace AIRecruiter.Application.DTOs.Companies;

public record SubmitCompanyVerificationRequest(
    string? Website,
    string BusinessEmail,
    string? City,
    string? State,
    string? Description,
    string? VerificationDocumentReference);

public record CompanyVerificationStatusDto(
    string Status,
    string? Note,
    DateTime? SubmittedAtUtc,
    DateTime? ReviewedAtUtc);

public record PendingCompanyVerificationDto(
    int CompanyId,
    string CompanyName,
    string? Website,
    string? BusinessEmail,
    string? City,
    string? State,
    string? Description,
    string? VerificationDocumentReference,
    string Status,
    DateTime? SubmittedAtUtc);

public record SetCompanyVerificationStatusRequest(string Status, string? Note);
