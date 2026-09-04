import { useState } from "react";
import { ChevronDown, ChevronUp } from "lucide-react";
import type { JobQualityScore } from "../types";
import { Badge, type BadgeTone } from "./ui/Badge";

function toneForScore(score: number): BadgeTone {
  if (score >= 80) return "success";
  if (score >= 50) return "warning";
  return "danger";
}

/** Recruiter-only quality-score display — never rendered on any public/candidate-facing
 * job view. Expandable to show the specific missing-item suggestions the backend returns. */
export default function JobQualityScoreBadge({ qualityScore }: { qualityScore: JobQualityScore }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <div>
      <button
        type="button"
        className="link-button"
        style={{ display: "inline-flex", alignItems: "center", gap: "0.3rem" }}
        onClick={() => setExpanded((v) => !v)}
        aria-expanded={expanded}
      >
        <Badge tone={toneForScore(qualityScore.score)}>Quality score: {qualityScore.score}/100</Badge>
        {qualityScore.suggestions.length > 0 && (expanded ? <ChevronUp size={14} /> : <ChevronDown size={14} />)}
      </button>

      {expanded && qualityScore.suggestions.length > 0 && (
        <ul className="job-quality-suggestions">
          {qualityScore.suggestions.map((s) => (
            <li key={s.label} title={s.tip}>{s.label}</li>
          ))}
        </ul>
      )}
    </div>
  );
}
