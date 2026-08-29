import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

export default function Footer() {
  const { user } = useAuth();

  return (
    <footer className="app-footer">
      <div className="app-footer-inner">
        <div className="app-footer-brand">
          <span className="brand-mark">
            <span className="brand-ai">AI</span> Recruiter
          </span>
          <p>A modern hiring platform concept — built as a portfolio project, not a production service.</p>
        </div>

        <div className="app-footer-links">
          <div>
            <h4>Product</h4>
            <Link to="/jobs">Browse Jobs</Link>
            {!user && <Link to="/register">Create Account</Link>}
            {!user && <Link to="/login">Sign In</Link>}
          </div>

          {user?.role === "Candidate" && (
            <div>
              <h4>For Candidates</h4>
              <Link to="/candidate/dashboard">Dashboard</Link>
              <Link to="/applications">My Applications</Link>
              <Link to="/profile">Edit Profile</Link>
            </div>
          )}

          {user?.role === "Recruiter" && (
            <div>
              <h4>For Recruiters</h4>
              <Link to="/recruiter/dashboard">Dashboard</Link>
              <Link to="/post-job">Post a Job</Link>
              <Link to="/onboarding">Company Profile</Link>
            </div>
          )}

          {!user && (
            <div>
              <h4>For Recruiters</h4>
              <Link to="/register">Post a Job</Link>
            </div>
          )}
        </div>
      </div>

      <div className="app-footer-bottom">
        <span>© {new Date().getFullYear()} AI Recruiter — demo project. Not affiliated with any real recruitment service.</span>
      </div>
    </footer>
  );
}
