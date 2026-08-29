import { useState, type FormEvent } from "react";
import { useNavigate, useLocation, Link } from "react-router-dom";
import { BadgeCheck, Sparkles, Target } from "lucide-react";
import { login } from "../api/auth";
import { useAuth } from "../context/AuthContext";
import { getErrorCode, getErrorMessage } from "../utils/errors";
import type { UserRole } from "../types";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import PasswordInput from "../components/ui/PasswordInput";

interface FieldErrors {
  email?: string;
  password?: string;
  general?: string;
}

interface LocationState {
  prefillEmail?: string;
  justRegistered?: boolean;
}

function dashboardPathForRole(role: UserRole): string {
  return role === "Recruiter" ? "/recruiter/dashboard" : "/candidate/dashboard";
}

export default function LoginPage() {
  const location = useLocation();
  const state = (location.state as LocationState | null) ?? {};

  const [email, setEmail] = useState(state.prefillEmail ?? "");
  const [password, setPassword] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState(false);
  const { setSession } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const clientErrors: FieldErrors = {};
    if (!email.trim()) clientErrors.email = "Email is required.";
    if (!password) clientErrors.password = "Password is required.";
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setLoading(true);
    try {
      const auth = await login({ email, password });
      setSession(auth);
      // Role comes from the server's AuthResponse (itself derived from the JWT), never
      // from anything the client could have supplied — the frontend just routes on it.
      navigate(dashboardPathForRole(auth.role));
    } catch (err) {
      const message = getErrorMessage(err, "Login failed");
      const code = getErrorCode(err);

      if (code === "UNAUTHORIZED") {
        // Deliberately shown under password, not email — the backend never reveals
        // whether the email or the password was wrong.
        setFieldErrors({ password: message });
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
          <h2>Welcome back to your career workspace</h2>
          <p>Track applications, discover roles matched to your skills, and manage your hiring pipeline — all in one place.</p>
          <div className="auth-brand-features">
            <span className="auth-brand-feature"><Target size={18} /> Personalized job recommendations</span>
            <span className="auth-brand-feature"><BadgeCheck size={18} /> Explainable, local resume matching</span>
            <span className="auth-brand-feature"><Sparkles size={18} /> A single dashboard for your whole search</span>
          </div>
        </div>

        <div className="auth-form-panel">
          <h1>Sign in</h1>
          <p>Enter your credentials to access your dashboard.</p>

          {state.justRegistered && <p className="success" style={{ marginBottom: "1.25rem" }}>Account created — sign in to continue.</p>}

          <form onSubmit={handleSubmit} noValidate>
            <FormField label="Email" htmlFor="login-email" error={fieldErrors.email}>
              <input
                id="login-email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
              />
            </FormField>
            <FormField label="Password" htmlFor="login-password" error={fieldErrors.password}>
              <PasswordInput
                id="login-password"
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
            </FormField>

            {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

            <Button type="submit" loading={loading} fullWidth>
              Sign in
            </Button>
          </form>

          <p className="auth-footer-link">
            No account? <Link to="/register">Create one</Link>
          </p>
        </div>
      </div>
    </div>
  );
}
