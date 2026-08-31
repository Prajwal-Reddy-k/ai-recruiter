using AIRecruiter.Infrastructure.Email;
using AIRecruiter.Infrastructure.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace AIRecruiter.UnitTests.Email;

public class EmailSenderTests
{
    [Fact]
    public void SmtpOptions_AllFieldsPresent_IsConfiguredTrue()
    {
        var options = new SmtpOptions
        {
            Host = "smtp.example.com",
            Username = "user@example.com",
            Password = "secret",
            SenderEmail = "noreply@example.com",
        };

        Assert.True(options.IsConfigured);
    }

    [Theory]
    [InlineData("", "user", "pass", "sender@example.com")]
    [InlineData("smtp.example.com", "", "pass", "sender@example.com")]
    [InlineData("smtp.example.com", "user", "", "sender@example.com")]
    [InlineData("smtp.example.com", "user", "pass", "")]
    public void SmtpOptions_MissingAnyField_IsConfiguredFalse(string host, string username, string password, string senderEmail)
    {
        var options = new SmtpOptions { Host = host, Username = username, Password = password, SenderEmail = senderEmail };

        Assert.False(options.IsConfigured);
    }

    [Fact]
    public async Task DevEmailSender_DoesNotThrow_AndSendsNothingExternally()
    {
        var sut = new DevEmailSender(NullLogger<DevEmailSender>.Instance);

        // Only asserts it completes safely — a Development-only sender must never fail the
        // calling flow, since forgot-password's response must not depend on delivery.
        await sut.SendAsync("someone@example.com", "Subject", "<p>html</p>", "text");
    }

    [Fact]
    public async Task NullEmailSender_DoesNotThrow()
    {
        var sut = new NullEmailSender(NullLogger<NullEmailSender>.Instance);

        await sut.SendAsync("someone@example.com", "Subject", "<p>html</p>", "text");
    }

    [Fact]
    public void PasswordResetEmailTemplate_ContainsCodeAndExpiry_InBothFormats()
    {
        var (subject, html, text) = PasswordResetEmailTemplate.Build("123456", 10);

        Assert.Contains("123456", html);
        Assert.Contains("123456", text);
        Assert.Contains("10", html);
        Assert.Contains("10", text);
        Assert.Contains("ignore", text, StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(subject));
    }
}
