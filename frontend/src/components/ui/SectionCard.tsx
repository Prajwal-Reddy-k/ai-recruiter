import type { ReactNode } from "react";
import Card from "./Card";
import SectionHeader from "./SectionHeader";

interface SectionCardProps {
  title: string;
  subtitle?: string;
  action?: ReactNode;
  children: ReactNode;
}

/** Card + SectionHeader, pre-wired — the pairing every dashboard section already reaches for,
 * codified so new sections don't re-derive the spacing/heading combo by hand. */
export default function SectionCard({ title, subtitle, action, children }: SectionCardProps) {
  return (
    <Card>
      <SectionHeader title={title} subtitle={subtitle} action={action} as="h3" />
      {children}
    </Card>
  );
}
