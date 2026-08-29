namespace AIRecruiter.Application.DTOs.ExternalJobs;

public record ExternalJobListingDto(
    string Title,
    string CompanyName,
    string? Location,
    string Description,
    string Url,
    string? SalaryRange,
    DateTime? PostedAt,
    string Source);

public record ExternalJobSearchRequest(string? Keywords, string? Location, int Page, int PageSize);

public record ExternalJobSearchResult(
    IReadOnlyList<ExternalJobListingDto> Items,
    int Page,
    int PageSize,
    int? TotalCount);
