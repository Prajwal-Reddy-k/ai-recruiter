import { useEffect, useState } from "react";
import { ClipboardCheck } from "lucide-react";
import { getFeedbackSummary } from "../api/interviewFeedback";
import type { InterviewFeedbackSummary } from "../types";
import Card from "./ui/Card";
import { Badge } from "./ui/Badge";

const RECOMMENDATION_TONE: Record<string, "success" | "warning" | "danger" | "neutral"> = {
  StrongYes: "success",
  Yes: "success",
  Neutral: "neutral",
  No: "warning",
  StrongNo: "danger",
};

function Avg({ label, value }: { label: string; value: number | null }) {
  return (
    <div className="feedback-avg">
      <span className="hint">{label}</span>
      <strong>{value === null ? "—" : value.toFixed(1)}</strong>
    </div>
  );
}

/** Aggregated scorecard summary for one completed interview — recruiter-only (never shown
 * to candidates); access is enforced server-side (owning recruiter, company Owner, or an
 * assigned HiringManager/Interviewer). Silently renders nothing if the caller isn't
 * authorized or no feedback exists yet, since this is an optional supplementary panel. */
export default function FeedbackSummary({ interviewId }: { interviewId: number }) {
  const [summary, setSummary] = useState<InterviewFeedbackSummary | null>(null);

  useEffect(() => {
    getFeedbackSummary(interviewId)
      .then(setSummary)
      .catch(() => setSummary(null));
  }, [interviewId]);

  if (!summary || summary.scorecards.length === 0) return null;

  return (
    <Card className="ui-card-padded" style={{ marginTop: "1.25rem" }}>
      <h3 style={{ marginBottom: "0.75rem" }}>
        <ClipboardCheck size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />
        Interview Feedback
      </h3>

      <div className="feedback-avg-row">
        <Avg label="Technical" value={summary.averageTechnicalScore} />
        <Avg label="Communication" value={summary.averageCommunicationScore} />
        <Avg label="Problem-solving" value={summary.averageProblemSolvingScore} />
        <Avg label="Culture fit" value={summary.averageCultureFitScore} />
      </div>

      <ul style={{ marginTop: "1rem", display: "flex", flexDirection: "column", gap: "0.75rem" }}>
        {summary.scorecards.map((s) => (
          <li key={s.id} className="feedback-scorecard-row">
            <div className="job-card-top">
              <span>{s.authorName}</span>
              <Badge tone={RECOMMENDATION_TONE[s.recommendation] ?? "neutral"}>{s.recommendation}</Badge>
            </div>
            <p className="hint">
              Technical {s.technicalScore} · Communication {s.communicationScore} · Problem-solving {s.problemSolvingScore} · Culture fit {s.cultureFitScore}
            </p>
            {s.strengths && <p><strong>Strengths:</strong> {s.strengths}</p>}
            {s.concerns && <p><strong>Concerns:</strong> {s.concerns}</p>}
          </li>
        ))}
      </ul>
    </Card>
  );
}
