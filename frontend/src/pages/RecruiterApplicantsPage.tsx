import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { CalendarClock, Users } from "lucide-react";
import { getApplicationsForJob, updateApplicationStatus, type ApplicationStatusValue } from "../api/applications";
import { getJobById } from "../api/jobs";
import { scheduleInterview } from "../api/interviews";
import type { ApplicantScreeningFilter, JobApplication, JobPosting } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import Button from "../components/ui/Button";
import Avatar from "../components/ui/Avatar";
import FormField from "../components/ui/FormField";
import { resolveAvatarUrl } from "../utils/format";
import ScheduleInterviewModal, { type ScheduleInterviewFormPayload } from "../components/ScheduleInterviewModal";
import SaveToPoolModal from "../components/SaveToPoolModal";

type FilterMode = "" | "yesNo" | "option" | "number" | "requiredAnswered" | "requiredUnanswered";

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
  const [job, setJob] = useState<JobPosting | null>(null);
  const [questionId, setQuestionId] = useState<number | "">("");
  const [filterMode, setFilterMode] = useState<FilterMode>("");
  const [yesNoValue, setYesNoValue] = useState<"Yes" | "No">("Yes");
  const [optionId, setOptionId] = useState<number | "">("");
  const [minNumber, setMinNumber] = useState("");
  const [maxNumber, setMaxNumber] = useState("");

  function loadApplications(filter?: ApplicantScreeningFilter) {
    if (!id) return;
    setLoading(true);
    getApplicationsForJob(Number(id), filter)
      .then(setApplications)
      .catch(() => setError("You don't have access to this job's applicants."))
      .finally(() => setLoading(false));
  }

  useEffect(() => {
    if (!id) return;
    loadApplications();
    getJobById(Number(id)).then(setJob).catch(() => setJob(null));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const selectedQuestion = job?.screeningQuestions.find((q) => q.id === questionId);

  function applyFilter() {
    if (filterMode === "requiredAnswered" || filterMode === "requiredUnanswered") {
      loadApplications({ requiredAnsweredOnly: filterMode === "requiredAnswered" });
      return;
    }
    if (!questionId) return;
    if (filterMode === "yesNo") loadApplications({ questionId, yesNo: yesNoValue });
    else if (filterMode === "option" && optionId) loadApplications({ questionId, optionId });
    else if (filterMode === "number") loadApplications({ questionId, minNumber: minNumber ? Number(minNumber) : undefined, maxNumber: maxNumber ? Number(maxNumber) : undefined });
  }

  function clearFilter() {
    setQuestionId("");
    setFilterMode("");
    setOptionId("");
    setMinNumber("");
    setMaxNumber("");
    loadApplications();
  }

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

      {job && job.screeningQuestions.length > 0 && (
        <div className="job-card" style={{ marginBottom: "1.5rem" }}>
          <h3 style={{ marginBottom: "0.5rem" }}>Filter by screening answer</h3>
          <div className="form-row">
            <FormField label="Question" htmlFor="filter-question">
              <select
                id="filter-question"
                value={questionId}
                onChange={(e) => {
                  setQuestionId(e.target.value ? Number(e.target.value) : "");
                  setFilterMode("");
                }}
              >
                <option value="">Any question…</option>
                {job.screeningQuestions.map((q) => <option key={q.id} value={q.id}>{q.questionText}</option>)}
              </select>
            </FormField>
            {selectedQuestion && (
              <FormField label="Filter type" htmlFor="filter-mode">
                <select id="filter-mode" value={filterMode} onChange={(e) => setFilterMode(e.target.value as FilterMode)}>
                  <option value="">Select...</option>
                  {selectedQuestion.questionType === "YesNo" && <option value="yesNo">Answered Yes/No</option>}
                  {(selectedQuestion.questionType === "SingleChoice" || selectedQuestion.questionType === "MultipleChoice") && (
                    <option value="option">Selected option</option>
                  )}
                  {selectedQuestion.questionType === "Number" && <option value="number">Numeric range</option>}
                </select>
              </FormField>
            )}
          </div>

          {filterMode === "yesNo" && (
            <FormField label="Value" htmlFor="filter-yesno">
              <select id="filter-yesno" value={yesNoValue} onChange={(e) => setYesNoValue(e.target.value as "Yes" | "No")}>
                <option value="Yes">Yes</option>
                <option value="No">No</option>
              </select>
            </FormField>
          )}
          {filterMode === "option" && selectedQuestion && (
            <FormField label="Option" htmlFor="filter-option">
              <select id="filter-option" value={optionId} onChange={(e) => setOptionId(e.target.value ? Number(e.target.value) : "")}>
                <option value="">Select an option…</option>
                {selectedQuestion.options.map((o) => <option key={o.id} value={o.id}>{o.optionText}</option>)}
              </select>
            </FormField>
          )}
          {filterMode === "number" && (
            <div className="form-row">
              <FormField label="Min" htmlFor="filter-min"><input id="filter-min" type="number" value={minNumber} onChange={(e) => setMinNumber(e.target.value)} /></FormField>
              <FormField label="Max" htmlFor="filter-max"><input id="filter-max" type="number" value={maxNumber} onChange={(e) => setMaxNumber(e.target.value)} /></FormField>
            </div>
          )}

          <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.5rem", flexWrap: "wrap" }}>
            <Button size="sm" onClick={applyFilter} disabled={!filterMode}>Apply filter</Button>
            <Button size="sm" variant="secondary" onClick={() => loadApplications({ requiredAnsweredOnly: true })}>All required answered</Button>
            <Button size="sm" variant="secondary" onClick={() => loadApplications({ requiredAnsweredOnly: false })}>Missing required answers</Button>
            <Button size="sm" variant="ghost" onClick={clearFilter}>Clear filters</Button>
          </div>
        </div>
      )}

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
                {app.requiredQuestionsTotalCount > 0 && (
                  <span>Screening: {app.requiredQuestionsAnsweredCount}/{app.requiredQuestionsTotalCount} required answered</span>
                )}
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
