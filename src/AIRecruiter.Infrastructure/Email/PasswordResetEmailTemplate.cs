namespace AIRecruiter.Infrastructure.Email;

/// <summary>Builds the password-reset verification-code email in both HTML and plain-text
/// form. No templating engine/dependency needed for content this small.</summary>
public static class PasswordResetEmailTemplate
{
    public const string AppName = "AI Recruiter";

    public static (string Subject, string Html, string Text) Build(string code, int expiryMinutes)
    {
        var subject = $"{AppName} password reset code";

        var text =
            $"""
            {AppName} — Password Reset

            Your verification code is: {code}

            This code expires in {expiryMinutes} minutes and can only be used once.

            If you did not request a password reset, you can safely ignore this email —
            your password will not be changed.
            """;

        var html = $"""
            <!DOCTYPE html>
            <html>
              <body style="margin:0;padding:0;background:#f4f5f7;font-family:Segoe UI,Arial,sans-serif;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f5f7;padding:32px 0;">
                  <tr>
                    <td align="center">
                      <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;">
                        <tr>
                          <td style="background:#0f172a;padding:24px 32px;">
                            <span style="color:#22c55e;font-weight:700;font-size:20px;">AI</span>
                            <span style="color:#ffffff;font-weight:700;font-size:20px;"> Recruiter</span>
                          </td>
                        </tr>
                        <tr>
                          <td style="padding:32px;">
                            <h1 style="font-size:20px;margin:0 0 12px;color:#0f172a;">Reset your password</h1>
                            <p style="font-size:14px;color:#475569;line-height:1.6;margin:0 0 24px;">
                              Use the verification code below to reset your {AppName} password. This code expires in
                              <strong>{expiryMinutes} minutes</strong> and can only be used once.
                            </p>
                            <div style="background:#f1f5f9;border-radius:8px;padding:20px;text-align:center;margin-bottom:24px;">
                              <span style="font-size:32px;font-weight:700;letter-spacing:8px;color:#0f172a;">{code}</span>
                            </div>
                            <p style="font-size:13px;color:#94a3b8;line-height:1.6;margin:0;">
                              If you did not request a password reset, you can safely ignore this email —
                              your password will not be changed.
                            </p>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                </table>
              </body>
            </html>
            """;

        return (subject, html, text);
    }
}
