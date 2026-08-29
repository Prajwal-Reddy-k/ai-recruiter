import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Briefcase, Copy, Pencil } from "lucide-react";
import { archiveJob, closeJob, duplicateJob, getMyJobs, publishJob, reopenJob, type JobStatusValue } from "../api/jobs";
import type { RecruiterJobSummary } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import Modal from "../components/ui/Modal";
import Button from "../components/ui/Button";

type Tab = "All" | "Draft" | "Open" | "Closed" | "Archived";
const TABS: { key: Tab; label: string }[] = [
  { key: "All", label: "All" },
  { key: "Draft", label: "Draft" },
  { key: "Open", label: "Published" },
  { key: "Closed", label: "Closed" },
  { key: "Archived", label: "Archived" },
];

type ConfirmAction = { kind: "close" | "archive"; jobId: number; jobTitle: string } | null;

function conversionRate(views: number, applications: number): string {
  if (views === 0) return "No data yet";
  return `${Math.round((applications / views) * 100)}%`;
}

export default function ManageJobsPage() {
  const toast = useToast();
  const [jobs, setJobs] = useState<RecruiterJobSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>("All");
  const [updatingId, setUpdatingId] = useState<number | null>(null);
  const [confirmAction, setConfirmAction] = useState<ConfirmAction>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setJobs(await getMyJobs());
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load your jobs"));
    } finally {
      setLoading(false);
    }
  }

  const filtered = useMemo(
    () => (tab === "All" ? jobs : jobs.filter((s) => s.job.status === tab)),
    [jobs, tab]
  );

  const counts = useMemo(() => {
    const map: Record<Tab, number> = { All: jobs.length, Draft: 0, Open: 0, Closed: 0, Archived: 0 };
    for (const s of jobs) {
      if (s.job.status in map) map[s.job.status as Tab]++;
    }
    return map;
  }, [jobs]);

  function updateLocal(jobId: number, status: JobStatusValue) {
    setJobs((prev) => prev.map((s) => (s.job.id === jobId ? { ...s, job: { ...s.job, status } } : s)));
  }

  async function handlePublish(jobId: number) {
    setUpdatingId(jobId);
    try {
      await publishJob(jobId);
      updateLocal(jobId, "Open");
      toast.success("Job published.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to publish job"));
    } finally {
      setUpdatingId(null);
    }
  }

  async function handleReopen(jobId: number) {
    setUpdatingId(jobId);
    try {
      await reopenJob(jobId);
      updateLocal(jobId, "Open");
      toast.success("Job reopened.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to reopen job"));
    } finally {
      setUpdatingId(null);
    }
  }

  async function handleDuplicate(jobId: number) {
    setUpdatingId(jobId);
    try {
      await duplicateJob(jobId);
      toast.success("Job duplicated as a new draft.");
      await load();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to duplicate job"));
    } finally {
      setUpdatingId(null);
    }
  }

  async function handleConfirmedAction() {
    if (!confirmAction) return;
    const { kind, jobId } = confirmAction;
    setConfirmAction(null);
    setUpdatingId(jobId);
    try {
      if (kind === "close") {
        await closeJob(jobId);
        updateLocal(jobId, "Closed");
        toast.success("Job closed. It no longer accepts new applications.");
      } else {
        await archiveJob(jobId);
        updateLocal(jobId, "Archived");
        toast.success("Job archived.");
      }
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update job status"));
    } finally {
      setUpdatingId(null);
    }
  }

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <div className="page-header">
        <h1>Manage Jobs</h1>
        <p>Track drafts, published roles, and closed or archived postings in one place.</p>
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      <div className="admin-tabs" role="tablist">
        {TABS.map((t) => (
          <button
            key={t.key}
            type="button"
            role="tab"
            aria-selected={tab === t.key}
            className={`admin-tab ${tab === t.key ? "admin-tab-active" : ""}`}
            onClick={() => setTab(t.key)}
          >
            {t.label} ({counts[t.key]})
          </button>
        ))}
      </div>

      {jobs.length === 0 ? (
        <EmptyState
          icon={<Briefcase size={32} />}
          title="You haven't posted any jobs yet"
          description="Post your first role to start receiving applications."
          action={<Link to="/post-job" className="btn btn-primary">Post a Job</Link>}
        />
      ) : filtered.length === 0 ? (
        <EmptyState icon={<Briefcase size={32} />} title={`No ${tab.toLowerCase()} jobs`} description="Nothing to show in this tab yet." />
      ) : (
        <ul className="job-list">
          {filtered.map(({ job, applicationCount }) => (
            <li key={job.id} className="job-card">
              <Link to={`/jobs/${job.id}`} className="job-card-title">
                {job.title}
              </Link>
              <p className="job-card-meta">
                <StatusBadge status={job.status} />
                <span>{job.displayLocation}</span>
                <span>Created {new Date(job.createdAt).toLocaleDateString()}</span>
                {job.publishedAt && <span>Published {new Date(job.publishedAt).toLocaleDateString()}</span>}
              </p>
              <p className="job-card-meta">
                <span>{applicationCount} {applicationCount === 1 ? "application" : "applications"}</span>
                <span>{job.viewCount} {job.viewCount === 1 ? "view" : "views"}</span>
                <span>Conversion: {conversionRate(job.viewCount, applicationCount)}</span>
              </p>
              <div className="job-card-actions">
                <Link to={`/jobs/${job.id}/applicants`}>View applicants →</Link>
                <Link to={`/jobs/${job.id}/board`}>Applicant board →</Link>
                {job.status !== "Archived" && (
                  <Link to={`/jobs/${job.id}/edit`}><Pencil size={13} style={{ verticalAlign: "-2px", marginRight: "0.2rem" }} />Edit</Link>
                )}

                {job.status === "Draft" && (
                  <button type="button" className="link-button" disabled={updatingId === job.id} onClick={() => handlePublish(job.id)}>
                    Publish
                  </button>
                )}
                {job.status === "Open" && (
                  <button
                    type="button"
                    className="link-button"
                    disabled={updatingId === job.id}
                    onClick={() => setConfirmAction({ kind: "close", jobId: job.id, jobTitle: job.title })}
                  >
                    Close job
                  </button>
                )}
                {job.status === "Closed" && (
                  <button type="button" className="link-button" disabled={updatingId === job.id} onClick={() => handleReopen(job.id)}>
                    Reopen
                  </button>
                )}
                {(job.status === "Draft" || job.status === "Open" || job.status === "Closed") && (
                  <button
                    type="button"
                    className="link-button"
                    disabled={updatingId === job.id}
                    onClick={() => setConfirmAction({ kind: "archive", jobId: job.id, jobTitle: job.title })}
                  >
                    Archive
                  </button>
                )}
                <button type="button" className="link-button" disabled={updatingId === job.id} onClick={() => handleDuplicate(job.id)}>
                  <Copy size={13} style={{ verticalAlign: "-2px", marginRight: "0.2rem" }} />Duplicate
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}

      <Modal
        open={confirmAction !== null}
        onClose={() => setConfirmAction(null)}
        title={confirmAction?.kind === "close" ? "Close this job?" : "Archive this job?"}
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmAction(null)}>Cancel</Button>
            <Button variant="danger" onClick={handleConfirmedAction}>
              {confirmAction?.kind === "close" ? "Close job" : "Archive job"}
            </Button>
          </>
        }
      >
        {confirmAction?.kind === "close"
          ? `"${confirmAction.jobTitle}" will stop accepting new applications. You can reopen it later.`
          : `"${confirmAction?.jobTitle}" will be archived and can no longer be edited or reopened.`}
      </Modal>
    </div>
  );
}
