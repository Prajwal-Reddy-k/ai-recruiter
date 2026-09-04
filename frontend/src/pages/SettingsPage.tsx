import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { Bell, KeyRound, ShieldAlert, User as UserIcon } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import {
  cancelAccountDeletion,
  changePassword,
  getNotificationPreferences,
  getPrivacySummary,
  requestAccountDeletion,
  updateNotificationPreferences,
} from "../api/account";
import { getMyCandidateProfile } from "../api/candidates";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import type { AccountDeletionStatus, NotificationPreferences } from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import PasswordInput from "../components/ui/PasswordInput";
import Switch from "../components/ui/Switch";
import ConfirmDialog from "../components/ui/ConfirmDialog";
import Avatar from "../components/ui/Avatar";
import YourDetailsCard from "../components/YourDetailsCard";
import { resolveAvatarUrl } from "../utils/format";

interface PasswordFieldErrors {
  currentPassword?: string;
  newPassword?: string;
  confirmPassword?: string;
  general?: string;
}

function ChangePasswordCard() {
  const toast = useToast();
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<PasswordFieldErrors>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    if (newPassword.length < 8) {
      setFieldErrors({ newPassword: "New password must be at least 8 characters." });
      return;
    }
    if (newPassword !== confirmPassword) {
      setFieldErrors({ confirmPassword: "Passwords do not match." });
      return;
    }

    setSaving(true);
    try {
      await changePassword({ currentPassword, newPassword, confirmPassword });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      toast.success("Password changed. Other signed-in devices have been logged out.");
    } catch (err) {
      const serverFieldErrors = getFieldErrors(err);
      if (serverFieldErrors) {
        setFieldErrors(serverFieldErrors as PasswordFieldErrors);
      } else {
        setFieldErrors({ general: getErrorMessage(err, "Failed to change password") });
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card className="ui-card-padded">
      <h2><KeyRound size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />Change password</h2>
      <p className="hint" style={{ marginBottom: "1rem" }}>Changing your password signs you out of any other active sessions.</p>
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Current password" htmlFor="settings-current-password" required error={fieldErrors.currentPassword}>
          <PasswordInput id="settings-current-password" value={currentPassword} onChange={(e) => setCurrentPassword(e.target.value)} aria-required="true" />
        </FormField>
        <FormField label="New password" htmlFor="settings-new-password" required hint="At least 8 characters." error={fieldErrors.newPassword}>
          <PasswordInput id="settings-new-password" value={newPassword} onChange={(e) => setNewPassword(e.target.value)} aria-required="true" />
        </FormField>
        <FormField label="Confirm new password" htmlFor="settings-confirm-password" required error={fieldErrors.confirmPassword}>
          <PasswordInput id="settings-confirm-password" value={confirmPassword} onChange={(e) => setConfirmPassword(e.target.value)} aria-required="true" />
        </FormField>

        {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

        <div className="form-actions">
          <Button type="submit" loading={saving}>Change password</Button>
        </div>
      </form>
    </Card>
  );
}

function NotificationPreferencesCard() {
  const toast = useToast();
  const [prefs, setPrefs] = useState<NotificationPreferences | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    getNotificationPreferences().then(setPrefs).catch(() => setPrefs(null));
  }, []);

  async function handleToggle(key: keyof NotificationPreferences, value: boolean) {
    if (!prefs) return;
    const next = { ...prefs, [key]: value };
    setPrefs(next);
    setSaving(true);
    try {
      await updateNotificationPreferences(next);
    } catch (err) {
      setPrefs(prefs);
      toast.error(getErrorMessage(err, "Failed to update notification preferences"));
    } finally {
      setSaving(false);
    }
  }

  if (!prefs) return null;

  const rows: { key: keyof NotificationPreferences; label: string; description: string }[] = [
    { key: "messagesEnabled", label: "Messages", description: "New messages from recruiters or candidates." },
    { key: "applicationsEnabled", label: "Applications", description: "New applicants and application status changes." },
    { key: "interviewsEnabled", label: "Interviews", description: "Interview proposals, reschedules, and responses." },
    { key: "invitationsEnabled", label: "Invitations", description: "Invitations to apply for a role." },
  ];

  return (
    <Card className="ui-card-padded">
      <h2><Bell size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />Notification preferences</h2>
      <p className="hint" style={{ marginBottom: "0.5rem" }}>
        Controls in-app notifications only — this app never sends email or SMS. Account-critical notices (like a job you posted expiring) always come through.
      </p>
      {rows.map((row) => (
        <div className="settings-toggle-row" key={row.key}>
          <div>
            <strong>{row.label}</strong>
            <p>{row.description}</p>
          </div>
          <Switch checked={prefs[row.key]} onChange={(v) => handleToggle(row.key, v)} label={`Toggle ${row.label} notifications`} />
        </div>
      ))}
      {saving && <p className="hint" style={{ marginTop: "0.5rem" }}>Saving…</p>}
    </Card>
  );
}

function PrivacyCard() {
  const { user } = useAuth();
  const [visibility, setVisibility] = useState<string | null>(null);

  useEffect(() => {
    if (user?.role === "Candidate") {
      getMyCandidateProfile().then((p) => setVisibility(p.profileVisibility)).catch(() => setVisibility(null));
    }
  }, [user?.role]);

  if (user?.role !== "Candidate") {
    return (
      <Card className="ui-card-padded">
        <h2>Privacy</h2>
        <p className="hint" style={{ marginBottom: "0.75rem" }}>No additional privacy controls apply to your account type.</p>
        <Link to="/privacy" className="btn btn-secondary btn-sm">Privacy & Data Center</Link>
      </Card>
    );
  }

  return (
    <Card className="ui-card-padded">
      <h2>Privacy</h2>
      <p className="hint" style={{ marginBottom: "0.75rem" }}>
        Profile visibility: <strong>{visibility ?? "Loading…"}</strong>
      </p>
      <div style={{ display: "flex", gap: "0.5rem" }}>
        <Link to="/profile" className="btn btn-secondary btn-sm">Manage on your profile</Link>
        <Link to="/privacy" className="btn btn-secondary btn-sm">Privacy & Data Center</Link>
      </div>
    </Card>
  );
}

function DangerZoneCard() {
  const toast = useToast();
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [deletionStatus, setDeletionStatus] = useState<AccountDeletionStatus | null>(null);
  const [cancelling, setCancelling] = useState(false);

  useEffect(() => {
    getPrivacySummary().then((s) => setDeletionStatus(s.deletion)).catch(() => {});
  }, []);

  async function handleConfirmDeletion() {
    if (!password) {
      setError("Enter your password to confirm.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      const status = await requestAccountDeletion({ password });
      setDeletionStatus(status);
      toast.info(`Account deletion scheduled for ${status.scheduledDeactivationAtUtc ? new Date(status.scheduledDeactivationAtUtc).toLocaleDateString() : "later"} — your account stays active until then.`);
      setConfirmOpen(false);
    } catch (err) {
      setError(getErrorMessage(err, "Failed to process your request"));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleCancelDeletion() {
    setCancelling(true);
    try {
      const status = await cancelAccountDeletion();
      setDeletionStatus(status);
      toast.success("Account deletion cancelled.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to cancel deletion request"));
    } finally {
      setCancelling(false);
    }
  }

  return (
    <div className="settings-danger-zone">
      <h3><ShieldAlert size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />Danger zone</h3>

      {deletionStatus?.isPending ? (
        <>
          <p className="error" style={{ marginBottom: "1rem" }}>
            Your account is scheduled for deactivation on{" "}
            {deletionStatus.scheduledDeactivationAtUtc && new Date(deletionStatus.scheduledDeactivationAtUtc).toLocaleDateString()}
            {" "}({deletionStatus.daysRemaining} day{deletionStatus.daysRemaining === 1 ? "" : "s"} remaining). Your account stays active and usable until then.
          </p>
          <Button variant="secondary" loading={cancelling} onClick={handleCancelDeletion}>Cancel deletion</Button>
        </>
      ) : (
        <>
          <p className="hint" style={{ marginBottom: "1rem" }}>
            Requesting account deletion starts a 14-day grace period — your account stays active and usable, and you
            can cancel at any time before it's deactivated. Your data is not permanently erased.
          </p>
          <Button variant="danger" onClick={() => { setConfirmOpen(true); setPassword(""); setError(null); }}>
            Request account deletion
          </Button>
        </>
      )}

      <ConfirmDialog
        open={confirmOpen}
        title="Request account deletion?"
        confirmLabel="Request deletion"
        danger
        loading={submitting}
        onConfirm={handleConfirmDeletion}
        onCancel={() => setConfirmOpen(false)}
      >
        <p style={{ marginBottom: "1rem" }}>
          Your account will be deactivated in 14 days unless you cancel before then. You'll stay signed in and can keep
          using the platform during this period. Enter your password to confirm.
        </p>
        <FormField label="Password" htmlFor="delete-confirm-password" error={error ?? undefined}>
          <PasswordInput id="delete-confirm-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </FormField>
      </ConfirmDialog>
    </div>
  );
}

export default function SettingsPage() {
  const { user } = useAuth();

  return (
    <div className="settings-layout">
      <PageHeader title="Account Settings" subtitle="Manage your profile, security, and notification preferences." />

      <Card className="ui-card-padded" style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
        <Avatar name={user?.fullName ?? "?"} size={56} src={resolveAvatarUrl(user?.avatarUrl)} />
        <div>
          <h2 style={{ display: "flex", alignItems: "center", gap: "0.4rem" }}><UserIcon size={16} />{user?.fullName}</h2>
          <p className="hint">{user?.email} · {user?.role}</p>
        </div>
      </Card>

      <YourDetailsCard />
      <ChangePasswordCard />
      <NotificationPreferencesCard />
      <PrivacyCard />
      <DangerZoneCard />
    </div>
  );
}
