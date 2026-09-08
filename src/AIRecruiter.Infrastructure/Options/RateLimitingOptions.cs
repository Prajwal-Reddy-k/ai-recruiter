namespace AIRecruiter.Infrastructure.Options;

/// <summary>Configures ASP.NET Core's built-in Microsoft.AspNetCore.RateLimiting middleware —
/// a coarser, pipeline-level gate that sits alongside (not instead of) the existing
/// IIpRateLimiter business-rule limiters (forgot-password, referrals, feedback, etc.), which
/// remain unchanged. All three policies here partition by client IP, for both anonymous and
/// authenticated routes — the existing IIpRateLimiter call sites already provide finer,
/// per-user throttling where that matters more than per-IP.</summary>
public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public RateLimitWindow Auth { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
    public RateLimitWindow General { get; set; } = new() { PermitLimit = 300, WindowSeconds = 60 };
    public RateLimitWindow Export { get; set; } = new() { PermitLimit = 10, WindowSeconds = 60 };
}

public class RateLimitWindow
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
}
