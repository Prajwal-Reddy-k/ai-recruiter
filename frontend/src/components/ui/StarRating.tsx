import { Star } from "lucide-react";

interface StarRatingProps {
  value: number;
  onChange?: (value: number) => void;
  size?: number;
  label?: string;
}

/** Controlled 1-5 star rating. Read-only (display) when onChange is omitted. */
export default function StarRating({ value, onChange, size = 18, label }: StarRatingProps) {
  const readOnly = !onChange;

  return (
    <div style={{ display: "inline-flex", alignItems: "center", gap: "0.15rem" }} role={readOnly ? "img" : "radiogroup"} aria-label={label ?? `${value} out of 5 stars`}>
      {[1, 2, 3, 4, 5].map((star) => (
        <button
          key={star}
          type="button"
          disabled={readOnly}
          onClick={() => onChange?.(star)}
          aria-label={`${star} star${star === 1 ? "" : "s"}`}
          aria-pressed={!readOnly && value === star}
          style={{
            background: "none",
            border: "none",
            padding: 0,
            cursor: readOnly ? "default" : "pointer",
            color: star <= value ? "var(--color-accent)" : "var(--border)",
            display: "inline-flex",
          }}
        >
          <Star size={size} fill={star <= value ? "currentColor" : "none"} />
        </button>
      ))}
    </div>
  );
}
