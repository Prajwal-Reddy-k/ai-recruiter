import { copyToClipboard } from "./clipboard";
import { recordJobShare } from "../api/jobs";

/** Builds a shareable link for a job, with a client-only, never-persisted token appended
 * so an external analytics tool can distinguish individual share instances. The token is
 * never sent to or stored by the backend — the aggregate share count is a separate counter
 * incremented via recordJobShare(). */
export function buildJobShareUrl(jobId: number): string {
  const token = crypto.randomUUID().slice(0, 8);
  return `${window.location.origin}/jobs/${jobId}?st=${token}`;
}

export function buildWhatsAppShareUrl(url: string, title: string): string {
  return `https://wa.me/?text=${encodeURIComponent(`${title}\n${url}`)}`;
}

export function buildLinkedInShareUrl(url: string): string {
  return `https://www.linkedin.com/sharing/share-offsite/?url=${encodeURIComponent(url)}`;
}

export function buildEmailShareUrl(url: string, title: string): string {
  const subject = encodeURIComponent(`Job opportunity: ${title}`);
  const body = encodeURIComponent(`Thought this might interest you:\n\n${title}\n${url}`);
  return `mailto:?subject=${subject}&body=${body}`;
}

/** Fire-and-forget share-count increment — never blocks the share action itself. */
export function trackJobShare(jobId: number): void {
  recordJobShare(jobId).catch(() => {
    // Best-effort analytics only — a failed count increment should never surface to the user.
  });
}

export interface ShareJobOptions {
  jobId: number;
  title: string;
}

/** Uses the native Web Share API when available (mostly mobile), otherwise returns false so
 * the caller can fall back to a share menu with explicit Copy Link / WhatsApp / LinkedIn / Email options. */
export async function shareJobNatively(options: ShareJobOptions): Promise<boolean> {
  const url = buildJobShareUrl(options.jobId);
  if (navigator.share) {
    try {
      await navigator.share({ title: options.title, url });
      trackJobShare(options.jobId);
      return true;
    } catch {
      return false;
    }
  }
  return false;
}

export async function copyJobShareLink(jobId: number): Promise<boolean> {
  const url = buildJobShareUrl(jobId);
  const copied = await copyToClipboard(url);
  if (copied) trackJobShare(jobId);
  return copied;
}
