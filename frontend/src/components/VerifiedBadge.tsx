import { ShieldCheck } from "lucide-react";
import { Badge } from "./ui/Badge";

/** "Platform Verified" badge — shown only once a company's verification has been approved
 * by an Admin. Deliberately not labeled as a government/legal verification. */
export default function VerifiedBadge({ className }: { className?: string }) {
  return (
    <Badge tone="success" className={className}>
      <span
        style={{ display: "inline-flex", alignItems: "center", gap: "0.25rem" }}
        title="Reviewed and approved by the AI Recruiter platform team — not a government or legal verification."
      >
        <ShieldCheck size={12} /> Platform Verified
      </span>
    </Badge>
  );
}
