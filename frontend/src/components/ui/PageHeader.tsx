import type { ReactNode } from "react";

interface PageHeaderProps {
  title: ReactNode;
  subtitle?: string;
  action?: ReactNode;
}

/** Codifies the `.page-header` pattern already used inline across most pages, so title +
 * subtitle + one primary action stay visually consistent everywhere it's adopted. */
export default function PageHeader({ title, subtitle, action }: PageHeaderProps) {
  return (
    <div className="page-header page-header-with-action">
      <div>
        <h1>{title}</h1>
        {subtitle && <p>{subtitle}</p>}
      </div>
      {action && <div className="page-header-action">{action}</div>}
    </div>
  );
}
