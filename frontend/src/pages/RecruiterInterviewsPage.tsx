import { useEffect, useState } from "react";
import { CalendarClock } from "lucide-react";
import { cancelInterview, completeInterview, downloadInterviewIcs, getMyInterviews, rescheduleInterview } from "../api/interviews";
import type { Interview } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { saveBlobAsFile } from "../utils/download";
import InterviewCard from "../components/InterviewCard";
import ScheduleInterviewModal, { type ScheduleInterviewFormPayload } from "../components/ScheduleInterviewModal";
import EmptyState from "../components/ui/EmptyState";

type Tab = "All" | "Proposed" | "Scheduled" | "Completed" | "Cancelled" | "Declined";
const TABS: Tab[] = ["All", "Proposed", "Scheduled", "Completed", "Cancelled", "Declined"];

export default function RecruiterInterviewsPage() {
  const toast = useToast();
  const [interviews, setInterviews] = useState<Interview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>("All");
  const [rescheduleTarget, setRescheduleTarget] = useState<Interview | null>(null);

  useEffect(() => {
    void load(tab);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tab]);

  async function load(currentTab: Tab) {
    setLoading(true);
    setError(null);
    try {
      setInterviews(await getMyInterviews(currentTab === "All" ? undefined : currentTab));
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load interviews"));
    } finally {
      setLoading(false);
    }
  }

  async function handleCancel(interviewId: number) {
    try {
      await cancelInterview(interviewId);
      toast.success("Interview cancelled.");
      await load(tab);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to cancel interview"));
    }
  }

  async function handleComplete(interviewId: number) {
    try {
      await completeInterview(interviewId);
      toast.success("Interview marked completed.");
      await load(tab);
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

  async function handleRescheduleSubmit(payload: ScheduleInterviewFormPayload) {
    if (!rescheduleTarget) return;
    try {
      await rescheduleInterview(rescheduleTarget.id, payload);
      toast.success("Interview rescheduled — the candidate needs to reconfirm.");
      await load(tab);
    } catch (err) {
      throw new Error(getErrorMessage(err, "Failed to reschedule interview"));
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1><CalendarClock size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Interviews</h1>
        <p>Every interview across your company's jobs. Times shown in India Standard Time (IST).</p>
      </div>

      <div className="admin-tabs" role="tablist">
        {TABS.map((t) => (
          <button
            key={t}
            type="button"
            role="tab"
            aria-selected={tab === t}
            className={`admin-tab ${tab === t ? "admin-tab-active" : ""}`}
            onClick={() => setTab(t)}
          >
            {t}
          </button>
        ))}
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      {loading ? (
        <p>Loading...</p>
      ) : interviews.length === 0 ? (
        <EmptyState icon={<CalendarClock size={32} />} title={`No ${tab === "All" ? "" : tab.toLowerCase() + " "}interviews`} description="Nothing to show in this tab yet." />
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
          {interviews.map((iv) => (
            <InterviewCard
              key={iv.id}
              interview={iv}
              role="Recruiter"
              linkToApplication
              onReschedule={() => setRescheduleTarget(iv)}
              onCancel={() => handleCancel(iv.id)}
              onComplete={() => handleComplete(iv.id)}
              onDownloadIcs={() => handleDownloadIcs(iv.id)}
            />
          ))}
        </div>
      )}

      <ScheduleInterviewModal
        open={rescheduleTarget !== null}
        onClose={() => setRescheduleTarget(null)}
        onSubmit={handleRescheduleSubmit}
        mode="reschedule"
        existing={rescheduleTarget}
        candidateName={rescheduleTarget?.candidateFullName}
      />
    </div>
  );
}
