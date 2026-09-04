import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ArrowRight, Award, Bell, Bookmark, CalendarClock, ClipboardList, Eye, FileText, Heart, Mail, Search, Sparkles, Target, UserRound } from "lucide-react";
import { getCandidateDashboard } from "../api/dashboard";
import { acceptInvitation, declineInvitation, dismissInvitation, getMyInvitations } from "../api/invitations";
import { getFollowedCompanies, unfollowCompany, updateFollowNotifyPreference } from "../api/follows";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { getErrorMessage } from "../utils/errors";
import type { CandidateDashboard, FollowedCompany, Invitation, JobPosting } from "../types";
import { toIST } from "../utils/format";
import Card from "../components/ui/Card";
import StatCard from "../components/ui/StatCard";
import SectionHeader from "../components/ui/SectionHeader";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import Avatar from "../components/ui/Avatar";
import { resolveAvatarUrl } from "../utils/format";

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
  const toast = useToast();
  const [dashboard, setDashboard] = useState<CandidateDashboard | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [invitations, setInvitations] = useState<Invitation[]>([]);
  const [followedCompanies, setFollowedCompanies] = useState<FollowedCompany[]>([]);

  useEffect(() => {
    getCandidateDashboard()
      .then(setDashboard)
      .catch((err) => setError(getErrorMessage(err, "Failed to load your dashboard")))
      .finally(() => setLoading(false));
    getMyInvitations()
      .then((all) => setInvitations(all.filter((i) => i.status === "Sent" || i.status === "Viewed")))
      .catch(() => setInvitations([]));
    getFollowedCompanies()
      .then(setFollowedCompanies)
      .catch(() => setFollowedCompanies([]));
  }, []);

  async function handleUnfollowCompany(companyId: number) {
    try {
      await unfollowCompany(companyId);
      setFollowedCompanies((prev) => prev.filter((f) => f.companyId !== companyId));
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to unfollow company"));
    }
  }

  async function handleToggleNotify(companyId: number, notify: boolean) {
    setFollowedCompanies((prev) => prev.map((f) => (f.companyId === companyId ? { ...f, notifyOnNewJob: notify } : f)));
    try {
      await updateFollowNotifyPreference(companyId, notify);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update notification preference"));
      setFollowedCompanies((prev) => prev.map((f) => (f.companyId === companyId ? { ...f, notifyOnNewJob: !notify } : f)));
    }
  }

  async function handleAccept(id: number) {
    try {
      await acceptInvitation(id);
      setInvitations((prev) => prev.filter((i) => i.id !== id));
      toast.success("Invitation accepted.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to accept invitation"));
    }
  }

  async function handleDecline(id: number) {
    try {
      await declineInvitation(id);
      setInvitations((prev) => prev.filter((i) => i.id !== id));
      toast.success("Invitation declined.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to decline invitation"));
    }
  }

  async function handleDismiss(id: number) {
    try {
      await dismissInvitation(id);
      setInvitations((prev) => prev.filter((i) => i.id !== id));
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to dismiss invitation"));
    }
  }

  if (loading) return <p>Loading...</p>;
  if (error || !dashboard) return <p className="error">{error ?? "Dashboard unavailable."}</p>;

  const { applicationSummary } = dashboard;

  return (
    <div className="dashboard-page">
      <div className="page-header dashboard-welcome">
        <div style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
          <Avatar name={user?.fullName ?? "?"} size={56} src={resolveAvatarUrl(user?.avatarUrl)} />
          <div>
            <h1>Welcome back{user ? `, ${user.fullName.split(" ")[0]}` : ""}</h1>
            <p>Here's how your job search is going.</p>
          </div>
        </div>
      </div>

      {dashboard.nextBestActions.length > 0 && (
        <Card>
          <SectionHeader title="Next best actions" subtitle="A few things that would help your search right now." as="h3" />
          <ul className="job-list-compact">
            {dashboard.nextBestActions.map((action) => (
              <li key={action.label} className="job-card job-card-compact">
                <Link to={action.linkPath}>
                  <h4><Sparkles size={16} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />{action.label}</h4>
                </Link>
                <p className="hint">{action.description}</p>
                <Link to={action.linkPath} className="link-button" style={{ display: "inline-flex", alignItems: "center", gap: "0.25rem", marginTop: "0.3rem" }}>
                  Go <ArrowRight size={13} />
                </Link>
              </li>
            ))}
          </ul>
        </Card>
      )}

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

      {invitations.length > 0 && (
        <Card>
          <SectionHeader title="Invitations to apply" as="h3" />
          <ul className="job-list-compact">
            {invitations.map((inv) => (
              <li key={inv.id} className="job-card job-card-compact">
                <Link to={`/jobs/${inv.jobPostingId}`}>
                  <h4><Mail size={16} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />{inv.jobTitle}</h4>
                </Link>
                <p>{inv.companyName} · invited by {inv.invitedByName}</p>
                {inv.message && <p className="hint">"{inv.message}"</p>}
                <div style={{ display: "flex", gap: "0.75rem", marginTop: "0.5rem" }}>
                  <Link to={`/jobs/${inv.jobPostingId}`} className="btn btn-primary btn-sm">View job</Link>
                  <button type="button" className="link-button" onClick={() => handleAccept(inv.id)}>Accept</button>
                  <button type="button" className="link-button" onClick={() => handleDecline(inv.id)}>Decline</button>
                  <button type="button" className="link-button" onClick={() => handleDismiss(inv.id)}>Dismiss</button>
                </div>
              </li>
            ))}
          </ul>
        </Card>
      )}

      {dashboard.upcomingInterviews.length > 0 && (
        <Card>
          <SectionHeader title="Upcoming interviews" action={<Link to="/interviews">View all →</Link>} as="h3" />
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

      {dashboard.recentAssessmentResults.length > 0 && (
        <Card>
          <SectionHeader title="Skill assessments" action={<Link to="/assessments">View all →</Link>} as="h3" />
          <ul className="job-list-compact">
            {dashboard.recentAssessmentResults.map((r) => (
              <li key={r.attemptId} className="job-card job-card-compact">
                <h4><Award size={16} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />{r.category}</h4>
                <p>{r.percentageScore}% ({r.scoreCorrectCount}/{r.totalQuestionCount}) · {new Date(r.submittedAtUtc).toLocaleDateString()}</p>
              </li>
            ))}
          </ul>
        </Card>
      )}

      <Card>
        <SectionHeader title="Career goals" action={<Link to="/career-goals">View all →</Link>} as="h3" />
        {dashboard.careerGoalsSummary.goals.length === 0 ? (
          <EmptyState
            icon={<Target size={28} />}
            title="No career goals yet"
            description="Set a goal — a target role or skill — and track your progress toward it."
            action={<Link to="/career-goals" className="btn btn-secondary btn-sm">Set a goal</Link>}
          />
        ) : (
          <ul className="job-list-compact">
            {dashboard.careerGoalsSummary.goals.slice(0, 3).map((g) => (
              <li key={g.id} className="job-card job-card-compact">
                <h4>{g.targetRole || g.targetSkill || g.targetCompanyType}</h4>
                <div className="progress-bar" style={{ margin: "0.4rem 0" }}>
                  <div className="progress-bar-fill" style={{ width: `${g.progressPercent}%` }} />
                </div>
                <p className="hint">{g.progressPercent}% complete · {g.status}</p>
              </li>
            ))}
          </ul>
        )}
      </Card>

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

        <Card>
          <h2><Heart size={18} /> Followed companies</h2>
          {followedCompanies.length === 0 ? (
            <p className="hint">Follow a company from its profile page to get notified about new jobs.</p>
          ) : (
            <ul className="job-list-compact">
              {followedCompanies.map((f) => (
                <li key={f.companyId} className="job-card job-card-compact">
                  <Link to={`/companies/${f.companyId}`}>
                    <h4>{f.companyName}</h4>
                  </Link>
                  {f.industry && <p className="hint">{f.industry}</p>}
                  <div style={{ display: "flex", alignItems: "center", gap: "0.75rem", marginTop: "0.4rem" }}>
                    <label className="filter-option" style={{ padding: 0 }}>
                      <input
                        type="checkbox"
                        checked={f.notifyOnNewJob}
                        onChange={(e) => handleToggleNotify(f.companyId, e.target.checked)}
                      />
                      Notify me of new jobs
                    </label>
                    <button type="button" className="link-button" onClick={() => handleUnfollowCompany(f.companyId)}>Unfollow</button>
                  </div>
                </li>
              ))}
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
