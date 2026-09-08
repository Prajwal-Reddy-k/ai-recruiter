using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Validation;
using AIRecruiter.Infrastructure.Locations;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Moq;

namespace AIRecruiter.UnitTests.TestHelpers;

/// <summary>Shared constructors for the small cross-cutting services (India location
/// validation, audit logging, notifications) that most other services now depend on —
/// keeps individual test files from re-wiring the same plumbing.</summary>
public static class TestServiceFactory
{
    public static IndiaLocationValidator CreateLocationValidator() => new(new IndianLocationCatalog());

    /// <summary>Builds a real <see cref="CandidateProfileService"/> for tests that need one as
    /// a collaborator (not the direct subject under test) — callers that DO test avatar/resume
    /// upload behavior directly should construct their own mocks instead.</summary>
    public static CandidateProfileService CreateCandidateProfileService(
        AppDbContext db,
        IResumeStorage resumeStorage,
        IResumeTextExtractorFactory extractorFactory,
        ResumeFileValidator resumeValidator)
    {
        var avatarProcessor = new Mock<IAvatarImageProcessor>();
        return new CandidateProfileService(
            db,
            resumeStorage,
            extractorFactory,
            resumeValidator,
            Options.Create(new ResumeStorageOptions()),
            CreateLocationValidator(),
            new CandidateProfileValidator(),
            new ImageFileValidator(),
            avatarProcessor.Object,
            Options.Create(new AvatarOptions()));
    }

    public static AuditLogService CreateAuditLog(AppDbContext db) => new(db);

    public static NotificationService CreateNotifications(AppDbContext db) => new(db);

    public static SavedJobService CreateSavedJobs(AppDbContext db) => new(db, CreateAuditLog(db));

    public static JobAlertService CreateJobAlerts(AppDbContext db) => new(db);

    public static InMemoryViewDeduplicationService CreateViewDedup() => new();

    public static CoverLetterTemplateService CreateCoverLetterTemplateService(AppDbContext db) => new(db);

    public static SkillAssessmentService CreateSkillAssessmentService(AppDbContext db) => new(db, CreateAuditLog(db));

    public static PublicProfileService CreatePublicProfileService(AppDbContext db) => new(db);

    public static CareerGoalService CreateCareerGoalService(AppDbContext db) => new(db, CreateLocationValidator());

    public static OfferService CreateOfferService(AppDbContext db) => new(db, CreateNotifications(db), CreateAuditLog(db));

    public static TalentPoolService CreateTalentPoolService(AppDbContext db) => new(db);

    public static ReferralService CreateReferralService(AppDbContext db) => new(db, new InMemoryIpRateLimiter());

    public static CompanyVerificationService CreateCompanyVerificationService(AppDbContext db) => new(db, CreateAuditLog(db));

    public static FollowService CreateFollowService(AppDbContext db) => new(db, CreateAuditLog(db));

    public static ActivityTimelineService CreateActivityTimelineService(AppDbContext db) => new(db, CreateAuditLog(db));

    public static SalaryInsightsService CreateSalaryInsightsService(AppDbContext db) => new(db);

    public static CompanyReviewService CreateCompanyReviewService(AppDbContext db) => new(db, CreateAuditLog(db), new InMemoryIpRateLimiter());

    public static AccountDataExportService CreateAccountDataExportService(AppDbContext db) => new(db, CreateAuditLog(db));

    public static TokenService CreateTokenService() => new(Options.Create(new JwtOptions
    {
        Secret = "unit-test-secret-key-at-least-32-characters-long",
        Issuer = "AIRecruiter.Tests",
        Audience = "AIRecruiter.Tests",
        ExpiryMinutes = 60,
    }));

    public static RefreshTokenService CreateRefreshTokens(AppDbContext db) => new(db, CreateTokenService(), CreateAuditLog(db));
}
