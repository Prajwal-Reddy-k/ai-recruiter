using AIRecruiter.Application.Validation;
using AIRecruiter.Infrastructure.Locations;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;

namespace AIRecruiter.UnitTests.TestHelpers;

/// <summary>Shared constructors for the small cross-cutting services (India location
/// validation, audit logging, notifications) that most other services now depend on —
/// keeps individual test files from re-wiring the same plumbing.</summary>
public static class TestServiceFactory
{
    public static IndiaLocationValidator CreateLocationValidator() => new(new IndianLocationCatalog());

    public static AuditLogService CreateAuditLog(AppDbContext db) => new(db);

    public static NotificationService CreateNotifications(AppDbContext db) => new(db);

    public static SavedJobService CreateSavedJobs(AppDbContext db) => new(db, CreateAuditLog(db));

    public static JobAlertService CreateJobAlerts(AppDbContext db) => new(db);

    public static InMemoryViewDeduplicationService CreateViewDedup() => new();
}
