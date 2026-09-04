using AIRecruiter.Application.DTOs.Audit;

namespace AIRecruiter.Application.Interfaces;

public interface IAuditLogService
{
    /// <summary>
    /// Records an audit entry. <paramref name="metadata"/> must only ever contain small,
    /// non-sensitive values (IDs, status names, titles) — never passwords, hashes, JWTs,
    /// tokens, or resume content.
    /// </summary>
    Task LogAsync(int? actorUserId, string? actorRole, string actionType, string entityType, int? entityId, object? metadata = null, CancellationToken ct = default);

    Task<IReadOnlyList<AuditLogEntryDto>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AuditLogEntryDto>> GetForCompanyAsync(int companyId, CancellationToken ct = default);

    /// <summary>Strictly self-scoped — only entries where this exact user is the actor.
    /// Never the broader team-wide scope GetForCompanyAsync uses.</summary>
    Task<IReadOnlyList<AuditLogEntryDto>> GetForUserAsync(int userId, string? actionTypeFilter, DateTime? from, DateTime? to, CancellationToken ct = default);
}
