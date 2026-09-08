using System.Text;
using AIRecruiter.API.Middleware;
using AIRecruiter.Application;
using AIRecruiter.Infrastructure;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "AI Recruiter API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });
    options.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", null),
            new List<string>()
        }
    });
});

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddApplicationServices();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidAudience = jwtOptions.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
    };

    // A password reset rotates User.SecurityStamp; comparing it here against the stamp
    // embedded in the token at issuance is what makes a reset invalidate every JWT that
    // was issued before it, despite JWTs otherwise being stateless/unrevocable.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.Principal?.FindFirst("sub")?.Value;
            var tokenStamp = context.Principal?.FindFirst("securityStamp")?.Value;

            if (userIdClaim is null || tokenStamp is null || !int.TryParse(userIdClaim, out var userId))
            {
                context.Fail("Invalid token.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var currentStamp = await db.Users
                .Where(u => u.Id == userId)
                .Select(u => u.SecurityStamp)
                .FirstOrDefaultAsync();

            if (currentStamp is null || currentStamp != tokenStamp)
            {
                context.Fail("This session is no longer valid — please sign in again.");
            }
        }
    };
});

builder.Services.AddAuthorization();

var rateLimitOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Same application/problem+json shape ExceptionHandlingMiddleware produces for a
    // RateLimitedException, so a 429 looks identical regardless of which layer produced it.
    options.OnRejected = async (context, ct) =>
    {
        var retryAfterSeconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
            ? (int)retryAfter.TotalSeconds
            : rateLimitOptions.General.WindowSeconds;

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "RATE_LIMITED",
            Detail = "Too many requests. Please try again later.",
        };
        problem.Extensions["errorCode"] = "RATE_LIMITED";

        await context.HttpContext.Response.WriteAsJsonAsync(problem, ct);
    };

    static string PartitionKey(HttpContext ctx) => ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // Looser default for the rest of the API — a global limiter, automatically overridden by
    // any endpoint carrying its own [EnableRateLimiting("...")] policy (auth/export below),
    // so nothing is ever double-limited by two policies at once.
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx), _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = rateLimitOptions.General.PermitLimit,
            Window = TimeSpan.FromSeconds(rateLimitOptions.General.WindowSeconds),
            QueueLimit = 0,
        }));

    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx), _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = rateLimitOptions.Auth.PermitLimit,
        Window = TimeSpan.FromSeconds(rateLimitOptions.Auth.WindowSeconds),
        QueueLimit = 0,
    }));

    options.AddPolicy("export", ctx => RateLimitPartition.GetFixedWindowLimiter(PartitionKey(ctx), _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = rateLimitOptions.Export.PermitLimit,
        Window = TimeSpan.FromSeconds(rateLimitOptions.Export.WindowSeconds),
        QueueLimit = 0,
    }));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        {
            // Dev convenience: Vite picks the next free port when 5173 is taken, so allow
            // any localhost origin (covers 5173, 5174, and beyond) rather than hardcoding one.
            // The Testing environment (Playwright/CI) is the same story — its frontend and
            // backend run on different localhost ports too.
            policy.SetIsOriginAllowed(origin => new Uri(origin).Host == "localhost");
        }
        else
        {
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
            policy.WithOrigins(allowedOrigins);
        }

        policy.AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    var db = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DataSeeder.SeedAsync(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must run before HTTPS redirection: ASP.NET Core's CORS middleware answers/short-circuits
// preflight OPTIONS requests itself, so if it runs first the preflight never reaches the
// redirect middleware. Browsers refuse to follow redirects for preflight requests, so having
// UseHttpsRedirection() run first (the previous bug here) broke every cross-origin POST/PUT/etc.
app.UseCors("AllowFrontend");

// Only force HTTPS outside Development — the SPA dev server talks to the API over plain HTTP
// on its "http" launch profile, and forcing HTTPS here would just reintroduce redirect issues
// (and requires a trusted local dev cert) for no benefit in local development.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Liveness check that never touches the database — used by the Playwright webServer config
// (and could back a container/orchestrator health probe) to know Kestrel itself is up,
// independent of whether the (Testing-environment, reset-seed-created) database exists yet.
app.MapGet("/health", () => Results.Ok());

app.MapControllers();

app.Run();

// Exposed so the test project can host this API in-process via WebApplicationFactory<Program>.
public partial class Program { }
