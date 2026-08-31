import { useEffect, useMemo, useState } from "react";
import { CalendarClock } from "lucide-react";
import { acceptInterview, declineInterview, downloadInterviewIcs, getMyInterviews } from "../api/interviews";
import type { Interview } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { saveBlobAsFile } from "../utils/download";
import InterviewCard from "../components/InterviewCard";
import EmptyState from "../components/ui/EmptyState";

export default function CandidateInterviewsPage() {
  const toast = useToast();
  const [interviews, setInterviews] = useState<Interview[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setInterviews(await getMyInterviews());
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load your interviews"));
    } finally {
      setLoading(false);
    }
  }

  async function handleAccept(interviewId: number) {
    try {
      await acceptInterview(interviewId);
      toast.success("Interview accepted.");
      await load();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to accept interview"));
    }
  }

  async function handleDecline(interviewId: number, note?: string) {
    try {
      await declineInterview(interviewId, { responseNote: note });
      toast.success("Interview declined.");
      await load();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to decline interview"));
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

  const { upcoming, past } = useMemo(() => {
    const now = Date.now();
    const upcoming: Interview[] = [];
    const past: Interview[] = [];
    for (const iv of interviews) {
      const isActive = iv.status === "Proposed" || iv.status === "Scheduled";
      const isFuture = new Date(iv.scheduledStartUtc).getTime() > now;
      if (isActive && isFuture) upcoming.push(iv);
      else past.push(iv);
    }
    upcoming.sort((a, b) => new Date(a.scheduledStartUtc).getTime() - new Date(b.scheduledStartUtc).getTime());
    past.sort((a, b) => new Date(b.scheduledStartUtc).getTime() - new Date(a.scheduledStartUtc).getTime());
    return { upcoming, past };
  }, [interviews]);

  if (loading) return <p>Loading...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div>
      <div className="page-header">
        <h1><CalendarClock size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />My Interviews</h1>
        <p>All times are shown in India Standard Time (IST).</p>
      </div>

      <div style={{ marginBottom: "2rem" }}>
        <h3 style={{ marginBottom: "0.75rem" }}>Upcoming</h3>
        {upcoming.length === 0 ? (
          <EmptyState icon={<CalendarClock size={28} />} title="No upcoming interviews" description="Proposed and confirmed interviews will show up here." />
        ) : (
          <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
            {upcoming.map((iv) => (
              <InterviewCard
                key={iv.id}
                interview={iv}
                role="Candidate"
                linkToApplication
                onAccept={() => handleAccept(iv.id)}
                onDecline={(note) => handleDecline(iv.id, note)}
                onDownloadIcs={() => handleDownloadIcs(iv.id)}
              />
            ))}
          </div>
        )}
      </div>

      {past.length > 0 && (
        <div>
          <h3 style={{ marginBottom: "0.75rem" }}>Past</h3>
          <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
            {past.map((iv) => (
              <InterviewCard key={iv.id} interview={iv} role="Candidate" linkToApplication />
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
