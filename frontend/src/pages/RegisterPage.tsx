import { useEffect, useState, type FormEvent } from "react";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import { Briefcase, Mail, Search, UserRound } from "lucide-react";
import { register } from "../api/auth";
import { resolveReferralToken } from "../api/referrals";
import type { ReferralTokenPreview, UserRole } from "../types";
import { getErrorCode, getErrorMessage, getFieldErrors } from "../utils/errors";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import PasswordInput from "../components/ui/PasswordInput";

interface FieldErrors {
  fullName?: string;
  email?: string;
  password?: string;
  general?: string;
}

export default function RegisterPage() {
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<UserRole>("Candidate");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const referralToken = searchParams.get("ref") ?? undefined;
  const [referralPreview, setReferralPreview] = useState<ReferralTokenPreview | null>(null);

  useEffect(() => {
    if (!referralToken) return;
    // Invalid/expired tokens fail silently — a bad referral link should never block
    // registration, it just means no welcome banner shows.
    resolveReferralToken(referralToken).then(setReferralPreview).catch(() => setReferralPreview(null));
  }, [referralToken]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const clientErrors: FieldErrors = {};
    const trimmedName = fullName.trim();
    if (!trimmedName) {
      clientErrors.fullName = "Full name is required.";
    } else if (trimmedName.length < 2 || trimmedName.length > 100) {
      clientErrors.fullName = "Full name must be between 2 and 100 characters.";
    } else if (!/^[\p{L}\s.'-]+$/u.test(trimmedName)) {
      clientErrors.fullName = "Full name can only contain letters, spaces, and reasonable punctuation.";
    }
    if (!email.trim()) clientErrors.email = "Email is required.";
    if (password.length < 6) clientErrors.password = "Password must be at least 6 characters.";
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setLoading(true);
    try {
      await register({ fullName, email, password, role, referralToken });
      // Registration succeeds but does not log the user in — they confirm their
      // credentials once more on the Login page, which also keeps the login code path
      // (and its audit trail) as the single place a session actually gets created.
      navigate("/login", { state: { prefillEmail: email, justRegistered: true } });
    } catch (err) {
      const message = getErrorMessage(err, "Registration failed");
      const code = getErrorCode(err);
      const serverFieldErrors = getFieldErrors(err);

      if (code === "EMAIL_TAKEN") {
        setFieldErrors({ email: message });
      } else if (serverFieldErrors) {
        setFieldErrors(serverFieldErrors as FieldErrors);
      } else {
        setFieldErrors({ general: message });
      }
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
          <h2>Start building your next chapter</h2>
          <p>Whether you're hunting for your next role or your next hire, one account gets you a workspace built around it.</p>
          <div className="auth-brand-features">
            <span className="auth-brand-feature"><Search size={18} /> Search thousands of open roles</span>
            <span className="auth-brand-feature"><Briefcase size={18} /> Post jobs and manage applicants</span>
            <span className="auth-brand-feature"><UserRound size={18} /> A profile that works for you</span>
          </div>
        </div>

        <div className="auth-form-panel">
          <h1>Create account</h1>
          <p>It only takes a minute to get started.</p>

          {referralPreview && (
            <p className="hint" style={{ marginBottom: "1rem" }}>
              <Mail size={14} style={{ verticalAlign: "-2px", marginRight: "0.3rem" }} />
              You were referred to <strong>{referralPreview.jobTitle}</strong> at <strong>{referralPreview.companyName}</strong>.
            </p>
          )}

          <form onSubmit={handleSubmit} noValidate>
            <div className="form-field">
              <span className="form-field-label">I am a</span>
              <div className="role-picker">
                <button
                  type="button"
                  className={`role-option ${role === "Candidate" ? "role-option-selected" : ""}`}
                  onClick={() => setRole("Candidate")}
                  aria-pressed={role === "Candidate"}
                >
                  <UserRound size={22} />
                  Candidate
                </button>
                <button
                  type="button"
                  className={`role-option ${role === "Recruiter" ? "role-option-selected" : ""}`}
                  onClick={() => setRole("Recruiter")}
                  aria-pressed={role === "Recruiter"}
                >
                  <Briefcase size={22} />
                  Recruiter
                </button>
              </div>
            </div>

            <FormField label="Full name" htmlFor="register-name" error={fieldErrors.fullName}>
              <input id="register-name" autoComplete="name" value={fullName} onChange={(e) => setFullName(e.target.value)} />
            </FormField>
            <FormField label="Email" htmlFor="register-email" error={fieldErrors.email}>
              <input
                id="register-email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </FormField>
            <FormField
              label="Password"
              htmlFor="register-password"
              error={fieldErrors.password}
              hint="At least 6 characters."
            >
              <PasswordInput
                id="register-password"
                autoComplete="new-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </FormField>

            {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

            <Button type="submit" loading={loading} fullWidth>
              Create account
            </Button>
          </form>

          <p className="auth-footer-link">
            Already have an account? <Link to="/login">Sign in</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
