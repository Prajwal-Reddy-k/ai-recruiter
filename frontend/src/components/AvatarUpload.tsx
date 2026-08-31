import { useRef, useState } from "react";
import { Trash2, UploadCloud } from "lucide-react";
import { removeAvatar, uploadAvatar } from "../api/candidates";
import type { CandidateProfile } from "../types";
import { getErrorMessage } from "../utils/errors";
import { resolveAvatarUrl } from "../utils/format";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import Avatar from "./ui/Avatar";

const ALLOWED_EXTENSIONS = [".jpg", ".jpeg", ".png", ".webp"];
const MAX_SIZE_BYTES = 5 * 1024 * 1024;

interface AvatarUploadProps {
  profile: CandidateProfile;
  onChange: (profile: CandidateProfile) => void;
}

type Status = "idle" | "uploading" | "removing";

function validateClientSide(file: File): string | null {
  const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
  if (!ALLOWED_EXTENSIONS.includes(extension)) {
    return "Only JPG, JPEG, PNG, and WEBP images are supported.";
  }
  if (file.size > MAX_SIZE_BYTES) {
    return "Image must be 5 MB or smaller.";
  }
  return null;
}

export default function AvatarUpload({ profile, onChange }: AvatarUploadProps) {
  const toast = useToast();
  const { updateAvatarUrl } = useAuth();
  const [status, setStatus] = useState<Status>("idle");
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  async function handleFileChange() {
    const file = fileInputRef.current?.files?.[0];
    if (!file) return;

    const clientError = validateClientSide(file);
    if (clientError) {
      setError(clientError);
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }

    setStatus("uploading");
    setProgress(0);
    setError(null);

    try {
      const updated = await uploadAvatar(file, setProgress);
      onChange(updated);
      updateAvatarUrl(updated.avatarUrl);
      toast.success("Profile photo updated.");
    } catch (err) {
      setError(getErrorMessage(err, "Photo upload failed"));
    } finally {
      setStatus("idle");
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  async function handleRemove() {
    setStatus("removing");
    setError(null);
    try {
      const updated = await removeAvatar();
      onChange(updated);
      updateAvatarUrl(updated.avatarUrl);
      toast.success("Profile photo removed.");
    } catch (err) {
      setError(getErrorMessage(err, "Failed to remove photo"));
    } finally {
      setStatus("idle");
    }
  }

  return (
    <div className="avatar-upload">
      <Avatar name={profile.fullName} size={88} src={resolveAvatarUrl(profile.avatarUrl)} />

      <div className="avatar-upload-controls">
        <label className="btn btn-secondary btn-sm avatar-upload-label">
          <UploadCloud size={16} />
          {profile.avatarUrl ? "Replace photo" : "Upload photo"}
          <input
            ref={fileInputRef}
            type="file"
            accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
            onChange={handleFileChange}
            disabled={status !== "idle"}
            aria-label="Upload profile photo"
            style={{ display: "none" }}
          />
        </label>

        {profile.avatarUrl && (
          <button
            type="button"
            className="btn btn-ghost btn-sm"
            onClick={handleRemove}
            disabled={status !== "idle"}
            aria-label="Remove profile photo"
          >
            <Trash2 size={16} /> Remove
          </button>
        )}
      </div>

      {status === "uploading" && (
        <div className="upload-progress" role="status" aria-live="polite">
          <div className="upload-progress-bar" style={{ width: `${progress}%` }} />
          <span>{progress}%</span>
        </div>
      )}
      {status === "removing" && <p className="hint">Removing...</p>}
      {error && (
        <span className="field-error" role="alert">
          {error}
        </span>
      )}
      <p className="hint">JPG, PNG, or WEBP, up to 5 MB. Square images look best.</p>
    </div>
  );
}
