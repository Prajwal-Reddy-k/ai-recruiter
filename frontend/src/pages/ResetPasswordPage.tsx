import { useState, type FormEvent } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { CheckCircle2, ShieldCheck } from "lucide-react";
import { resetPassword } from "../api/auth";
import { getErrorMessage } from "../utils/errors";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import PasswordInput from "../components/ui/PasswordInput";

interface LocationState {
  resetToken?: string;
  email?: string;
}

interface FieldErrors {
  newPassword?: string;
  confirmPassword?: string;
  general?: string;
}

function passwordStrengthHints(password: string): { label: string; met: boolean }[] {
  return [
    { label: "At least 6 characters", met: password.length >= 6 },
    { label: "Contains a letter", met: /[a-zA-Z]/.test(password) },
    { label: "Contains a number", met: /\d/.test(password) },
  ];
}

export default function ResetPasswordPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const resetToken = (location.state as LocationState | null)?.resetToken;

  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState(false);

  if (!resetToken) {
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

  const hints = passwordStrengthHints(newPassword);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const clientErrors: FieldErrors = {};
    if (newPassword.length < 6) clientErrors.newPassword = "Password must be at least 6 characters.";
    if (newPassword !== confirmPassword) clientErrors.confirmPassword = "Passwords do not match.";
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setLoading(true);
    try {
      await resetPassword(resetToken!, newPassword, confirmPassword);
      // Clear sensitive local state before navigating away — nothing password-related is
      // ever persisted to localStorage in the first place.
      setNewPassword("");
      setConfirmPassword("");
      navigate("/login", { state: { passwordReset: true } });
    } catch (err) {
      setFieldErrors({ general: getErrorMessage(err, "Failed to reset password. Please request a new code.") });
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="auth-card">
        <div className="auth-brand-panel">
          <span className="brand-mark">
            <span className="brand-ai">AI</span> Recruiter
          </span>
          <h2>Almost there</h2>
          <p>Choose a new password for your account. You'll need to sign in again once it's updated.</p>
          <div className="auth-brand-features">
            <span className="auth-brand-feature"><ShieldCheck size={18} /> Hashed with BCrypt, never stored in plain text</span>
            <span className="auth-brand-feature"><CheckCircle2 size={18} /> All existing sessions are signed out for safety</span>
          </div>
        </div>

        <div className="auth-form-panel">
          <h1>Set a new password</h1>
          <p>Choose a strong new password for your account.</p>

          <form onSubmit={handleSubmit} noValidate>
            <FormField label="New password" htmlFor="reset-new-password" error={fieldErrors.newPassword}>
              <PasswordInput
                id="reset-new-password"
                autoComplete="new-password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
              />
            </FormField>

            {newPassword.length > 0 && (
              <ul className="password-strength-hints" aria-live="polite">
                {hints.map((hint) => (
                  <li key={hint.label} className={hint.met ? "password-strength-met" : ""}>
                    {hint.met ? "✓" : "○"} {hint.label}
                  </li>
                ))}
              </ul>
            )}

            <FormField label="Confirm new password" htmlFor="reset-confirm-password" error={fieldErrors.confirmPassword}>
              <PasswordInput
                id="reset-confirm-password"
                autoComplete="new-password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
              />
            </FormField>

            {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

            <Button type="submit" loading={loading} fullWidth>
              Update password
            </Button>
          </form>

          <p className="auth-footer-link">
            <Link to="/login">Back to sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
