using System.Net;
using System.Net.Mail;
using AIRecruiter.Application.Interfaces;
using AIRecruiter.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIRecruiter.Infrastructure.Email;

/// <summary>Sends real email through any user-supplied SMTP account (Gmail app password,
/// Outlook, a self-hosted relay, etc.) — no paid email API involved. Only registered when
/// every required <see cref="SmtpOptions"/> field is present; credentials are never logged.</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_options.SenderEmail, _options.SenderName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false,
        };
        message.To.Add(toEmail);

        var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
        message.AlternateViews.Add(htmlView);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
        };

        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Never log the SMTP password/credentials — only that the send failed.
            _logger.LogError(ex, "Failed to send email via SMTP to a recipient (message suppressed).");
            throw;
        }
    }
}
