using AIRecruiter.Application.DTOs.Users;

namespace AIRecruiter.Application.Interfaces;

/// <summary>Builds a self-service export of the caller's own data — every query is scoped to
/// the caller's id, and PasswordHash/SecurityStamp/reset-code fields/internal storage keys are
/// never included by construction (see AccountExportDto).</summary>
public interface IAccountDataExportService
{
    Task<AccountExportDto> GetMyDataExportAsync(int userId, CancellationToken ct = default);
    Task<string> GetMyDataExportAsCsvAsync(int userId, CancellationToken ct = default);
}
