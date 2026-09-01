import type { StatusHistoryEntry } from "../types";

interface ApplicationTimelineProps {
  entries: StatusHistoryEntry[];
}

export default function ApplicationTimeline({ entries }: ApplicationTimelineProps) {
  if (entries.length === 0) return null;

  return (
    <ol className="app-timeline">
      {entries.map((entry, i) => (
        <li key={i} className="app-timeline-item">
          <span className="app-timeline-dot" aria-hidden="true" />
          <div className="app-timeline-content">
            <p className="app-timeline-status">
              {entry.fromStatus ? `${entry.fromStatus} → ${entry.toStatus}` : entry.toStatus}
            </p>
            <p className="hint">{new Date(entry.changedAt).toLocaleString()} · by {entry.changedByName}</p>
            {entry.note && <p className="app-timeline-note">{entry.note}</p>}
          </div>
        </li>
      ))}
    </ol>
  );
}
