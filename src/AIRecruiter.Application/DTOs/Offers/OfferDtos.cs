namespace AIRecruiter.Application.DTOs.Offers;

public record OfferDto(
    int Id,
    int JobApplicationId,
    string JobTitle,
    string CompanyName,
    string CandidateName,
    decimal OfferedSalary,
    string SalaryType,
    DateTime JoiningDate,
    string? WorkCity,
    string? WorkState,
    bool IsRemote,
    string EmploymentType,
    string? ProbationDetails,
    string? Benefits,
    DateTime ExpiryDateUtc,
    string? RecruiterMessage,
    string Status,
    DateTime? SentAtUtc,
    DateTime? RespondedAtUtc,
    string? CandidateResponseNote,
    DateTime CreatedAt);

public record OfferStatusHistoryEntryDto(string? FromStatus, string ToStatus, string ChangedByName, DateTime ChangedAt, string? Note);

public record OfferDetailDto(OfferDto Offer, IReadOnlyList<OfferStatusHistoryEntryDto> StatusHistory);

public record CreateOfferRequest(
    decimal OfferedSalary,
    string SalaryType,
    DateTime JoiningDate,
    string? WorkCity,
    string? WorkState,
    bool IsRemote,
    string EmploymentType,
    string? ProbationDetails,
    string? Benefits,
    DateTime ExpiryDateUtc,
    string? RecruiterMessage);

public record UpdateOfferRequest(
    decimal OfferedSalary,
    string SalaryType,
    DateTime JoiningDate,
    string? WorkCity,
    string? WorkState,
    bool IsRemote,
    string EmploymentType,
    string? ProbationDetails,
    string? Benefits,
    DateTime ExpiryDateUtc,
    string? RecruiterMessage);

public record RespondToOfferRequest(bool Accept, string? Note);
