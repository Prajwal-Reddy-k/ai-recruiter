using AIRecruiter.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIRecruiter.Infrastructure.Email;

/// <summary>Production fallback when SMTP hasn't been configured — never sends anything and
/// never logs email content (unlike <see cref="DevEmailSender"/>), only a loud server-side
/// warning so an operator notices the misconfiguration. Keeps the forgot-password endpoint's
/// "always return a generic success" contract intact even when mail truly cannot be sent.</summary>
public class NullEmailSender : IEmailSender
{
    private readonly ILogger<NullEmailSender> _logger;

    public NullEmailSender(ILogger<NullEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        _logger.LogWarning(
            "Email was not sent because no SMTP account is configured (Smtp:Host/Username/Password/SenderEmail). " +
            "Set these via environment variables or a secret manager to enable outbound email.");
        return Task.CompletedTask;
    }
}
