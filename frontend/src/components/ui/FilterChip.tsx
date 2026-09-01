import { X } from "lucide-react";

interface FilterChipProps {
  label: string;
  onRemove: () => void;
}

export default function FilterChip({ label, onRemove }: FilterChipProps) {
  return (
    <span className="filter-chip-active-pill">
      {label}
      <button type="button" onClick={onRemove} aria-label={`Remove filter: ${label}`}>
        <X size={12} />
      </button>
    </span>
  );
}
