using AIRecruiter.Application.Interfaces;

namespace AIRecruiter.UnitTests.TestHelpers;

/// <summary>Test double that records every "sent" email instead of making a real SMTP
/// connection, so tests can assert on the raw verification code without needing a mailbox.</summary>
public class RecordingEmailSender : IEmailSender
{
    public List<(string ToEmail, string Subject, string Html, string Text)> SentEmails { get; } = new();

    public Task SendAsync(string toEmail, string subject, string htmlBody, string textBody, CancellationToken ct = default)
    {
        SentEmails.Add((toEmail, subject, htmlBody, textBody));
        return Task.CompletedTask;
    }
}
