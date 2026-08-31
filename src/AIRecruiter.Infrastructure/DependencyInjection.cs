using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Email;
using AIRecruiter.Infrastructure.ExternalJobs;
using AIRecruiter.Infrastructure.Locations;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.Infrastructure.Storage;
using AIRecruiter.Infrastructure.TextExtraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIRecruiter.Infrastructure;

public static class DependencyInjection
{
    /// <summary><paramref name="isDevelopment"/> gates the Development-only email sender —
    /// it must never be reachable outside Development, even if SMTP happens to be unconfigured
    /// there too. Callers pass <c>builder.Environment.IsDevelopment()</c>.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isDevelopment)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ResumeStorageOptions>(configuration.GetSection(ResumeStorageOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection(CloudinaryOptions.SectionName));
        services.Configure<AdzunaOptions>(configuration.GetSection(AdzunaOptions.SectionName));
        services.Configure<NominatimOptions>(configuration.GetSection(NominatimOptions.SectionName));
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));

        services.AddMemoryCache();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJobPostingService, JobPostingService>();
        services.AddScoped<IRecruiterOnboardingService, RecruiterOnboardingService>();
        services.AddScoped<CandidateProfileService>();
        services.AddScoped<ICandidateProfileService>(sp => sp.GetRequiredService<CandidateProfileService>());
        services.AddScoped<IJobApplicationService, JobApplicationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<IIndianLocationCatalog, IndianLocationCatalog>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IInterviewService, InterviewService>();
        services.AddScoped<ISavedJobService, SavedJobService>();
        services.AddScoped<IJobAlertService, JobAlertService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddSingleton<IViewDeduplicationService, InMemoryViewDeduplicationService>();
        services.AddScoped<ICandidateSearchService, CandidateSearchService>();
        services.AddSingleton<IIpRateLimiter, InMemoryIpRateLimiter>();

        // Email: a real SMTP account (any provider) if fully configured; otherwise a
        // Development-only sender that logs the content locally; otherwise (e.g. Production
        // with SMTP left unconfigured) a fallback that sends nothing and only warns.
        var smtpOptions = configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
        if (smtpOptions.IsConfigured)
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else if (isDevelopment)
        {
            services.AddScoped<IEmailSender, DevEmailSender>();
        }
        else
        {
            services.AddScoped<IEmailSender, NullEmailSender>();
        }

        // Text extraction
        services.AddSingleton<IResumeTextExtractor, PdfResumeTextExtractor>();
        services.AddSingleton<IResumeTextExtractor, DocxResumeTextExtractor>();
        services.AddSingleton<IResumeTextExtractorFactory, ResumeTextExtractorFactory>();

        // Resume storage: Cloudinary only if fully configured, local disk otherwise.
        var cloudinaryOptions = configuration.GetSection(CloudinaryOptions.SectionName).Get<CloudinaryOptions>() ?? new CloudinaryOptions();
        if (cloudinaryOptions.IsConfigured)
        {
            services.AddHttpClient("CloudinaryDownload");
            services.AddScoped<IResumeStorage, CloudinaryResumeStorage>();
        }
        else
        {
            services.AddScoped<IResumeStorage, LocalResumeStorage>();
        }

        // External job search: Adzuna only if credentials are present.
        var adzunaOptions = configuration.GetSection(AdzunaOptions.SectionName).Get<AdzunaOptions>() ?? new AdzunaOptions();
        services.AddHttpClient("Adzuna", client =>
        {
            client.BaseAddress = new Uri("https://api.adzuna.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        if (adzunaOptions.IsConfigured)
        {
            services.AddScoped<IExternalJobSearchService, AdzunaJobSearchService>();
        }
        else
        {
            services.AddScoped<IExternalJobSearchService, DisabledExternalJobSearchService>();
        }

        // Location autocomplete (Nominatim) — identifying User-Agent per usage policy.
        var nominatimOptions = configuration.GetSection(NominatimOptions.SectionName).Get<NominatimOptions>() ?? new NominatimOptions();
        services.AddHttpClient("Nominatim", client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            client.Timeout = TimeSpan.FromSeconds(8);
            client.DefaultRequestHeaders.UserAgent.ParseAdd($"AIRecruiter-Portfolio/1.0 (contact: {nominatimOptions.ContactEmail})");
        });
        services.AddSingleton<NominatimRateGate>();
        services.AddScoped<ILocationSearchService, NominatimLocationSearchService>();

        return services;
    }
}
