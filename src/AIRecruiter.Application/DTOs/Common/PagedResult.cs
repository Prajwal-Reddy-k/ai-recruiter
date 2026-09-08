namespace AIRecruiter.Application.DTOs.Common;

/// <summary>Standard server-side pagination wrapper — mirrors the shape of
/// ExternalJobDtos.ExternalJobSearchResult (the existing Adzuna-integration pagination DTO) so
/// paginated local endpoints look the same to clients as the external-job-search endpoint.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public static class PagingDefaults
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 50;

    public static int ClampPage(int page) => Math.Max(1, page);

    public static int ClampPageSize(int pageSize) => pageSize <= 0
        ? DefaultPageSize
        : Math.Clamp(pageSize, 1, MaxPageSize);
}
