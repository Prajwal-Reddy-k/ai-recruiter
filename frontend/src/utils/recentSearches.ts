const MAX_RECENT = 5;

function storageKey(userId: number): string {
  return `recentSearches:${userId}`;
}

export function getRecentSearches(userId: number): string[] {
  try {
    const raw = localStorage.getItem(storageKey(userId));
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.filter((s): s is string => typeof s === "string") : [];
  } catch {
    return [];
  }
}

export function addRecentSearch(userId: number, query: string): string[] {
  const trimmed = query.trim();
  if (!trimmed) return getRecentSearches(userId);

  const existing = getRecentSearches(userId).filter((s) => s.toLowerCase() !== trimmed.toLowerCase());
  const next = [trimmed, ...existing].slice(0, MAX_RECENT);
  try {
    localStorage.setItem(storageKey(userId), JSON.stringify(next));
  } catch {
    // Storage can fail in private browsing — recent searches are a convenience, not critical.
  }
  return next;
}
