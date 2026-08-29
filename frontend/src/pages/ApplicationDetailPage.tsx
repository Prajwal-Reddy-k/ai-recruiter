import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { CalendarClock, Download, Sparkles } from "lucide-react";
import {
  getApplicationDetail,
  downloadApplicationResume,
  updateApplicationStatus,
  withdrawApplication,
  type ApplicationStatusValue,
} from "../api/applications";
import {
  downloadInterviewIcs,
  getInterviewsForApplication,
  proposeInterview,
  respondToInterview,
} from "../api/interviews";
import type { Interview, JobApplicationDetail } from "../types";
import { saveBlobAsFile } from "../utils/download";
import { toIST } from "../utils/format";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { getErrorMessage } from "../utils/errors";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import Modal from "../components/ui/Modal";
import FormField from "../components/ui/FormField";
import { StatusBadge } from "../components/ui/Badge";

const TERMINAL_STATUSES = new Set(["Withdrawn", "Rejected", "Hired"]);

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

export default function ApplicationDetailPage() {
  const { id } = useParams();
  const { user } = useAuth();
  const toast = useToast();
  const [application, setApplication] = useState<JobApplicationDetail | null>(null);
  const [interviews, setInterviews] = useState<Interview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionLoading, setActionLoading] = useState(false);
  const [confirmWithdraw, setConfirmWithdraw] = useState(false);
  const [statusNote, setStatusNote] = useState("");

  const [proposeOpen, setProposeOpen] = useState(false);
  const [slotInputs, setSlotInputs] = useState<{ start: string; end: string }[]>([{ start: "", end: "" }]);
  const [proposing, setProposing] = useState(false);
  const [proposeError, setProposeError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    Promise.all([getApplicationDetail(Number(id)), getInterviewsForApplication(Number(id))])
      .then(([app, ivs]) => {
        setApplication(app);
        setInterviews(ivs);
      })
      .catch(() => setError("This application could not be found or you don't have access to it."))
      .finally(() => setLoading(false));
  }, [id]);

  async function refreshInterviews() {
    if (!application) return;
    const ivs = await getInterviewsForApplication(application.id);
    setInterviews(ivs);
  }

  async function handleDownloadResume() {
    if (!application) return;
    const blob = await downloadApplicationResume(application.id);
    saveBlobAsFile(blob, `${application.candidateFullName}-resume`);
  }

  async function handleWithdraw() {
    if (!application) return;
    setConfirmWithdraw(false);

    setActionError(null);
    setActionLoading(true);
    try {
      const updated = await withdrawApplication(application.id);
      setApplication({ ...application, status: updated.status, updatedAt: updated.updatedAt });
      toast.success("Application withdrawn.");
    } catch (err) {
      setActionError(getErrorMessage(err, "Failed to withdraw application"));
    } finally {
      setActionLoading(false);
    }
  }

  async function handleStatusChange(status: ApplicationStatusValue) {
    if (!application) return;
    setActionError(null);
    setActionLoading(true);
    try {
      const updated = await updateApplicationStatus(application.id, status, statusNote || undefined);
      const refreshed = await getApplicationDetail(application.id);
      setApplication(refreshed);
      setStatusNote("");
      toast.success(`Status updated to ${updated.status}.`);
    } catch (err) {
      setActionError(getErrorMessage(err, "Failed to update application status"));
    } finally {
      setActionLoading(false);
    }
  }

  function addSlotInput() {
    setSlotInputs((prev) => [...prev, { start: "", end: "" }]);
  }

  async function handleProposeSubmit() {
    if (!application) return;
    const valid = slotInputs.filter((s) => s.start && s.end);
    if (valid.length === 0) {
      setProposeError("Add at least one slot with a start and end time.");
      return;
    }
    setProposing(true);
    setProposeError(null);
    try {
      await proposeInterview(application.id, {
        slots: valid.map((s) => ({
          startUtc: new Date(s.start).toISOString(),
          endUtc: new Date(s.end).toISOString(),
        })),
      });
      toast.success("Interview slots proposed.");
      setProposeOpen(false);
      setSlotInputs([{ start: "", end: "" }]);
      await refreshInterviews();
    } catch (err) {
      setProposeError(getErrorMessage(err, "Failed to propose interview slots"));
    } finally {
      setProposing(false);
    }
  }

  async function handleRespond(interviewId: number, acceptedSlotId?: number, declineNote?: string) {
    try {
      await respondToInterview(interviewId, { acceptedSlotId, declineNote });
      toast.success(acceptedSlotId ? "Interview accepted." : "Interview declined.");
      await refreshInterviews();
      if (application) setApplication(await getApplicationDetail(application.id));
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to respond to interview"));
    }
  }

  async function handleDownloadIcs(interviewId: number) {
    const blob = await downloadInterviewIcs(interviewId);
    saveBlobAsFile(blob, `interview-${interviewId}.ics`);
  }

  if (loading) return <p>Loading...</p>;
  if (error || !application) return <p className="error">{error ?? "Application not found."}</p>;

  return (
    <div className="job-detail-layout">
      <div className="job-detail-main">
        <div className="job-detail-header">
          <h1>{application.jobTitle}</h1>
          <p className="job-detail-company">{application.companyName}</p>
        </div>

        <div className="job-detail-facts">
          <span className="job-detail-fact">Candidate: {application.candidateFullName}</span>
          <span className="job-detail-fact"><StatusBadge status={application.status} /></span>
          <span className="job-detail-fact">Applied {new Date(application.createdAt).toLocaleDateString()}</span>
        </div>

        {application.coverNote && (
          <>
            <h3 style={{ marginBottom: "0.5rem" }}>Cover note</h3>
            <p className="job-detail-body" style={{ marginBottom: "1.5rem" }}>{application.coverNote}</p>
          </>
        )}

        {actionError && <p className="error" style={{ marginBottom: "1rem" }}>{actionError}</p>}

        <div style={{ display: "flex", gap: "0.75rem", flexWrap: "wrap", alignItems: "center" }}>
          <Button variant="secondary" icon={<Download size={16} />} onClick={handleDownloadResume}>
            Download resume
          </Button>

          {user?.role === "Candidate" && !TERMINAL_STATUSES.has(application.status) && (
            <Button variant="danger" loading={actionLoading} onClick={() => setConfirmWithdraw(true)}>
              Withdraw application
            </Button>
          )}

          {user?.role === "Recruiter" && application.status !== "Withdrawn" && (
            <Button variant="secondary" icon={<CalendarClock size={16} />} onClick={() => setProposeOpen(true)}>
              Propose interview
            </Button>
          )}
        </div>

        {user?.role === "Recruiter" && application.status !== "Withdrawn" && (
          <Card className="ui-card-padded" style={{ marginTop: "1.25rem" }}>
            <h3 style={{ marginBottom: "0.75rem" }}>Update status</h3>
            <div className="applicant-actions" style={{ marginTop: 0 }}>
              <label htmlFor="app-status" className="hint">Status:</label>
              <select
                id="app-status"
                value={application.status as ApplicationStatusValue}
                disabled={actionLoading}
                onChange={(e) => handleStatusChange(e.target.value as ApplicationStatusValue)}
              >
                {RECRUITER_SELECTABLE_STATUSES.map((status) => (
                  <option key={status} value={status}>
                    {status}
                  </option>
                ))}
              </select>
            </div>
            <FormField label="Note to candidate (optional)" htmlFor="status-note" hint="Shown to the candidate in their application timeline.">
              <input id="status-note" value={statusNote} onChange={(e) => setStatusNote(e.target.value)} placeholder="e.g. Great interview — moving you to the next round" />
            </FormField>
          </Card>
        )}

        {interviews.length > 0 && (
          <Card className="ui-card-padded" style={{ marginTop: "1.25rem" }}>
            <h3 style={{ marginBottom: "0.75rem" }}>Interviews</h3>
            {interviews.map((iv) => (
              <div key={iv.id} style={{ marginBottom: "1rem", paddingBottom: "1rem", borderBottom: "1px solid var(--border)" }}>
                <p><strong>Status:</strong> {iv.status}</p>
                {iv.slots.map((slot) => (
                  <div key={slot.id} style={{ display: "flex", gap: "0.75rem", alignItems: "center", marginTop: "0.5rem" }}>
                    <span>{toIST(slot.startUtc)} – {toIST(slot.endUtc)}</span>
                    {slot.isSelected && <StatusBadge status="Scheduled" />}
                    {user?.role === "Candidate" && iv.status === "Proposed" && !slot.isSelected && (
                      <Button variant="secondary" onClick={() => handleRespond(iv.id, slot.id)}>Accept this slot</Button>
                    )}
                  </div>
                ))}
                {user?.role === "Candidate" && iv.status === "Proposed" && (
                  <Button variant="ghost" onClick={() => handleRespond(iv.id, undefined, "Not available for the proposed times")} style={{ marginTop: "0.5rem" }}>
                    Decline all
                  </Button>
                )}
                {iv.status === "Scheduled" && (
                  <Button variant="secondary" onClick={() => handleDownloadIcs(iv.id)} style={{ marginTop: "0.5rem" }}>
                    Download .ics
                  </Button>
                )}
                {iv.declineNote && <p className="hint" style={{ marginTop: "0.5rem" }}>Note: {iv.declineNote}</p>}
              </div>
            ))}
          </Card>
        )}

        {application.statusHistory.length > 0 && (
          <Card className="ui-card-padded" style={{ marginTop: "1.25rem" }}>
            <h3 style={{ marginBottom: "0.75rem" }}>Status timeline</h3>
            <ul className="status-timeline">
              {application.statusHistory.map((entry, i) => (
                <li key={i} className="status-timeline-entry">
                  <p>
                    {entry.fromStatus ? `${entry.fromStatus} → ${entry.toStatus}` : entry.toStatus}
                    {" "}<span className="hint">by {entry.changedByName}</span>
                  </p>
                  <p className="hint">{new Date(entry.changedAt).toLocaleString()}</p>
                  {entry.note && <p style={{ marginTop: "0.25rem" }}>{entry.note}</p>}
                </li>
              ))}
            </ul>
          </Card>
        )}

        <div className="match-panel" style={{ marginTop: "1.25rem" }}>
          <h3><Sparkles size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />AI-assisted / explainable matching</h3>
          <p className="disclaimer" style={{ marginTop: "0.75rem" }}>
            This score is generated by a fully local, rule-based matching engine — not an external AI service.
            It is decision support only, not an automated hiring decision or rejection.
          </p>

          {application.matchScore === null ? (
            <p className="hint" style={{ marginTop: "1rem" }}>
              No match score is available for this application (no resume was on file at the time of applying).
            </p>
          ) : (
            <>
              <p className="match-score" style={{ marginTop: "1rem" }}>Match score: {application.matchScore}/100</p>

              {application.matchedSkills.length > 0 && (
                <>
                  <h4 style={{ margin: "1rem 0 0.5rem" }}>Matched skills</h4>
                  <div className="chip-list">
                    {application.matchedSkills.map((s) => (
                      <span key={s} className="chip chip-matched">{s}</span>
                    ))}
                  </div>
                </>
              )}

              {application.missingSkills.length > 0 && (
                <>
                  <h4 style={{ margin: "1rem 0 0.5rem" }}>Missing skills</h4>
                  <div className="chip-list">
                    {application.missingSkills.map((s) => (
                      <span key={s} className="chip chip-missing">{s}</span>
                    ))}
                  </div>
                </>
              )}

              {application.suggestedImprovements.length > 0 && (
                <>
                  <h4 style={{ margin: "1rem 0 0.5rem" }}>Suggested improvements</h4>
                  <ul style={{ paddingLeft: "1.25rem", color: "var(--text-muted)", fontSize: "var(--font-sm)" }}>
                    {application.suggestedImprovements.map((s) => (
                      <li key={s}>{s}</li>
                    ))}
                  </ul>
                </>
              )}

              {application.scoringExplanation && (
                <>
                  <h4 style={{ margin: "1rem 0 0.5rem" }}>How this score was calculated</h4>
                  <p className="hint">{application.scoringExplanation}</p>
                </>
              )}
            </>
          )}
        </div>
      </div>

      <div className="job-detail-sidebar">
        <Card>
          <h3 style={{ marginBottom: "0.75rem" }}>Application summary</h3>
          <p className="hint">Job</p>
          <p style={{ marginBottom: "0.75rem" }}>{application.jobTitle}</p>
          <p className="hint">Company</p>
          <p style={{ marginBottom: "0.75rem" }}>{application.companyName}</p>
          <p className="hint">Current status</p>
          <StatusBadge status={application.status} />
        </Card>
      </div>

      <Modal
        open={confirmWithdraw}
        onClose={() => setConfirmWithdraw(false)}
        title="Withdraw application"
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmWithdraw(false)}>Cancel</Button>
            <Button variant="danger" onClick={handleWithdraw}>Withdraw</Button>
          </>
        }
      >
        Withdraw your application for "{application.jobTitle}"? This can't be undone.
      </Modal>

      <Modal
        open={proposeOpen}
        onClose={() => setProposeOpen(false)}
        title="Propose interview slots"
        footer={
          <>
            <Button variant="secondary" onClick={() => setProposeOpen(false)}>Cancel</Button>
            <Button onClick={handleProposeSubmit} loading={proposing}>Send to candidate</Button>
          </>
        }
      >
        {slotInputs.map((slot, i) => (
          <div className="form-row" key={i} style={{ marginBottom: "0.75rem" }}>
            <FormField label="Start" htmlFor={`slot-start-${i}`}>
              <input
                id={`slot-start-${i}`}
                type="datetime-local"
                value={slot.start}
                onChange={(e) =>
                  setSlotInputs((prev) => prev.map((s, idx) => (idx === i ? { ...s, start: e.target.value } : s)))
                }
              />
            </FormField>
            <FormField label="End" htmlFor={`slot-end-${i}`}>
              <input
                id={`slot-end-${i}`}
                type="datetime-local"
                value={slot.end}
                onChange={(e) =>
                  setSlotInputs((prev) => prev.map((s, idx) => (idx === i ? { ...s, end: e.target.value } : s)))
                }
              />
            </FormField>
          </div>
        ))}
        <button type="button" className="link-button" onClick={addSlotInput}>+ Add another slot</button>
        {proposeError && <p className="error" style={{ marginTop: "0.75rem" }}>{proposeError}</p>}
      </Modal>
    </div>
  );
}
