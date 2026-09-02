import { useState, type FormEvent } from "react";
import { createReferral } from "../api/referrals";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { copyToClipboard } from "../utils/clipboard";
import { useToast } from "../context/ToastContext";
import Modal from "./ui/Modal";
import Button from "./ui/Button";
import FormField from "./ui/FormField";

interface CreateReferralModalProps {
  jobPostingId: number;
  jobTitle: string;
  onClose: () => void;
}

export default function CreateReferralModal({ jobPostingId, jobTitle, onClose }: CreateReferralModalProps) {
  const toast = useToast();
  const [referredName, setReferredName] = useState("");
  const [referredEmail, setReferredEmail] = useState("");
  const [referredPhone, setReferredPhone] = useState("");
  const [relevantSkillsCsv, setRelevantSkillsCsv] = useState("");
  const [note, setNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [referralLink, setReferralLink] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      const response = await createReferral({
        referredName, referredEmail,
        referredPhone: referredPhone || undefined,
        relevantSkillsCsv: relevantSkillsCsv || undefined,
        note: note || undefined,
        jobPostingId,
      });
      setReferralLink(`${window.location.origin}/register?ref=${response.rawToken}`);
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe);
      else setErrors({ general: getErrorMessage(err, "Failed to create referral") });
    } finally {
      setSaving(false);
    }
  }

  async function handleCopyLink() {
    if (!referralLink) return;
    const copied = await copyToClipboard(referralLink);
    if (copied) toast.success("Link copied.");
    else toast.error("Couldn't copy the link — copy it manually.");
  }

  if (referralLink) {
    return (
      <Modal open onClose={onClose} title="Referral created" footer={<Button onClick={onClose}>Done</Button>}>
        <p>Share this link with the person you're referring for <strong>{jobTitle}</strong>:</p>
        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem", marginTop: "0.75rem" }}>
          <code style={{ wordBreak: "break-all" }}>{referralLink}</code>
        </div>
        <Button size="sm" variant="secondary" style={{ marginTop: "0.75rem" }} onClick={handleCopyLink}>Copy link</Button>
        <p className="hint" style={{ marginTop: "0.75rem" }}>No email is sent automatically — share this link however you'd like.</p>
      </Modal>
    );
  }

  return (
    <Modal
      open
      onClose={onClose}
      title={`Refer a friend for ${jobTitle}`}
      footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Create referral link</Button></>}
    >
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Their name" htmlFor="ref-name" required error={errors.referredName}>
          <input id="ref-name" value={referredName} onChange={(e) => setReferredName(e.target.value)} />
        </FormField>
        <FormField label="Their email" htmlFor="ref-email" required error={errors.referredEmail}>
          <input id="ref-email" type="email" value={referredEmail} onChange={(e) => setReferredEmail(e.target.value)} />
        </FormField>
        <FormField label="Their phone" htmlFor="ref-phone" hint="Optional" error={errors.referredPhone}>
          <input id="ref-phone" value={referredPhone} onChange={(e) => setReferredPhone(e.target.value)} />
        </FormField>
        <FormField label="Relevant skills" htmlFor="ref-skills" hint="Comma separated, optional">
          <input id="ref-skills" value={relevantSkillsCsv} onChange={(e) => setRelevantSkillsCsv(e.target.value)} />
        </FormField>
        <FormField label="Note" htmlFor="ref-note" hint="Optional">
          <textarea id="ref-note" rows={3} value={note} onChange={(e) => setNote(e.target.value)} maxLength={2000} />
        </FormField>
        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}
