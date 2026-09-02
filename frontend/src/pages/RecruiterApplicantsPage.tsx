import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { CalendarClock, Users } from "lucide-react";
import { getApplicationsForJob, updateApplicationStatus, type ApplicationStatusValue } from "../api/applications";
import { scheduleInterview } from "../api/interviews";
import type { JobApplication } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import Button from "../components/ui/Button";
import Avatar from "../components/ui/Avatar";
import { resolveAvatarUrl } from "../utils/format";
import ScheduleInterviewModal, { type ScheduleInterviewFormPayload } from "../components/ScheduleInterviewModal";
import SaveToPoolModal from "../components/SaveToPoolModal";

const RECRUITER_SELECTABLE_STATUSES: ApplicationStatusValue[] = [
  "Applied",
  "Screening",
  "Shortlisted",
  "InterviewScheduled",
  "InterviewCompleted",
  "Offer",
  "Hired",
  "Rejected",
];

export default function RecruiterApplicantsPage() {
  const { id } = useParams();
  const toast = useToast();
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [updatingId, setUpdatingId] = useState<number | null>(null);
  const [updateError, setUpdateError] = useState<string | null>(null);
  const [scheduleTarget, setScheduleTarget] = useState<JobApplication | null>(null);
  const [poolTarget, setPoolTarget] = useState<JobApplication | null>(null);

  useEffect(() => {
    if (!id) return;
    getApplicationsForJob(Number(id))
      .then(setApplications)
      .catch(() => setError("You don't have access to this job's applicants."))
      .finally(() => setLoading(false));
  }, [id]);

  async function handleStatusChange(applicationId: number, status: ApplicationStatusValue) {
    setUpdateError(null);
    setUpdatingId(applicationId);
    try {
      const updated = await updateApplicationStatus(applicationId, status);
      setApplications((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      toast.success(`Status updated to ${status}.`);
    } catch (err) {
      setUpdateError(getErrorMessage(err, "Failed to update application status"));
    } finally {
      setUpdatingId(null);
    }
  }

  async function handleScheduleSubmit(payload: ScheduleInterviewFormPayload) {
    if (!scheduleTarget) return;
    try {
      await scheduleInterview(scheduleTarget.id, payload);
      toast.success("Interview invitation sent.");
    } catch (err) {
      throw new Error(getErrorMessage(err, "Failed to schedule interview"));
    }
  }

  if (loading) return <p>Loading...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div>
      <div className="page-header">
        <h1>Applicants</h1>
        <p className="hint">
          Match scores are AI-assisted / explainable decision support only — not an automated hiring decision.
        </p>
        {id && <Link to={`/jobs/${id}/board`} className="btn btn-secondary btn-sm">Open Kanban board</Link>}
      </div>
      {updateError && <p className="error" style={{ marginBottom: "1rem" }}>{updateError}</p>}
      {applications.length === 0 ? (
        <EmptyState icon={<Users size={32} />} title="No applicants yet" description="Check back once candidates start applying." />
      ) : (
        <ul className="job-list">
          {applications.map((app) => (
            <li key={app.id} className="job-card">
              <Link to={`/applications/${app.id}`} className="job-card-title" style={{ display: "flex", alignItems: "center", gap: "0.6rem" }}>
                <Avatar name={app.candidateFullName ?? "?"} size={32} src={resolveAvatarUrl(app.candidateAvatarUrl)} />
                {app.candidateFullName ?? `Application #${app.id}`}
              </Link>
              <p className="job-card-meta">
                <StatusBadge status={app.status} />
                <span>Applied {new Date(app.createdAt).toLocaleDateString()}</span>
                {app.matchScore !== null && <span>Match score: {app.matchScore}/100</span>}
              </p>
              {app.status === "Withdrawn" ? (
                <p className="hint" style={{ marginTop: "0.5rem" }}>This candidate withdrew their application.</p>
              ) : (
                <div className="applicant-actions" style={{ flexWrap: "wrap" }}>
                  <label htmlFor={`status-${app.id}`} className="hint">Status:</label>
                  <select
                    id={`status-${app.id}`}
                    value={app.status as ApplicationStatusValue}
                    disabled={updatingId === app.id}
                    onChange={(e) => handleStatusChange(app.id, e.target.value as ApplicationStatusValue)}
                  >
                    {RECRUITER_SELECTABLE_STATUSES.map((status) => (
                      <option key={status} value={status}>
                        {status}
                      </option>
                    ))}
                  </select>
                  <Button size="sm" variant="secondary" icon={<CalendarClock size={14} />} onClick={() => setScheduleTarget(app)}>
                    Schedule interview
                  </Button>
                  {app.candidateProfileId != null && (
                    <Button size="sm" variant="secondary" onClick={() => setPoolTarget(app)}>
                      Save to pool
                    </Button>
                  )}
                </div>
              )}
            </li>
          ))}
        </ul>
      )}

      <ScheduleInterviewModal
        open={scheduleTarget !== null}
        onClose={() => setScheduleTarget(null)}
        onSubmit={handleScheduleSubmit}
        candidateName={scheduleTarget?.candidateFullName ?? undefined}
      />

      {poolTarget && poolTarget.candidateProfileId != null && (
        <SaveToPoolModal
          candidateProfileId={poolTarget.candidateProfileId}
          candidateName={poolTarget.candidateFullName ?? "this candidate"}
          onClose={() => setPoolTarget(null)}
        />
      )}
    </div>
  );
}
