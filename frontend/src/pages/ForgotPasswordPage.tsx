import { useState, type FormEvent } from "react";
import { useNavigate, Link } from "react-router-dom";
import { KeyRound, ShieldCheck } from "lucide-react";
import { forgotPassword } from "../api/auth";
import { getErrorMessage } from "../utils/errors";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";

interface FieldErrors {
  email?: string;
  general?: string;
}

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const trimmed = email.trim();
    if (!trimmed) {
      setFieldErrors({ email: "Email is required." });
      return;
    }
    if (!/^\S+@\S+\.\S+$/.test(trimmed)) {
      setFieldErrors({ email: "Enter a valid email address." });
      return;
    }

    setLoading(true);
    try {
      await forgotPassword(trimmed);
      // The backend always returns the same generic response whether or not the email is
      // registered — we move to the code-entry step regardless, so the UI can't be used to
      // probe which emails have accounts.
      navigate("/verify-reset-code", { state: { email: trimmed } });
    } catch (err) {
      setFieldErrors({ general: getErrorMessage(err, "Something went wrong. Please try again.") });
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
          <h2>Let's get you back in</h2>
          <p>Enter the email on your account and we'll send a verification code to reset your password.</p>
          <div className="auth-brand-features">
            <span className="auth-brand-feature"><KeyRound size={18} /> A one-time 6-digit code, valid for 10 minutes</span>
            <span className="auth-brand-feature"><ShieldCheck size={18} /> Your account stays secure the whole way through</span>
          </div>
        </div>

        <div className="auth-form-panel">
          <h1>Forgot password</h1>
          <p>Enter your registered email address and we'll send you a verification code.</p>

          <form onSubmit={handleSubmit} noValidate>
            <FormField label="Email" htmlFor="forgot-email" error={fieldErrors.email}>
              <input
                id="forgot-email"
                type="email"
                autoComplete="email"
                autoFocus
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </FormField>

            {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

            <Button type="submit" loading={loading} fullWidth>
              Send verification code
            </Button>
          </form>

          <p className="auth-footer-link">
            Remembered your password? <Link to="/login">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
