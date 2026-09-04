import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { CalendarClock, Download, FileText, Sparkles } from "lucide-react";
import {
  getApplicationDetail,
  downloadApplicationResume,
  updateApplicationStatus,
  withdrawApplication,
  type ApplicationStatusValue,
} from "../api/applications";
import {
  acceptInterview,
  cancelInterview,
  completeInterview,
  declineInterview,
  downloadInterviewIcs,
  getInterviewsForApplication,
  rescheduleInterview,
  scheduleInterview,
} from "../api/interviews";
import {
  createOfferDraft, getOffersForApplication, updateOfferDraft, sendOffer, withdrawOffer, getOfferDetail, respondToOffer,
} from "../api/offers";
import type { Interview, JobApplicationDetail, Offer, UpsertOfferRequest } from "../types";
import { saveBlobAsFile } from "../utils/download";
import { generateOfferPdf } from "../utils/offerPdf";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { getErrorCode, getErrorMessage } from "../utils/errors";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import ConfirmDialog from "../components/ui/ConfirmDialog";
import FormField from "../components/ui/FormField";
import { StatusBadge } from "../components/ui/Badge";
import InterviewCard from "../components/InterviewCard";
import ScheduleInterviewModal, { type ScheduleInterviewFormPayload } from "../components/ScheduleInterviewModal";
import MessageThread from "../components/MessageThread";
import FeedbackSummary from "../components/FeedbackSummary";
import ApplicationTimeline from "../components/ApplicationTimeline";
import OfferFormModal from "../components/OfferFormModal";

const OFFER_INACTIVE_STATUSES = new Set(["Declined", "Withdrawn", "Expired"]);

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

  const [scheduleOpen, setScheduleOpen] = useState(false);
  const [rescheduleTarget, setRescheduleTarget] = useState<Interview | null>(null);

  const [offers, setOffers] = useState<Offer[]>([]);
  const [editingOffer, setEditingOffer] = useState<Offer | "new" | null>(null);
  const [offerActionLoading, setOfferActionLoading] = useState(false);
  const [respondAction, setRespondAction] = useState<"accept" | "decline" | null>(null);
  const [respondNote, setRespondNote] = useState("");

  useEffect(() => {
    if (!id) return;
    Promise.all([getApplicationDetail(Number(id)), getInterviewsForApplication(Number(id)), getOffersForApplication(Number(id))])
      .then(([app, ivs, offerList]) => {
        setApplication(app);
        setInterviews(ivs);
        setOffers(offerList);
      })
      .catch(() => setError("This application could not be found or you don't have access to it."))
      .finally(() => setLoading(false));
  }, [id]);

  async function refreshOffers() {
    if (!application) return;
    setOffers(await getOffersForApplication(application.id));
  }

  const latestOffer = offers[0] ?? null;
  const hasActiveOffer = latestOffer !== null && !OFFER_INACTIVE_STATUSES.has(latestOffer.status) && latestOffer.status !== "Accepted";

  async function handleSaveOffer(data: UpsertOfferRequest) {
    if (!application) return;
    if (editingOffer === "new") {
      await createOfferDraft(application.id, data);
      toast.success("Offer draft created.");
    } else if (editingOffer) {
      await updateOfferDraft(editingOffer.id, data);
      toast.success("Offer draft updated.");
    }
    setEditingOffer(null);
    await refreshOffers();
  }

  async function handleSendOffer(offerId: number) {
    setOfferActionLoading(true);
    try {
      await sendOffer(offerId);
      toast.success("Offer sent to the candidate.");
      await refreshOffers();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to send offer"));
    } finally {
      setOfferActionLoading(false);
    }
  }

  async function handleWithdrawOffer(offerId: number) {
    setOfferActionLoading(true);
    try {
      await withdrawOffer(offerId);
      toast.success("Offer withdrawn.");
      await refreshOffers();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to withdraw offer"));
    } finally {
      setOfferActionLoading(false);
    }
  }

  async function handleRespondToOffer() {
    if (!latestOffer || !respondAction) return;
    setOfferActionLoading(true);
    try {
      await respondToOffer(latestOffer.id, { accept: respondAction === "accept", note: respondNote || undefined });
      toast.success(respondAction === "accept" ? "Offer accepted!" : "Offer declined.");
      await refreshOffers();
      if (application) setApplication(await getApplicationDetail(application.id));
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to respond to offer"));
    } finally {
      setOfferActionLoading(false);
      setRespondAction(null);
      setRespondNote("");
    }
  }

  async function handleDownloadOfferPdf(offerId: number) {
    try {
      const detail = await getOfferDetail(offerId);
      const blob = generateOfferPdf(detail);
      saveBlobAsFile(blob, `offer-${detail.offer.jobTitle.replace(/\s+/g, "_")}.pdf`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to generate offer PDF"));
    }
  }

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
      if (getErrorCode(err) === "APPLICATION_FINAL") {
        setActionError("This application has already reached a final status and can no longer be withdrawn.");
      } else {
        setActionError(getErrorMessage(err, "Failed to withdraw application"));
      }
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

  async function handleScheduleSubmit(payload: ScheduleInterviewFormPayload) {
    if (!application) return;
    try {
      await scheduleInterview(application.id, payload);
      toast.success("Interview invitation sent.");
      await refreshInterviews();
    } catch (err) {
      throw new Error(getErrorMessage(err, "Failed to schedule interview"));
    }
  }

  async function handleRescheduleSubmit(payload: ScheduleInterviewFormPayload) {
    if (!rescheduleTarget) return;
    try {
      await rescheduleInterview(rescheduleTarget.id, payload);
      toast.success("Interview rescheduled — the candidate needs to reconfirm.");
      await refreshInterviews();
    } catch (err) {
      throw new Error(getErrorMessage(err, "Failed to reschedule interview"));
    }
  }

  async function handleAccept(interviewId: number) {
    try {
      await acceptInterview(interviewId);
      toast.success("Interview accepted.");
      await refreshInterviews();
      if (application) setApplication(await getApplicationDetail(application.id));
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to accept interview"));
    }
  }

  async function handleDecline(interviewId: number, note?: string) {
    try {
      await declineInterview(interviewId, { responseNote: note });
      toast.success("Interview declined.");
      await refreshInterviews();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to decline interview"));
    }
  }

  async function handleCancel(interviewId: number) {
    try {
      await cancelInterview(interviewId);
      toast.success("Interview cancelled.");
      await refreshInterviews();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to cancel interview"));
    }
  }

  async function handleComplete(interviewId: number) {
    try {
      await completeInterview(interviewId);
      toast.success("Interview marked completed.");
      await refreshInterviews();
      if (application) setApplication(await getApplicationDetail(application.id));
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to mark interview completed"));
    }
  }

  async function handleDownloadIcs(interviewId: number) {
    try {
      const blob = await downloadInterviewIcs(interviewId);
      saveBlobAsFile(blob, `interview-${interviewId}.ics`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to download calendar file"));
    }
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

        {application.nextAction && (
          <p className="hint" style={{ marginBottom: "1rem" }}>
            <strong>Next:</strong> {application.nextAction}
          </p>
        )}

        {application.coverNote && (
          <>
            <h3 style={{ marginBottom: "0.5rem" }}>Cover note</h3>
            <p className="job-detail-body" style={{ marginBottom: "1.5rem" }}>{application.coverNote}</p>
          </>
        )}

        {application.screeningAnswers.length > 0 && (
          <>
            <h3 style={{ marginBottom: "0.5rem" }}>Screening answers</h3>
            <ul className="job-list-compact" style={{ marginBottom: "1.5rem" }}>
              {application.screeningAnswers.map((a) => (
                <li key={a.questionId} className="job-card job-card-compact">
                  <h4>{a.questionText}</h4>
                  <p>
                    {a.selectedOptionTexts.length > 0
                      ? a.selectedOptionTexts.join(", ")
                      : a.numberValue !== null
                      ? a.numberValue
                      : a.textValue || <span className="hint">No answer provided.</span>}
                  </p>
                  {a.preferredAnswer != null && (
                    <p className="hint">Recruiter's preferred answer (private): {a.preferredAnswer}</p>
                  )}
                </li>
              ))}
            </ul>
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
            <Button variant="secondary" icon={<CalendarClock size={16} />} onClick={() => setScheduleOpen(true)}>
              Schedule interview
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
          <div style={{ marginTop: "1.25rem", display: "flex", flexDirection: "column", gap: "0.75rem" }}>
            <h3>Interviews</h3>
            {interviews.map((iv) => (
              <InterviewCard
                key={iv.id}
                interview={iv}
                role={user?.role === "Recruiter" ? "Recruiter" : "Candidate"}
                onAccept={() => handleAccept(iv.id)}
                onDecline={(note) => handleDecline(iv.id, note)}
                onReschedule={() => setRescheduleTarget(iv)}
                onCancel={() => handleCancel(iv.id)}
                onComplete={() => handleComplete(iv.id)}
                onDownloadIcs={() => handleDownloadIcs(iv.id)}
              />
            ))}
          </div>
        )}

        {user?.role === "Recruiter" && interviews.filter((iv) => iv.status === "Completed").map((iv) => (
          <FeedbackSummary key={iv.id} interviewId={iv.id} />
        ))}

        <Card className="ui-card-padded" style={{ marginTop: "1.25rem" }}>
          <div className="section-header">
            <h3><FileText size={18} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />Offer</h3>
            {user?.role === "Recruiter" && !hasActiveOffer && application.status !== "Withdrawn" && (
              <Button size="sm" onClick={() => setEditingOffer("new")}>Create offer</Button>
            )}
          </div>

          {!latestOffer ? (
            <p className="hint">No offer has been created for this application yet.</p>
          ) : (
            <div>
              <p><StatusBadge status={latestOffer.status} /></p>
              <p style={{ marginTop: "0.5rem" }}>
                {latestOffer.salaryType === "Monthly" ? "₹" + latestOffer.offeredSalary.toLocaleString("en-IN") + "/month" : "₹" + latestOffer.offeredSalary.toLocaleString("en-IN") + "/year"}
                {" · "}Joining {new Date(latestOffer.joiningDate).toLocaleDateString()}
              </p>
              <p className="hint">{latestOffer.isRemote ? "Remote — India" : [latestOffer.workCity, latestOffer.workState].filter(Boolean).join(", ")} · {latestOffer.employmentType}</p>
              {latestOffer.recruiterMessage && <p style={{ marginTop: "0.5rem" }}>{latestOffer.recruiterMessage}</p>}
              <p className="hint" style={{ marginTop: "0.5rem" }}>Expires {new Date(latestOffer.expiryDateUtc).toLocaleDateString()}</p>

              <div style={{ display: "flex", gap: "0.75rem", marginTop: "0.75rem", flexWrap: "wrap" }}>
                <Button size="sm" variant="secondary" icon={<Download size={14} />} onClick={() => handleDownloadOfferPdf(latestOffer.id)}>
                  Download PDF
                </Button>

                {user?.role === "Recruiter" && latestOffer.status === "Draft" && (
                  <>
                    <Button size="sm" variant="secondary" onClick={() => setEditingOffer(latestOffer)}>Edit draft</Button>
                    <Button size="sm" loading={offerActionLoading} onClick={() => handleSendOffer(latestOffer.id)}>Send offer</Button>
                  </>
                )}
                {user?.role === "Recruiter" && (latestOffer.status === "Sent" || latestOffer.status === "Viewed") && (
                  <Button size="sm" variant="danger" loading={offerActionLoading} onClick={() => handleWithdrawOffer(latestOffer.id)}>Withdraw offer</Button>
                )}

                {user?.role === "Candidate" && (latestOffer.status === "Sent" || latestOffer.status === "Viewed") && (
                  <>
                    <Button size="sm" onClick={() => setRespondAction("accept")}>Accept offer</Button>
                    <Button size="sm" variant="danger" onClick={() => setRespondAction("decline")}>Decline offer</Button>
                  </>
                )}
              </div>
            </div>
          )}

          {offers.length > 1 && (
            <div style={{ marginTop: "1rem" }}>
              <p className="hint" style={{ marginBottom: "0.4rem" }}>Earlier offers on this application:</p>
              <ul className="job-list-compact">
                {offers.slice(1).map((o) => (
                  <li key={o.id} className="job-card job-card-compact">
                    <StatusBadge status={o.status} />
                    <p className="hint">
                      {o.salaryType === "Monthly" ? "₹" + o.offeredSalary.toLocaleString("en-IN") + "/month" : "₹" + o.offeredSalary.toLocaleString("en-IN") + "/year"}
                      {" · "}Created {new Date(o.createdAt).toLocaleDateString()}
                    </p>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </Card>

        <Card className="ui-card-padded" style={{ marginTop: "1.25rem" }}>
          <h3 style={{ marginBottom: "0.75rem" }}>Messages</h3>
          <MessageThread applicationId={application.id} />
        </Card>

        {application.statusHistory.length > 0 && (
          <Card className="ui-card-padded" style={{ marginTop: "1.25rem" }}>
            <h3 style={{ marginBottom: "0.75rem" }}>Application timeline</h3>
            <ApplicationTimeline entries={application.statusHistory} />
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

      <ConfirmDialog
        open={confirmWithdraw}
        title="Withdraw application"
        confirmLabel="Withdraw"
        danger
        loading={actionLoading}
        onConfirm={handleWithdraw}
        onCancel={() => setConfirmWithdraw(false)}
      >
        Withdraw your application for "{application.jobTitle}"? This can't be undone.
      </ConfirmDialog>

      <ScheduleInterviewModal
        open={scheduleOpen}
        onClose={() => setScheduleOpen(false)}
        onSubmit={handleScheduleSubmit}
        candidateName={application.candidateFullName}
      />

      <ScheduleInterviewModal
        open={rescheduleTarget !== null}
        onClose={() => setRescheduleTarget(null)}
        onSubmit={handleRescheduleSubmit}
        mode="reschedule"
        existing={rescheduleTarget}
        candidateName={application.candidateFullName}
      />

      {editingOffer && (
        <OfferFormModal
          initial={editingOffer === "new" ? null : editingOffer}
          onClose={() => setEditingOffer(null)}
          onSave={handleSaveOffer}
        />
      )}

      <ConfirmDialog
        open={respondAction !== null}
        title={respondAction === "accept" ? "Accept this offer?" : "Decline this offer?"}
        confirmLabel={respondAction === "accept" ? "Accept offer" : "Decline offer"}
        danger={respondAction === "decline"}
        loading={offerActionLoading}
        onConfirm={handleRespondToOffer}
        onCancel={() => { setRespondAction(null); setRespondNote(""); }}
      >
        <FormField label="Note (optional)" htmlFor="respond-note">
          <textarea id="respond-note" rows={3} value={respondNote} onChange={(e) => setRespondNote(e.target.value)} />
        </FormField>
      </ConfirmDialog>
    </div>
  );
}
