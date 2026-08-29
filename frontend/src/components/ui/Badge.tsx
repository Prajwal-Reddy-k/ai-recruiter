export type BadgeTone = "success" | "warning" | "danger" | "info" | "neutral" | "accent";

interface BadgeProps {
  children: React.ReactNode;
  tone?: BadgeTone;
  className?: string;
}

export function Badge({ children, tone = "neutral", className }: BadgeProps) {
  return <span className={["ui-badge", `ui-badge-${tone}`, className ?? ""].filter(Boolean).join(" ")}>{children}</span>;
}

// Single source of truth for status -> visual tone across the whole app, instead of
// re-deciding "is this status good or bad" inline on every page.
const STATUS_TONES: Record<string, BadgeTone> = {
  Open: "success",
  Hired: "success",
  Offer: "success",
  Scheduled: "success",
  Approved: "success",
  Draft: "neutral",
  Applied: "info",
  Screening: "info",
  Shortlisted: "accent",
  InterviewScheduled: "accent",
  InterviewCompleted: "accent",
  Proposed: "accent",
  Rejected: "danger",
  Cancelled: "danger",
  Removed: "danger",
  Dismissed: "neutral",
  Withdrawn: "neutral",
  Closed: "neutral",
  Archived: "neutral",
  Hidden: "warning",
  Pending: "warning",
  Reviewed: "neutral",
};

function toDisplayLabel(status: string): string {
  // "InterviewScheduled" -> "Interview Scheduled"
  return status.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export function StatusBadge({ status, className }: { status: string; className?: string }) {
  const tone = STATUS_TONES[status] ?? "neutral";
  return (
    <Badge tone={tone} className={className}>
      {toDisplayLabel(status)}
    </Badge>
  );
}
