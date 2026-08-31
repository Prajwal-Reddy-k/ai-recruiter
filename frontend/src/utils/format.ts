/** Resolves an API-relative avatar path (e.g. "/candidates/5/avatar") into an absolute URL
 * usable as an <img src>, by prefixing the configured API base URL. */
export function resolveAvatarUrl(avatarUrl: string | null | undefined): string | undefined {
  if (!avatarUrl) return undefined;
  return `${import.meta.env.VITE_API_BASE_URL}${avatarUrl}`;
}

export function getInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

export function formatRelativeTime(dateIso: string): string {
  const date = new Date(dateIso);
  const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

  if (seconds < 60) return "Just now";
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days === 1) return "Yesterday";
  if (days < 30) return `${days}d ago`;
  const months = Math.floor(days / 30);
  if (months < 12) return `${months}mo ago`;
  return `${Math.floor(months / 12)}y ago`;
}

export function formatSalaryRange(min: number | null, max: number | null): string {
  if (min === null && max === null) return "Not disclosed";
  const fmt = (n: number) => (n >= 1000 ? `${Math.round(n / 1000)}k` : `${n}`);
  if (min !== null && max !== null) return `₹${fmt(min)} - ₹${fmt(max)}`;
  return `₹${fmt((min ?? max)!)}+`;
}

export function formatExperienceRange(minYears: number | null, maxYears: number | null): string {
  if (minYears == null && maxYears == null) return "Any experience";
  if (minYears != null && maxYears != null) return `${minYears}-${maxYears} yrs`;
  return `${minYears ?? maxYears}+ yrs`;
}

export function daysAgo(dateIso: string): number {
  const date = new Date(dateIso);
  return Math.floor((Date.now() - date.getTime()) / (1000 * 60 * 60 * 24));
}

/** Renders a UTC ISO timestamp in India Standard Time (fixed UTC+5:30, no DST). */
export function toIST(utcIso: string): string {
  const date = new Date(utcIso);
  return (
    date.toLocaleString("en-IN", {
      timeZone: "Asia/Kolkata",
      day: "numeric",
      month: "short",
      year: "numeric",
      hour: "numeric",
      minute: "2-digit",
      hour12: true,
    }) + " IST"
  );
}
