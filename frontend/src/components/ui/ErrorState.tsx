import type { ReactNode } from "react";
import { AlertTriangle } from "lucide-react";

interface ErrorStateProps {
  title?: string;
  description?: string;
  action?: ReactNode;
}

/** Mirrors EmptyState.tsx for the "load failed" case — a fixed warning icon and a default
 * title, since a failed load is inherently negative, unlike EmptyState's neutral "nothing
 * here yet" framing. */
export default function ErrorState({ title = "Something went wrong", description, action }: ErrorStateProps) {
  return (
    <div className="empty-state empty-state-error">
      <span className="empty-state-icon"><AlertTriangle size={32} /></span>
      <h3>{title}</h3>
      {description && <p>{description}</p>}
      {action && <div className="empty-state-action">{action}</div>}
    </div>
  );
}
