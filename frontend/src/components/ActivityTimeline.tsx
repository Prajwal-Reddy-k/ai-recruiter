import type { ActivityTimelineEntry } from "../types";
import { formatRelativeTime, toIST } from "../utils/format";

interface ActivityTimelineProps {
  entries: ActivityTimelineEntry[];
}

function toDisplayLabel(type: string): string {
  return type.replace(/([a-z])([A-Z])/g, "$1 $2");
}

/** Generic version of ApplicationTimeline — reuses its exact CSS classes, but shows a
 * relative timestamp as the primary label (full timestamp available via title/tooltip)
 * instead of a raw toLocaleString(). */
export default function ActivityTimeline({ entries }: ActivityTimelineProps) {
  if (entries.length === 0) return null;

  return (
    <ol className="app-timeline">
      {entries.map((entry, i) => (
        <li key={i} className="app-timeline-item">
          <span className="app-timeline-dot" aria-hidden="true" />
          <div className="app-timeline-content">
            <p className="app-timeline-status">{toDisplayLabel(entry.type)}</p>
            <p className="hint">{entry.message}</p>
            <p className="hint" title={toIST(entry.timestampUtc)}>{formatRelativeTime(entry.timestampUtc)}</p>
          </div>
        </li>
      ))}
    </ol>
  );
}
