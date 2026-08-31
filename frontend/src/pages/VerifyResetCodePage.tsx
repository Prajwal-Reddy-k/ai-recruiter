import { useEffect, useState } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { MailCheck, ShieldCheck } from "lucide-react";
import { forgotPassword, verifyResetCode } from "../api/auth";
import { getErrorMessage } from "../utils/errors";
import Button from "../components/ui/Button";
import OtpInput from "../components/ui/OtpInput";

const RESEND_COOLDOWN_SECONDS = 60;

interface LocationState {
  email?: string;
}

export default function VerifyResetCodePage() {
  const location = useLocation();
  const navigate = useNavigate();
  const email = (location.state as LocationState | null)?.email;

  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [cooldown, setCooldown] = useState(RESEND_COOLDOWN_SECONDS);

  useEffect(() => {
    if (!email) return;
    if (cooldown <= 0) return;
    const timer = setInterval(() => setCooldown((c) => Math.max(0, c - 1)), 1000);
    return () => clearInterval(timer);
  }, [email, cooldown]);

  // No email in navigation state means this page was opened directly (e.g. a refresh) —
  // there is nothing to verify against, so send the user back to start the flow properly.
  if (!email) {
    return (
      <div className="auth-shell">
        <div className="auth-card">
          <div className="auth-form-panel" style={{ gridColumn: "1 / -1" }}>
            <h1>Session expired</h1>
            <p>Please start the password reset process again.</p>
            <Link to="/forgot-password" className="btn btn-primary" style={{ marginTop: "1rem", display: "inline-block" }}>
              Start over
            </Link>
          </div>
        </div>
      </div>
    );
  }

  async function handleSubmit() {
    setError(null);
    if (code.length !== 6) {
      setError("Enter the full 6-digit code.");
      return;
    }

    setLoading(true);
    try {
      const result = await verifyResetCode(email!, code);
      navigate("/reset-password", { state: { resetToken: result.resetToken, email } });
    } catch (err) {
      setError(getErrorMessage(err, "Invalid or expired code."));
      setCode("");
    } finally {
      setLoading(false);
    }
  }

  async function handleResend() {
    setError(null);
    setResending(true);
    try {
      await forgotPassword(email!);
      setCooldown(RESEND_COOLDOWN_SECONDS);
      setCode("");
    } catch (err) {
      setError(getErrorMessage(err, "Failed to resend the code. Please try again."));
    } finally {
      setResending(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="auth-brand-panel">
          <span className="brand-mark">
            <span className="brand-ai">AI</span> Recruiter
          </span>
          <h2>Check your inbox</h2>
          <p>We sent a 6-digit verification code to your email. It expires in 10 minutes.</p>
          <div className="auth-brand-features">
            <span className="auth-brand-feature"><MailCheck size={18} /> Sent to {email}</span>
            <span className="auth-brand-feature"><ShieldCheck size={18} /> Codes can only be used once</span>
          </div>
        </div>

        <div className="auth-form-panel">
          <h1>Enter verification code</h1>
          <p>Enter the 6-digit code we sent to <strong>{email}</strong>.</p>

          <form
            onSubmit={(e) => {
              e.preventDefault();
              void handleSubmit();
            }}
            noValidate
          >
            <div className="form-field">
              <span className="form-field-label" id="otp-label">Verification code</span>
              <OtpInput id="reset-code" value={code} onChange={setCode} disabled={loading} />
            </div>

            {error && <p className="error" role="alert" style={{ margin: "1rem 0" }}>{error}</p>}

            <Button type="submit" loading={loading} disabled={code.length !== 6} fullWidth>
              Verify code
            </Button>
          </form>

          <p className="auth-footer-link">
            {cooldown > 0 ? (
              <span className="hint">Resend code in {cooldown}s</span>
            ) : (
              <button type="button" className="link-button" onClick={handleResend} disabled={resending}>
                {resending ? "Sending..." : "Resend code"}
              </button>
            )}
          </p>
          <p className="auth-footer-link">
            <Link to="/forgot-password">Use a different email</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
