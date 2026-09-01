using AIRecruiter.Application.DTOs.Platform;

namespace AIRecruiter.Application.Interfaces;

public interface IPlatformStatsService
{
    Task<PlatformStatsDto> GetStatsAsync(CancellationToken ct = default);
}
