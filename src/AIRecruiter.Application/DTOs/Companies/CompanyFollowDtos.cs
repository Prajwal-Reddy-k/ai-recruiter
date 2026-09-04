namespace AIRecruiter.Application.DTOs.Companies;

public record FollowedCompanyDto(
    int CompanyId,
    string CompanyName,
    string? LogoUrl,
    string? Industry,
    bool NotifyOnNewJob,
    DateTime FollowedAtUtc);

public record UpdateFollowNotifyRequest(bool NotifyOnNewJob);
