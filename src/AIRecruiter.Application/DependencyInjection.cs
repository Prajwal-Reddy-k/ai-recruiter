using AIRecruiter.Application.Interfaces;
using AIRecruiter.Application.Matching;
using AIRecruiter.Application.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AIRecruiter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IResumeMatchingService, ResumeMatchingService>();
        services.AddSingleton<ResumeFileValidator>();
        services.AddSingleton<ImageFileValidator>();
        services.AddSingleton<CandidateProfileValidator>();
        services.AddSingleton<IndiaLocationValidator>();

        return services;
    }
}
