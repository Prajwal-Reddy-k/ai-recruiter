import { useState } from "react";
import { Link } from "react-router-dom";
import { CalendarClock, Download, MapPin, Phone, Video } from "lucide-react";
import type { Interview } from "../types";
import { toIST } from "../utils/format";
import { StatusBadge } from "./ui/Badge";
import Button from "./ui/Button";
import Modal from "./ui/Modal";

const TYPE_ICONS = { Online: Video, Phone: Phone, InPerson: MapPin } as const;

interface InterviewCardProps {
  interview: Interview;
  role: "Candidate" | "Recruiter";
  linkToApplication?: boolean;
  onAccept?: () => Promise<void> | void;
  onDecline?: (note?: string) => Promise<void> | void;
  onReschedule?: () => void;
  onCancel?: () => Promise<void> | void;
  onComplete?: () => Promise<void> | void;
  onDownloadIcs?: () => void;
}

export default function InterviewCard({
  interview,
  role,
  linkToApplication,
  onAccept,
  onDecline,
  onReschedule,
  onCancel,
  onComplete,
  onDownloadIcs,
}: InterviewCardProps) {
  const [busy, setBusy] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const [confirmDecline, setConfirmDecline] = useState(false);
  const [declineNote, setDeclineNote] = useState("");

  const TypeIcon = TYPE_ICONS[interview.type] ?? Video;

  async function run(action: (() => Promise<void> | void) | undefined) {
    if (!action) return;
    setBusy(true);
    try {
      await action();
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="ui-card ui-card-padded interview-card">
      <div className="job-card-top">
        <div>
          <h4>
            {linkToApplication ? (
              <Link to={`/applications/${interview.jobApplicationId}`}>{interview.jobTitle}</Link>
            ) : (
              interview.jobTitle
            )}
          </h4>
          <p className="job-card-company">
            {interview.companyName}
            {role === "Recruiter" && ` · ${interview.candidateFullName}`}
          </p>
        </div>
        <StatusBadge status={interview.status} />
      </div>

      <div className="job-card-meta" style={{ marginTop: "0.5rem" }}>
        <span><CalendarClock size={14} /> {toIST(interview.scheduledStartUtc)} – {toIST(interview.scheduledEndUtc)}</span>
        <span><TypeIcon size={14} /> {interview.type === "InPerson" ? "In Person" : interview.type}</span>
      </div>

      {interview.location && (
        <p style={{ marginTop: "0.5rem" }}>
          {interview.type === "Online" ? (
            <a href={interview.location} target="_blank" rel="noreferrer">{interview.location}</a>
          ) : (
            interview.location
          )}
        </p>
      )}

      {interview.recruiterNote && (
        <p className="hint" style={{ marginTop: "0.5rem" }}><strong>Note:</strong> {interview.recruiterNote}</p>
      )}
      {interview.candidateResponseNote && (
        <p className="hint" style={{ marginTop: "0.5rem" }}><strong>Candidate note:</strong> {interview.candidateResponseNote}</p>
      )}

      <div style={{ display: "flex", gap: "0.75rem", marginTop: "0.75rem", flexWrap: "wrap" }}>
        {role === "Candidate" && interview.status === "Proposed" && (
          <>
            <Button size="sm" loading={busy} onClick={() => run(onAccept)}>Accept</Button>
            <Button size="sm" variant="danger" onClick={() => setConfirmDecline(true)}>Decline</Button>
          </>
        )}

        {role === "Recruiter" && interview.canManage && (interview.status === "Proposed" || interview.status === "Scheduled") && (
          <>
            {onReschedule && <Button size="sm" variant="secondary" onClick={onReschedule}>Reschedule</Button>}
            <Button size="sm" variant="danger" onClick={() => setConfirmCancel(true)}>Cancel</Button>
            {interview.status === "Scheduled" && onComplete && (
              <Button size="sm" variant="secondary" loading={busy} onClick={() => run(onComplete)}>Mark completed</Button>
            )}
          </>
        )}

        {interview.status === "Scheduled" && onDownloadIcs && (
          <Button size="sm" variant="ghost" icon={<Download size={14} />} onClick={onDownloadIcs}>Add to calendar (.ics)</Button>
        )}
      </div>

      <Modal
        open={confirmCancel}
        onClose={() => setConfirmCancel(false)}
        title="Cancel this interview?"
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmCancel(false)}>Keep it</Button>
            <Button variant="danger" loading={busy} onClick={async () => { setConfirmCancel(false); await run(onCancel); }}>
              Cancel interview
            </Button>
          </>
        }
      >
        This will cancel the interview for {interview.candidateFullName || "this candidate"} and notify them. This can't be undone.
      </Modal>

      <Modal
        open={confirmDecline}
        onClose={() => setConfirmDecline(false)}
        title="Decline this interview?"
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmDecline(false)}>Go back</Button>
            <Button
              variant="danger"
              loading={busy}
              onClick={async () => {
                setConfirmDecline(false);
                await run(() => onDecline?.(declineNote || undefined));
                setDeclineNote("");
              }}
            >
              Decline
            </Button>
          </>
        }
      >
        <p style={{ marginBottom: "0.75rem" }}>Let the recruiter know why, if you'd like (optional).</p>
        <textarea
          rows={3}
          value={declineNote}
          onChange={(e) => setDeclineNote(e.target.value)}
          placeholder="e.g. Not available at this time"
        />
      </Modal>
    </div>
  );
}
