import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { BarChart3, Briefcase, Building2, CalendarClock, ClipboardList, FilePlus2, Users } from "lucide-react";
import { getRecruiterDashboard } from "../api/dashboard";
import { getMyActivity } from "../api/recruiters";
import { useAuth } from "../context/AuthContext";
import type { AuditLogEntry, RecruiterDashboard } from "../types";
import { getErrorMessage } from "../utils/errors";
import { toIST } from "../utils/format";
import Card from "../components/ui/Card";
import StatCard from "../components/ui/StatCard";
import SectionHeader from "../components/ui/SectionHeader";
import EmptyState from "../components/ui/EmptyState";
import { StatusBadge } from "../components/ui/Badge";

export default function RecruiterDashboardPage() {
  const { user } = useAuth();
  const [dashboard, setDashboard] = useState<RecruiterDashboard | null>(null);
  const [activity, setActivity] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getRecruiterDashboard()
      .then(setDashboard)
      .catch((err) => setError(getErrorMessage(err, "Failed to load your dashboard")))
      .finally(() => setLoading(false));
    getMyActivity()
      .then(setActivity)
      .catch(() => setActivity([]));
  }, []);

  if (loading) return <p>Loading...</p>;
  if (error || !dashboard) return <p className="error">{error ?? "Dashboard unavailable."}</p>;

  const { onboardingStatus } = dashboard;
  const shortlistedCount = dashboard.recentApplications.filter((a) => a.status === "Shortlisted").length;
  const maxApplicants = Math.max(1, ...dashboard.jobPerformance.map((j) => j.applicantCount));

  return (
    <div className="dashboard-page">
      <div className="page-header dashboard-welcome">
        <div>
          <h1>Welcome back{user ? `, ${user.fullName.split(" ")[0]}` : ""}</h1>
          <p>{onboardingStatus.isOnboarded ? onboardingStatus.companyName : "Complete your company profile to get started."}</p>
        </div>
        <Link to="/post-job" className="btn btn-primary">
          <FilePlus2 size={16} /> Post a Job
        </Link>
      </div>

      {!onboardingStatus.isOnboarded && (
        <div className="onboarding-banner">
          <div className="onboarding-banner-text">
            <h3>Finish setting up your company profile</h3>
            <p>Candidates and your job postings need this before you can start hiring.</p>
          </div>
          <Link to="/onboarding" className="btn btn-primary btn-sm">Complete onboarding</Link>
        </div>
      )}

      <div className="dashboard-grid">
        <StatCard icon={<Briefcase size={20} />} value={dashboard.activeJobPostingCount} label="Active Jobs" tone="primary" />
        <StatCard icon={<Users size={20} />} value={dashboard.totalApplicantCount} label="Total Applicants" tone="primary" />
        <StatCard icon={<ClipboardList size={20} />} value={shortlistedCount} label="Shortlisted (recent)" tone="accent" />
        <StatCard icon={<Building2 size={20} />} value={onboardingStatus.isOnboarded ? "Complete" : "Pending"} label="Company Profile" tone="neutral" />
      </div>

      <Card>
        <h2>Quick actions</h2>
        <ul className="quick-links" style={{ display: "flex", flexDirection: "row", gap: "1.5rem", flexWrap: "wrap" }}>
          <li><Link to="/post-job"><FilePlus2 size={16} /> Post a Job</Link></li>
          <li><Link to="/jobs/mine"><Briefcase size={16} /> Manage Jobs</Link></li>
          <li><Link to="/recruiter/candidates"><Users size={16} /> Candidates</Link></li>
          <li><Link to="/onboarding"><Building2 size={16} /> Company Profile</Link></li>
          <li><Link to="/recruiter/analytics"><BarChart3 size={16} /> Analytics</Link></li>
        </ul>
      </Card>

      {dashboard.upcomingInterviews.length > 0 && (
        <Card>
          <SectionHeader title="Upcoming interviews" action={<Link to="/recruiter/interviews">View all →</Link>} as="h3" />
          <ul className="job-list-compact">
            {dashboard.upcomingInterviews.map((iv) => (
              <li key={iv.interviewId} className="job-card job-card-compact">
                <Link to={`/applications/${iv.jobApplicationId}`}>
                  <h4><CalendarClock size={16} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />{iv.candidateFullName}</h4>
                </Link>
                <p>{iv.jobTitle} · {toIST(iv.startUtc)}</p>
              </li>
            ))}
          </ul>
        </Card>
      )}

      <Card>
        <SectionHeader title="Job performance" subtitle="Applicants per active job posting" as="h3" />
        {dashboard.jobPerformance.length === 0 ? (
          <EmptyState
            icon={<Briefcase size={28} />}
            title="You haven't posted any jobs yet"
            description="Post your first role to start seeing applicants and performance data here."
            action={<Link to="/post-job" className="btn btn-primary btn-sm">Post a Job</Link>}
          />
        ) : (
          <>
            <div className="bar-chart" style={{ marginBottom: "1.5rem" }}>
              {dashboard.jobPerformance.slice(0, 6).map((job) => (
                <div className="bar-chart-row" key={job.jobId}>
                  <span className="bar-chart-label">{job.title}</span>
                  <div className="bar-chart-track">
                    <div
                      className={`bar-chart-fill ${job.status !== "Open" ? "bar-chart-fill-accent" : ""}`}
                      style={{ width: `${(job.applicantCount / maxApplicants) * 100}%` }}
                    />
                  </div>
                  <span className="bar-chart-value">{job.applicantCount}</span>
                </div>
              ))}
            </div>

            <div className="table-scroll">
              <table className="dashboard-table">
                <thead>
                  <tr>
                    <th>Job</th>
                    <th>Status</th>
                    <th>Views</th>
                    <th>Applicants</th>
                    <th></th>
                  </tr>
                </thead>
                <tbody>
                  {dashboard.jobPerformance.map((job) => (
                    <tr key={job.jobId}>
                      <td><Link to={`/jobs/${job.jobId}`}>{job.title}</Link></td>
                      <td><StatusBadge status={job.status} /></td>
                      <td>{job.viewCount}</td>
                      <td>{job.applicantCount}</td>
                      <td><Link to={`/jobs/${job.jobId}/applicants`}>View applicants →</Link></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
      </Card>

      <Card>
        <SectionHeader title="Recent applications" as="h3" />
        {dashboard.recentApplications.length === 0 ? (
          <p className="hint">No applications yet.</p>
        ) : (
          <ul className="job-list-compact">
            {dashboard.recentApplications.map((app) => (
              <li key={app.id} className="job-card job-card-compact">
                <Link to={`/applications/${app.id}`}>
                  <h4>{app.candidateFullName ?? `Application #${app.id}`}</h4>
                </Link>
                <p>
                  {app.jobTitle} · <StatusBadge status={app.status} /> · {new Date(app.createdAt).toLocaleDateString()}
                  {app.matchScore !== null && <> · Match score: {app.matchScore}/100</>}
                </p>
              </li>
            ))}
          </ul>
        )}
      </Card>

      {activity.length > 0 && (
        <Card>
          <SectionHeader title="Recent activity" subtitle="Actions taken on your company's jobs and applications" as="h3" />
          <ul className="job-list-compact">
            {activity.slice(0, 10).map((entry) => (
              <li key={entry.id} className="job-card job-card-compact">
                <p>{entry.actionType} — {entry.entityType} #{entry.entityId ?? "—"}</p>
                <p className="hint">{new Date(entry.timestampUtc).toLocaleString()}</p>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}
