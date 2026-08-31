using AIRecruiter.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIRecruiter.Infrastructure.Email;

/// <summary>Development-only fallback used when no SMTP account is configured — logs the
/// email content locally instead of sending anything, so the password-reset flow can be
/// exercised without a real mailbox. This class is only ever registered when both
/// (a) SMTP is not configured and (b) the host environment is Development — see
/// <c>AddInfrastructure</c>. It must never be reachable in Production.</summary>
public class DevEmailSender : IEmailSender
{
    private readonly ILogger<DevEmailSender> _logger;

    public DevEmailSender(ILogger<DevEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[DEV EMAIL — not actually sent, SMTP is not configured]\nTo: {ToEmail}\nSubject: {Subject}\n{Body}",
            toEmail, subject, textBody);
        return Task.CompletedTask;
    }
}
