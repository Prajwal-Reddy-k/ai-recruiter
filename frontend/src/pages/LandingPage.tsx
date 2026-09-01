import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import {
  Briefcase, Building2, CheckCircle2, ClipboardList, FileSearch, Search, Sparkles, Users,
} from "lucide-react";
import { getOpenJobs } from "../api/jobs";
import { getPlatformStats } from "../api/platform";
import type { JobPosting, PlatformStats } from "../types";
import { SUGGESTED_SEARCHES } from "../utils/searchSuggestions";
import { POPULAR_ROLES, roleSearchPath } from "../utils/popularRoles";
import JobCard from "../components/JobCard";
import Button from "../components/ui/Button";
import StatCard from "../components/ui/StatCard";
import SearchBar from "../components/ui/SearchBar";
import { JobCardSkeleton } from "../components/ui/Skeleton";

export default function LandingPage() {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [location, setLocation] = useState("");
  const [featuredJobs, setFeaturedJobs] = useState<JobPosting[]>([]);
  const [stats, setStats] = useState<PlatformStats | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([getOpenJobs(), getPlatformStats()])
      .then(([jobs, platformStats]) => {
        setFeaturedJobs(jobs.slice(0, 6));
        setStats(platformStats);
      })
      .finally(() => setLoading(false));
  }, []);

  function handleSearch() {
    const params = new URLSearchParams();
    if (query.trim()) params.set("q", query.trim());
    if (location.trim()) params.set("location", location.trim());
    navigate(`/jobs${params.toString() ? `?${params.toString()}` : ""}`);
  }

  return (
    <div>
      <section className="landing-hero">
        <h1>Find the right job. Hire the right people.</h1>
        <p>
          AI Recruiter matches candidates to India-based roles with an explainable, locally-computed
          skill match — and gives recruiters a complete hiring workflow in one place.
        </p>

        <div className="landing-hero-search">
          <SearchBar
            value={query}
            onChange={setQuery}
            onSubmit={handleSearch}
            placeholder="Job title, skill, or company"
            suggestions={SUGGESTED_SEARCHES}
            aria-label="Search jobs by title or skill"
          />
          <input
            type="text"
            value={location}
            onChange={(e) => setLocation(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleSearch()}
            placeholder="City or state"
            aria-label="Location"
            style={{ flex: 1, minWidth: 160, padding: "0.75rem 1rem", border: "1px solid var(--border)", borderRadius: "var(--radius-md)" }}
          />
          <Button onClick={handleSearch} icon={<Search size={16} />}>Find Jobs</Button>
        </div>

        <div className="landing-hero-actions">
          <Link to="/jobs" className="btn btn-secondary" style={{ background: "white" }}>Find Jobs</Link>
          <Link to="/register" className="btn btn-primary">Post a Job</Link>
        </div>

        <nav className="landing-hero-roles" aria-label="Popular job roles">
          {POPULAR_ROLES.slice(0, 6).map((role) => (
            <Link key={role} to={roleSearchPath(role)}>{role}</Link>
          ))}
        </nav>
      </section>

      {stats && (
        <section className="landing-section">
          <div className="dashboard-grid">
            <StatCard icon={<Briefcase size={20} />} value={stats.openJobCount} label="Open jobs" tone="primary" />
            <StatCard icon={<Users size={20} />} value={stats.candidateCount} label="Candidates" tone="accent" />
            <StatCard icon={<Building2 size={20} />} value={stats.companyCount} label="Companies hiring" tone="neutral" />
          </div>
        </section>
      )}

      <section className="landing-section">
        <h2 className="landing-section-title">Featured jobs</h2>
        <p className="landing-section-subtitle">A sample of currently open roles.</p>
        {loading ? (
          <div className="job-list">
            {[1, 2, 3].map((i) => <JobCardSkeleton key={i} />)}
          </div>
        ) : featuredJobs.length === 0 ? (
          <p className="hint" style={{ textAlign: "center" }}>No open jobs right now — check back soon.</p>
        ) : (
          <div className="job-list">
            {featuredJobs.map((job) => <JobCard key={job.id} job={job} />)}
          </div>
        )}
      </section>

      <section className="landing-section landing-features-grid">
        <div>
          <h2>For Candidates</h2>
          <ul className="landing-feature-list">
            <li><CheckCircle2 size={18} /> Explainable, local resume match scoring — see exactly why a job fits</li>
            <li><CheckCircle2 size={18} /> Track every application's status in one dashboard</li>
            <li><CheckCircle2 size={18} /> Save jobs, set alerts, and control who can see your profile</li>
          </ul>
          <div style={{ marginTop: "1.25rem" }}>
            <Link to="/register" className="btn btn-secondary btn-sm">Create a candidate account</Link>
          </div>
        </div>
        <div>
          <h2>For Recruiters</h2>
          <ul className="landing-feature-list">
            <li><CheckCircle2 size={18} /> Post jobs and manage applicants on a Kanban board</li>
            <li><CheckCircle2 size={18} /> Search candidates across your company with CSV export</li>
            <li><CheckCircle2 size={18} /> Schedule interviews and collect structured feedback</li>
          </ul>
          <div style={{ marginTop: "1.25rem" }}>
            <Link to="/register" className="btn btn-secondary btn-sm">Create a recruiter account</Link>
          </div>
        </div>
      </section>

      <section className="landing-section">
        <h2 className="landing-section-title">How it works</h2>
        <div className="landing-steps">
          <div className="landing-step">
            <span className="landing-step-number">1</span>
            <h3><FileSearch size={16} style={{ verticalAlign: "-2px", marginRight: "0.3rem" }} />Create a profile</h3>
            <p className="hint">Candidates add skills and a resume; recruiters set up a company profile.</p>
          </div>
          <div className="landing-step">
            <span className="landing-step-number">2</span>
            <h3><ClipboardList size={16} style={{ verticalAlign: "-2px", marginRight: "0.3rem" }} />Search &amp; apply, or post &amp; review</h3>
            <p className="hint">Find matching roles and apply — or post a job and review applicants.</p>
          </div>
          <div className="landing-step">
            <span className="landing-step-number">3</span>
            <h3><Sparkles size={16} style={{ verticalAlign: "-2px", marginRight: "0.3rem" }} />Interview &amp; hire</h3>
            <p className="hint">Schedule interviews, collect feedback, and move to an offer.</p>
          </div>
        </div>
      </section>

      <section className="landing-cta">
        <h2>Ready to get started?</h2>
        <p>It's free — create an account in under a minute.</p>
        <div className="landing-hero-actions">
          <Link to="/register" className="btn btn-secondary" style={{ background: "white", color: "var(--color-primary)" }}>Create Account</Link>
          <Link to="/jobs" className="btn" style={{ background: "rgba(255,255,255,0.15)", color: "white", border: "1px solid rgba(255,255,255,0.4)" }}>Browse Jobs</Link>
        </div>
      </section>
    </div>
  );
}
