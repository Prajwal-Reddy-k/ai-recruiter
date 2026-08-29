import type { ReactNode } from "react";

interface StatCardProps {
  icon?: ReactNode;
  value: ReactNode;
  label: string;
  tone?: "primary" | "accent" | "neutral";
}

export default function StatCard({ icon, value, label, tone = "neutral" }: StatCardProps) {
  return (
    <div className={`stat-card stat-card-${tone}`}>
      {icon && <span className="stat-card-icon">{icon}</span>}
      <span className="stat-card-value">{value}</span>
      <span className="stat-card-label">{label}</span>
    </div>
  );
}
