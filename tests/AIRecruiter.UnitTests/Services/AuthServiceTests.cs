using System.Text.RegularExpressions;
using AIRecruiter.Application.DTOs.Auth;
using AIRecruiter.Application.Exceptions;
using AIRecruiter.Domain.Entities;
using AIRecruiter.Domain.Enums;
using AIRecruiter.Infrastructure.Options;
using AIRecruiter.Infrastructure.Persistence;
using AIRecruiter.Infrastructure.Services;
using AIRecruiter.UnitTests.TestHelpers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AIRecruiter.UnitTests.Services;

public class AuthServiceTests
{
    private static (AuthService Sut, RecordingEmailSender EmailSender) CreateSut(AppDbContext db)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Secret = "unit-test-secret-key-at-least-32-characters-long",
            Issuer = "AIRecruiter.Tests",
            Audience = "AIRecruiter.Tests",
            ExpiryMinutes = 60,
        });
        var tokenService = new TokenService(jwtOptions);
        var emailSender = new RecordingEmailSender();
        var sut = new AuthService(
            db, tokenService, TestServiceFactory.CreateAuditLog(db), emailSender,
            new InMemoryIpRateLimiter(), NullLogger<AuthService>.Instance);
        return (sut, emailSender);
    }

    private static string ExtractCode(string emailText)
    {
        var match = Regex.Match(emailText, @"\b\d{6}\b");
        Assert.True(match.Success, "Expected a 6-digit code in the email body.");
        return match.Value;
    }

    [Fact]
    public async Task RegisterAsync_NormalizesEmailCasingAndWhitespace()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);

        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "  Casey@Example.COM  ", "Passw0rd!", UserRole.Candidate));

        var stored = Assert.Single(db.Users);
        Assert.Equal("casey@example.com", stored.Email);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmailDifferentCasing_ThrowsConflict()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        await Assert.ThrowsAsync<ConflictException>(
            () => sut.RegisterAsync(new RegisterRequest("Casey Duplicate", "CASEY@EXAMPLE.COM", "Passw0rd!", UserRole.Candidate)));
    }

    [Fact]
    public async Task RegisterAsync_AdminRole_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.RegisterAsync(new RegisterRequest("Wannabe Admin", "admin@example.com", "Passw0rd!", UserRole.Admin)));
    }

    [Fact]
    public async Task RegisterAsync_NeverStoresPlainTextPassword()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);

        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var stored = Assert.Single(db.Users);
        Assert.NotEqual("Passw0rd!", stored.PasswordHash);
        Assert.StartsWith("$2", stored.PasswordHash); // BCrypt hash prefix
    }

    [Fact]
    public async Task LoginAsync_CaseInsensitiveEmail_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var result = await sut.LoginAsync(new LoginRequest("CASEY@example.com", "Passw0rd!"));

        Assert.Equal("casey@example.com", result.Email);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmail_ThrowsGenericUnauthorized()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync(new LoginRequest("nobody@example.com", "whatever")));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ThrowsSameGenericMessageAsUnknownEmail()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync(new LoginRequest("casey@example.com", "WrongPassword!")));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_DeactivatedAccount_ThrowsSameGenericMessage()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var user = Assert.Single(db.Users);
        user.IsActive = false;
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => sut.LoginAsync(new LoginRequest("casey@example.com", "Passw0rd!")));

        Assert.Equal("Invalid email or password.", ex.Message);
    }

    // --- Forgot password / reset flow ---

    [Fact]
    public async Task ForgotPasswordAsync_KnownAndUnknownEmail_ReturnSameGenericMessage()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        var known = await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var unknown = await sut.ForgotPasswordAsync(new ForgotPasswordRequest("nobody@example.com"), "1.1.1.2");

        Assert.Equal(known.Message, unknown.Message);
    }

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_DoesNotCreateCodeOrSendEmail()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("nobody@example.com"), "1.1.1.1");

        Assert.Empty(db.PasswordResetCodes);
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task ForgotPasswordAsync_KnownEmail_StoresOnlyHashedCode()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");

        var codeRow = Assert.Single(db.PasswordResetCodes);
        var rawCode = ExtractCode(emailSender.SentEmails[0].Text);

        Assert.NotEqual(rawCode, codeRow.CodeHash);
        Assert.StartsWith("$2", codeRow.CodeHash); // BCrypt hash prefix — never the raw code
        Assert.True(codeRow.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(9));
        Assert.False(codeRow.IsUsed);
        Assert.Equal(0, codeRow.FailedAttempts);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SecondRequest_InvalidatesFirstCode()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var firstCode = Assert.Single(db.PasswordResetCodes);
        firstCode.CreatedAt = DateTime.UtcNow.AddMinutes(-2); // step past the resend cooldown
        await db.SaveChangesAsync();

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");

        Assert.Equal(2, db.PasswordResetCodes.Count());
        Assert.True(firstCode.IsUsed);
    }

    [Fact]
    public async Task ForgotPasswordAsync_WithinResendCooldown_DoesNotIssueSecondCode()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");

        Assert.Single(db.PasswordResetCodes);
    }

    [Fact]
    public async Task VerifyResetCodeAsync_CorrectCode_Succeeds()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);

        var result = await sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1");

        Assert.False(string.IsNullOrWhiteSpace(result.ResetToken));
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task VerifyResetCodeAsync_WrongCode_ThrowsGenericValidationError()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", "000000"), "1.1.1.1"));

        Assert.Equal("Invalid or expired code.", ex.Message);
    }

    [Fact]
    public async Task VerifyResetCodeAsync_UnknownEmail_ThrowsSameGenericErrorAsWrongCode()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("nobody@example.com", "123456"), "1.1.1.1"));

        Assert.Equal("Invalid or expired code.", ex.Message);
    }

    [Fact]
    public async Task VerifyResetCodeAsync_ExpiredCode_IsRejected()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);

        var codeRow = Assert.Single(db.PasswordResetCodes);
        codeRow.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1"));
    }

    [Fact]
    public async Task VerifyResetCodeAsync_FiveWrongAttempts_LocksOutTheCorrectCodeToo()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", "000000"), "1.1.1.1"));
        }

        // The 6th attempt would be the correct code, but the code is now locked out.
        await Assert.ThrowsAsync<ValidationException>(
            () => sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1"));
    }

    [Fact]
    public async Task VerifyResetCodeAsync_CodeAlreadyUsed_CannotBeVerifiedAgain()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);

        await sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1");

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1"));
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndRotatesSecurityStamp()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        var originalStamp = Assert.Single(db.Users).SecurityStamp;

        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);
        var verified = await sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1");

        await sut.ResetPasswordAsync(new ResetPasswordRequest(verified.ResetToken, "NewPassw0rd!", "NewPassw0rd!"));

        var user = Assert.Single(db.Users);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassw0rd!", user.PasswordHash));
        Assert.NotEqual(originalStamp, user.SecurityStamp);

        // The old password no longer works.
        await Assert.ThrowsAsync<UnauthorizedException>(() => sut.LoginAsync(new LoginRequest("casey@example.com", "Passw0rd!")));
        var loggedIn = await sut.LoginAsync(new LoginRequest("casey@example.com", "NewPassw0rd!"));
        Assert.Equal("casey@example.com", loggedIn.Email);
    }

    [Fact]
    public async Task ResetPasswordAsync_MismatchedConfirmPassword_ThrowsValidation()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);
        var verified = await sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1");

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => sut.ResetPasswordAsync(new ResetPasswordRequest(verified.ResetToken, "NewPassw0rd!", "Different1!")));

        Assert.Equal("Passwords do not match.", ex.Message);
    }

    [Fact]
    public async Task ResetPasswordAsync_TokenReuse_IsRejected()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);
        var verified = await sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1");

        await sut.ResetPasswordAsync(new ResetPasswordRequest(verified.ResetToken, "NewPassw0rd!", "NewPassw0rd!"));

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ResetPasswordAsync(new ResetPasswordRequest(verified.ResetToken, "AnotherPass1!", "AnotherPass1!")));
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_IsRejected()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, emailSender) = CreateSut(db);
        await sut.RegisterAsync(new RegisterRequest("Casey Candidate", "casey@example.com", "Passw0rd!", UserRole.Candidate));
        await sut.ForgotPasswordAsync(new ForgotPasswordRequest("casey@example.com"), "1.1.1.1");
        var code = ExtractCode(emailSender.SentEmails[0].Text);
        var verified = await sut.VerifyResetCodeAsync(new VerifyResetCodeRequest("casey@example.com", code), "1.1.1.1");

        var codeRow = Assert.Single(db.PasswordResetCodes);
        codeRow.ResetTokenExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ResetPasswordAsync(new ResetPasswordRequest(verified.ResetToken, "NewPassw0rd!", "NewPassw0rd!")));
    }

    [Fact]
    public async Task ResetPasswordAsync_InvalidToken_IsRejected()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);

        await Assert.ThrowsAsync<ValidationException>(
            () => sut.ResetPasswordAsync(new ResetPasswordRequest("not-a-real-token", "NewPassw0rd!", "NewPassw0rd!")));
    }

    [Fact]
    public async Task ForgotPasswordAsync_TooManyRequestsFromSameIp_IsRateLimited()
    {
        using var db = TestDbContextFactory.Create();
        var (sut, _) = CreateSut(db);

        for (var i = 0; i < 10; i++)
        {
            await sut.ForgotPasswordAsync(new ForgotPasswordRequest($"user{i}@example.com"), "9.9.9.9");
        }

        await Assert.ThrowsAsync<RateLimitedException>(
            () => sut.ForgotPasswordAsync(new ForgotPasswordRequest("one-more@example.com"), "9.9.9.9"));
    }
}
