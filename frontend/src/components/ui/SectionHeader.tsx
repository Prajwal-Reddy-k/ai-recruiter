import type { ReactNode } from "react";

interface SectionHeaderProps {
  title: string;
  subtitle?: string;
  action?: ReactNode;
  as?: "h2" | "h3";
}

export default function SectionHeader({ title, subtitle, action, as = "h2" }: SectionHeaderProps) {
  const Heading = as;
  return (
    <div className="section-header">
      <div>
        <Heading>{title}</Heading>
        {subtitle && <p className="section-header-subtitle">{subtitle}</p>}
      </div>
      {action && <div className="section-header-action">{action}</div>}
    </div>
  );
}
