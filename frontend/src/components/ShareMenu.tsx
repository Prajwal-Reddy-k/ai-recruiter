import { useState } from "react";
import { Link2, Mail, Share2 } from "lucide-react";
import Modal from "./ui/Modal";
import Button from "./ui/Button";
import { useToast } from "../context/ToastContext";
import {
  buildEmailShareUrl,
  buildJobShareUrl,
  buildLinkedInShareUrl,
  buildWhatsAppShareUrl,
  copyJobShareLink,
  shareJobNatively,
  trackJobShare,
} from "../utils/shareLink";

/** Share button + menu for a job — Copy Link, WhatsApp, LinkedIn, Email, with native
 * Web Share used directly on supported devices (mostly mobile) instead of opening the menu. */
export default function ShareMenu({ jobId, jobTitle }: { jobId: number; jobTitle: string }) {
  const toast = useToast();
  const [open, setOpen] = useState(false);

  async function handleShareClick() {
    const sharedNatively = await shareJobNatively({ jobId, title: jobTitle });
    if (!sharedNatively) setOpen(true);
  }

  async function handleCopyLink() {
    const copied = await copyJobShareLink(jobId);
    if (copied) toast.success("Link copied.");
    else toast.error("Couldn't copy the link — copy it manually.");
    setOpen(false);
  }

  function handleExternalShare(buildUrl: (url: string) => string) {
    const url = buildJobShareUrl(jobId);
    window.open(buildUrl(url), "_blank", "noopener,noreferrer");
    trackJobShare(jobId);
    setOpen(false);
  }

  return (
    <>
      <button
        type="button"
        className="save-btn"
        onClick={handleShareClick}
        aria-label="Share this job"
        title="Share this job"
      >
        <Share2 size={17} />
      </button>

      <Modal open={open} onClose={() => setOpen(false)} title="Share this job">
        <div style={{ display: "flex", flexDirection: "column", gap: "0.5rem" }}>
          <Button variant="secondary" icon={<Link2 size={16} />} onClick={handleCopyLink}>
            Copy link
          </Button>
          <Button
            variant="secondary"
            onClick={() => handleExternalShare((url) => buildWhatsAppShareUrl(url, jobTitle))}
          >
            Share on WhatsApp
          </Button>
          <Button
            variant="secondary"
            onClick={() => handleExternalShare(buildLinkedInShareUrl)}
          >
            Share on LinkedIn
          </Button>
          <Button
            variant="secondary"
            icon={<Mail size={16} />}
            onClick={() => handleExternalShare((url) => buildEmailShareUrl(url, jobTitle))}
          >
            Share via email
          </Button>
        </div>
      </Modal>
    </>
  );
}
