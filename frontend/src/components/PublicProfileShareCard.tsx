import { useEffect, useState } from "react";
import { Link2 } from "lucide-react";
import { getMyPublicProfilePreview } from "../api/publicProfile";
import { getErrorMessage } from "../utils/errors";
import { copyToClipboard } from "../utils/clipboard";
import { useToast } from "../context/ToastContext";
import type { PublicProfilePreview } from "../types";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import Avatar from "../components/ui/Avatar";
import { resolveAvatarUrl } from "../utils/format";

export default function PublicProfileShareCard() {
  const toast = useToast();
  const [preview, setPreview] = useState<PublicProfilePreview | null>(null);
  const [loading, setLoading] = useState(true);
  const [showPreview, setShowPreview] = useState(false);

  useEffect(() => {
    getMyPublicProfilePreview()
      .then(setPreview)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load your public profile")))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleCopyLink() {
    if (!preview?.publicUrl) return;
    const fullUrl = `${window.location.origin}${preview.publicUrl}`;
    const copied = await copyToClipboard(fullUrl);
    if (copied) toast.success("Link copied.");
    else toast.error("Couldn't copy the link — copy it manually.");
  }

  if (loading || !preview) return null;

  return (
    <Card className="ui-card-padded" style={{ marginTop: "1rem" }}>
      <h3>Public profile link</h3>
      {preview.isCurrentlyPublic && preview.publicUrl ? (
        <>
          <p className="hint" style={{ marginTop: "0.5rem" }}>Anyone with this link can view your public profile:</p>
          <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", marginTop: "0.5rem" }}>
            <code>{window.location.origin}{preview.publicUrl}</code>
            <Button size="sm" variant="secondary" icon={<Link2 size={14} />} onClick={handleCopyLink}>Copy link</Button>
          </div>
        </>
      ) : (
        <p className="hint" style={{ marginTop: "0.5rem" }}>Save your profile as "Public shareable" to get a link you can share.</p>
      )}

      <Button size="sm" variant="secondary" style={{ marginTop: "0.75rem" }} onClick={() => setShowPreview((v) => !v)}>
        {showPreview ? "Hide preview" : "Preview public profile"}
      </Button>

      {showPreview && (
        <div style={{ marginTop: "1rem", border: "1px solid var(--border)", borderRadius: "var(--radius-md)", padding: "1rem" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "0.75rem" }}>
            <Avatar name={preview.preview.fullName} size={48} src={resolveAvatarUrl(preview.preview.avatarUrl)} />
            <div>
              <strong>{preview.preview.fullName}</strong>
              {preview.preview.headline && <p className="hint">{preview.preview.headline}</p>}
            </div>
          </div>
          {preview.preview.skillsCsv && (
            <div className="chip-list" style={{ marginTop: "0.75rem" }}>
              {preview.preview.skillsCsv.split(",").map((s) => s.trim()).filter(Boolean).map((s) => <span key={s} className="chip">{s}</span>)}
            </div>
          )}
          <p className="hint" style={{ marginTop: "0.75rem" }}>This is what recruiters and anyone with your link will see — email, phone, resume file, applications, and messages are never shown here.</p>
        </div>
      )}
    </Card>
  );
}
