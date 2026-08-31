import { Link } from "react-router-dom";
import { Mail, MapPin, Phone } from "lucide-react";
import { useAuth } from "../context/AuthContext";

const POPULAR_ROLES = [
  "Software Developer",
  "Java Developer",
  ".NET Developer",
  "React Developer",
  "Full Stack Developer",
  "Data Analyst",
  "Data Scientist",
  "UI/UX Designer",
  "Digital Marketing Executive",
  "HR Executive",
];

function roleSearchPath(role: string): string {
  return `/jobs?q=${encodeURIComponent(role)}`;
}

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

          <nav className="app-footer-roles" aria-label="Popular job roles">
            <h4>Popular Job Roles</h4>
            <div className="app-footer-roles-grid">
              {POPULAR_ROLES.map((role) => (
                <Link key={role} to={roleSearchPath(role)}>{role}</Link>
              ))}
            </div>
          </nav>

          <div>
            <h4>Contact</h4>
            <a href="mailto:support@airecruiter-demo.in" className="app-footer-contact-item">
              <Mail size={14} aria-hidden="true" /> support@airecruiter-demo.in
            </a>
            <a href="tel:+919000000000" className="app-footer-contact-item">
              <Phone size={14} aria-hidden="true" /> +91 90000 00000
            </a>
            <span className="app-footer-contact-item">
              <MapPin size={14} aria-hidden="true" /> Bengaluru, Karnataka, India
            </span>
          </div>
        </div>
      </div>

      <div className="app-footer-bottom">
        <span>© 2026 AI Recruiter. Portfolio demonstration project.</span>
        <div className="app-footer-bottom-links">
          <Link to="/privacy">Privacy Policy</Link>
          <Link to="/terms">Terms</Link>
        </div>
      </div>
    </footer>
  );
}
