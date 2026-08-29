import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Bell, Bookmark, CalendarClock, ClipboardList, Eye, FileText, Search, UserRound } from "lucide-react";
import { getCandidateDashboard } from "../api/dashboard";
import { useAuth } from "../context/AuthContext";
import type { CandidateDashboard, JobPosting } from "../types";
import { getErrorMessage } from "../utils/errors";
import { toIST } from "../utils/format";
import Card from "../components/ui/Card";
import StatCard from "../components/ui/StatCard";
import SectionHeader from "../components/ui/SectionHeader";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";

function JobRow({ job }: { job: JobPosting }) {
  return (
    <li className="job-card job-card-compact">
      <Link to={`/jobs/${job.id}`}>
        <h4>{job.title}</h4>
      </Link>
      <p>{job.companyName} · {job.displayLocation}</p>
    </li>
  );
}

export default function CandidateDashboardPage() {
  const { user } = useAuth();
  const [dashboard, setDashboard] = useState<CandidateDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getCandidateDashboard()
      .then(setDashboard)
      .catch((err) => setError(getErrorMessage(err, "Failed to load your dashboard")))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading...</p>;
  if (error || !dashboard) return <p className="error">{error ?? "Dashboard unavailable."}</p>;

  const { applicationSummary } = dashboard;

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1>Welcome back{user ? `, ${user.fullName.split(" ")[0]}` : ""}</h1>
        <p>Here's how your job search is going.</p>
      </div>

      <div className="dashboard-grid">
        <Card className="profile-completion-card">
          <h2><UserRound size={18} /> Profile completion</h2>
          <div className="progress-bar">
            <div className="progress-bar-fill" style={{ width: `${dashboard.profileCompletionPercent}%` }} />
          </div>
          <p className="hint">{dashboard.profileCompletionPercent}% complete</p>
          <Link to="/profile">Edit profile →</Link>
        </Card>

        <Card>
          <h2>Quick actions</h2>
          <ul className="quick-links">
            <li><Link to="/jobs"><Search size={16} /> Browse Jobs</Link></li>
            <li><Link to="/applications"><ClipboardList size={16} /> My Applications</Link></li>
            <li><Link to="/profile"><FileText size={16} /> Resume & Profile</Link></li>
            <li><Link to="/saved-jobs"><Bookmark size={16} /> Saved Jobs {dashboard.savedJobs.length > 0 && `(${dashboard.savedJobs.length})`}</Link></li>
            <li><Link to="/alerts"><Bell size={16} /> Job Alerts {dashboard.activeAlertCount > 0 && `(${dashboard.activeAlertCount})`}</Link></li>
          </ul>
        </Card>
      </div>

      {dashboard.alertMatches.length > 0 && (
        <Card>
          <SectionHeader title="New matches for your alerts" action={<Link to="/alerts">Manage alerts →</Link>} as="h3" />
          <ul className="job-list-compact">
            {dashboard.alertMatches.map((job) => <JobRow key={job.id} job={job} />)}
          </ul>
        </Card>
      )}

      {dashboard.upcomingInterviews.length > 0 && (
        <Card>
          <SectionHeader title="Upcoming interviews" as="h3" />
          <ul className="job-list-compact">
            {dashboard.upcomingInterviews.map((iv) => (
              <li key={iv.interviewId} className="job-card job-card-compact">
                <Link to={`/applications/${iv.jobApplicationId}`}>
                  <h4><CalendarClock size={16} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />{iv.jobTitle}</h4>
                </Link>
                <p>{iv.companyName} · {toIST(iv.startUtc)}</p>
              </li>
            ))}
          </ul>
        </Card>
      )}

      <div>
        <SectionHeader title="Application summary" action={<Link to="/applications">View all →</Link>} as="h3" />
        <div className="dashboard-grid">
          <StatCard value={applicationSummary.applied} label="Applied" tone="neutral" />
          <StatCard value={applicationSummary.underReview} label="Under Review" tone="accent" />
          <StatCard value={applicationSummary.shortlisted} label="Shortlisted" tone="primary" />
          <StatCard value={applicationSummary.rejected} label="Rejected" tone="neutral" />
        </div>
      </div>

      <Card>
        <SectionHeader title="Recommended for you" as="h3" />
        {dashboard.recommendedJobs.length === 0 ? (
          <EmptyState
            icon={<Search size={28} />}
            title="No recommendations yet"
            description="Add skills to your profile to get personalized job matches."
            action={<Link to="/profile" className="btn btn-secondary btn-sm">Add skills</Link>}
          />
        ) : (
          <ul className="job-list-compact">
            {dashboard.recommendedJobs.map((job) => <JobRow key={job.id} job={job} />)}
          </ul>
        )}
      </Card>

      {dashboard.skillSuggestions.length > 0 && (
        <Card>
          <SectionHeader title="Skills worth adding" subtitle="Frequently requested by open roles, not yet on your profile." as="h3" />
          <div className="chip-list">
            {dashboard.skillSuggestions.map((skill) => (
              <span key={skill} className="chip chip-missing">{skill}</span>
            ))}
          </div>
        </Card>
      )}

      <div className="dashboard-grid">
        <Card>
          <h2>
            <Eye size={18} /> Recently viewed
            {dashboard.recentlyViewedJobs.isSampleData && <span className="sample-data-badge">Sample data</span>}
          </h2>
          {dashboard.recentlyViewedJobs.items.length === 0 ? (
            <p className="hint">Nothing to show yet.</p>
          ) : (
            <ul className="job-list-compact">
              {dashboard.recentlyViewedJobs.items.map((job) => <JobRow key={job.id} job={job} />)}
            </ul>
          )}
        </Card>

        <Card>
          <h2 style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            Saved jobs
            {dashboard.savedJobs.length > 0 && <Link to="/saved-jobs" style={{ fontSize: "var(--font-sm)" }}>View all →</Link>}
          </h2>
          {dashboard.savedJobs.length === 0 ? (
            <p className="hint">Nothing to show yet — save a job to see it here.</p>
          ) : (
            <ul className="job-list-compact">
              {dashboard.savedJobs.map((job) => <JobRow key={job.id} job={job} />)}
            </ul>
          )}
        </Card>
      </div>

      <Card>
        <SectionHeader title="Recent applications" as="h3" />
        {dashboard.recentApplications.length === 0 ? (
          <EmptyState
            icon={<ClipboardList size={28} />}
            title="No applications yet"
            description="Once you apply to a role, it'll show up here."
            action={<Link to="/jobs" className="btn btn-secondary btn-sm">Browse open roles</Link>}
          />
        ) : (
          <ul className="job-list-compact">
            {dashboard.recentApplications.map((app) => (
              <li key={app.id} className="job-card job-card-compact">
                <Link to={`/applications/${app.id}`}>
                  <h4>{app.jobTitle}</h4>
                </Link>
                <p>
                  {app.companyName} · <StatusBadge status={app.status} /> · {new Date(app.createdAt).toLocaleDateString()}
                </p>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
