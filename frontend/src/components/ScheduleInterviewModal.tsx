import { useEffect, useState } from "react";
import type { Interview, InterviewTypeValue } from "../types";
import Button from "./ui/Button";
import Modal from "./ui/Modal";
import FormField from "./ui/FormField";

export interface ScheduleInterviewFormPayload {
  startUtc: string;
  endUtc: string;
  type: InterviewTypeValue;
  location?: string;
  recruiterNote?: string;
}

interface ScheduleInterviewModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (payload: ScheduleInterviewFormPayload) => Promise<void>;
  mode?: "schedule" | "reschedule";
  existing?: Interview | null;
  candidateName?: string;
}

const TYPE_OPTIONS: { value: InterviewTypeValue; label: string }[] = [
  { value: "Online", label: "Online" },
  { value: "Phone", label: "Phone" },
  { value: "InPerson", label: "In Person" },
];

const DURATION_OPTIONS = [15, 30, 45, 60, 90];

function toLocalInputValue(iso: string): string {
  const d = new Date(iso);
  const pad = (n: number) => n.toString().padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export default function ScheduleInterviewModal({ open, onClose, onSubmit, mode = "schedule", existing, candidateName }: ScheduleInterviewModalProps) {
  const [date, setDate] = useState("");
  const [duration, setDuration] = useState(30);
  const [type, setType] = useState<InterviewTypeValue>("Online");
  const [location, setLocation] = useState("");
  const [note, setNote] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    if (existing) {
      setDate(toLocalInputValue(existing.scheduledStartUtc));
      const durMinutes = Math.round((new Date(existing.scheduledEndUtc).getTime() - new Date(existing.scheduledStartUtc).getTime()) / 60000);
      setDuration(DURATION_OPTIONS.includes(durMinutes) ? durMinutes : 30);
      setType(existing.type);
      setLocation(existing.location ?? "");
      setNote(existing.recruiterNote ?? "");
    } else {
      setDate("");
      setDuration(30);
      setType("Online");
      setLocation("");
      setNote("");
    }
    setError(null);
  }, [open, existing]);

  async function handleSubmit() {
    setError(null);
    if (!date) {
      setError("Choose a date and time.");
      return;
    }
    const start = new Date(date);
    if (Number.isNaN(start.getTime())) {
      setError("Enter a valid date and time.");
      return;
    }
    if (start.getTime() <= Date.now()) {
      setError("The interview time must be in the future.");
      return;
    }
    const end = new Date(start.getTime() + duration * 60000);

    setSubmitting(true);
    try {
      await onSubmit({
        startUtc: start.toISOString(),
        endUtc: end.toISOString(),
        type,
        location: location || undefined,
        recruiterNote: note || undefined,
      });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save the interview.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={mode === "reschedule" ? "Reschedule interview" : "Schedule interview"}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
          <Button onClick={handleSubmit} loading={submitting}>
            {mode === "reschedule" ? "Save new time" : "Send invitation"}
          </Button>
        </>
      }
    >
      {candidateName && <p className="hint" style={{ marginBottom: "1rem" }}>Scheduling with {candidateName}</p>}

      <div className="form-row">
        <FormField label="Date & time" htmlFor="interview-datetime">
          <input id="interview-datetime" type="datetime-local" value={date} onChange={(e) => setDate(e.target.value)} />
        </FormField>
        <FormField label="Duration" htmlFor="interview-duration">
          <select id="interview-duration" value={duration} onChange={(e) => setDuration(Number(e.target.value))}>
            {DURATION_OPTIONS.map((d) => (
              <option key={d} value={d}>{d} minutes</option>
            ))}
          </select>
        </FormField>
      </div>

      <FormField label="Interview type" htmlFor="interview-type">
        <select id="interview-type" value={type} onChange={(e) => setType(e.target.value as InterviewTypeValue)}>
          {TYPE_OPTIONS.map((opt) => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>
      </FormField>

      <FormField
        label={type === "InPerson" ? "Venue address" : "Meeting link"}
        htmlFor="interview-location"
        hint={type === "Online" ? "e.g. a video call link" : type === "Phone" ? "e.g. phone number to call" : "e.g. office address"}
      >
        <input
          id="interview-location"
          value={location}
          onChange={(e) => setLocation(e.target.value)}
          placeholder={type === "InPerson" ? "123 Main Street, Bengaluru" : "https://meet.example.com/..."}
        />
      </FormField>

      <FormField label="Note to candidate (optional)" htmlFor="interview-note">
        <textarea id="interview-note" rows={3} value={note} onChange={(e) => setNote(e.target.value)} placeholder="What to expect, who they'll meet, etc." />
      </FormField>

      {error && <p className="error" style={{ marginTop: "0.75rem" }}>{error}</p>}
    </Modal>
  );
}
