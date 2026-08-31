namespace AIRecruiter.Application.Interfaces;

/// <summary>Provider-agnostic outbound email. Implementations: an SMTP sender configured
/// entirely through server-side configuration (any user-supplied SMTP account — no paid
/// service required), and a Development-only sender that logs the content locally instead
/// of sending anything.</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct = default);
}
