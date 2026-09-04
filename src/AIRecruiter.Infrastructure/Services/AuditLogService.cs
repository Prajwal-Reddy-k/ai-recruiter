using System.Text.Json;
using AIRecruiter.Application.DTOs.Audit;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIRecruiter.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int? actorUserId, string? actorRole, string actionType, string entityType, int? entityId, object? metadata = null, CancellationToken ct = default)
    {
        string? metadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata);

        _db.AuditLogEntries.Add(new AuditLogEntry
        {
            ActorUserId = actorUserId,
            ActorRole = actorRole,
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            MetadataJson = metadataJson,
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditLogEntryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entries = await _db.AuditLogEntries
            .Include(e => e.ActorUser)
            .OrderByDescending(e => e.TimestampUtc)
            .Take(500)
            .ToListAsync(ct);

        return entries.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<AuditLogEntryDto>> GetForCompanyAsync(int companyId, CancellationToken ct = default)
    {
        var userIds = await _db.RecruiterProfiles
            .Where(r => r.CompanyId == companyId)
            .Select(r => r.UserId)
            .ToListAsync(ct);

        var entries = await _db.AuditLogEntries
            .Include(e => e.ActorUser)
            .Where(e => e.ActorUserId != null && userIds.Contains(e.ActorUserId!.Value))
            .OrderByDescending(e => e.TimestampUtc)
            .Take(200)
            .ToListAsync(ct);

        return entries.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<AuditLogEntryDto>> GetForUserAsync(int userId, string? actionTypeFilter, DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = _db.AuditLogEntries.Include(e => e.ActorUser).Where(e => e.ActorUserId == userId);

        if (!string.IsNullOrWhiteSpace(actionTypeFilter))
        {
            query = query.Where(e => e.ActionType == actionTypeFilter);
        }
        if (from.HasValue)
        {
            query = query.Where(e => e.TimestampUtc >= from.Value);
        }
        if (to.HasValue)
        {
            query = query.Where(e => e.TimestampUtc <= to.Value);
        }

        var entries = await query.OrderByDescending(e => e.TimestampUtc).Take(300).ToListAsync(ct);
        return entries.Select(ToDto).ToList();
    }

    private static AuditLogEntryDto ToDto(AuditLogEntry e) => new(
        e.Id,
        e.ActorUserId,
        e.ActorUser?.FullName,
        e.ActorRole,
        e.ActionType,
        e.EntityType,
        e.EntityId,
        e.TimestampUtc,
        e.MetadataJson);
}
