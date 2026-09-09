using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AIRecruiter.Infrastructure.Options;

/// <summary>Fails startup fast with a specific, actionable message rather than letting a
/// missing/weak JWT secret surface later as a cryptic signing-key exception on first login.
/// The 32-character floor is enforced in every environment (it's a correctness/security
/// baseline, not a Production-only nicety) — Issuer/Audience presence is required only in
/// Production, since Development's shipped placeholder values already satisfy all of this.</summary>
public class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private const int MinSecretLength = 32;

    private readonly IHostEnvironment _environment;

    public JwtOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var failures = new List<string>();

        // Never include the secret's value here — only the fact that it's missing/short.
        if (string.IsNullOrWhiteSpace(options.Secret) || options.Secret.Length < MinSecretLength)
        {
            failures.Add($"Jwt:Secret is missing or shorter than {MinSecretLength} characters.");
        }

        if (_environment.IsProduction())
        {
            if (string.IsNullOrWhiteSpace(options.Issuer))
            {
                failures.Add("Jwt:Issuer is required in Production.");
            }

            if (string.IsNullOrWhiteSpace(options.Audience))
            {
                failures.Add("Jwt:Audience is required in Production.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
