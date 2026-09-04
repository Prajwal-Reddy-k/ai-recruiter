import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Download, ShieldCheck } from "lucide-react";
import { cancelAccountDeletion, downloadAccountDataExportCsv, getAccountDataExportJson, getPrivacySummary } from "../api/account";
import type { PrivacySummary } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { saveBlobAsFile } from "../utils/download";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import ConfirmDialog from "../components/ui/ConfirmDialog";

export default function PrivacyCenterPage() {
  const toast = useToast();
  const [summary, setSummary] = useState<PrivacySummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [exporting, setExporting] = useState<"json" | "csv" | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const [confirmCancelOpen, setConfirmCancelOpen] = useState(false);

  function load() {
    setLoading(true);
    getPrivacySummary()
      .then(setSummary)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load privacy settings")))
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  async function handleExportJson() {
    setExporting("json");
    try {
      const data = await getAccountDataExportJson();
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
      saveBlobAsFile(blob, "my-account-data.json");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to export your data"));
    } finally {
      setExporting(null);
    }
  }

  async function handleExportCsv() {
    setExporting("csv");
    try {
      const blob = await downloadAccountDataExportCsv();
      saveBlobAsFile(blob, "my-account-data.csv");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to export your data"));
    } finally {
      setExporting(null);
    }
  }

  async function handleCancelDeletion() {
    setCancelling(true);
    try {
      const status = await cancelAccountDeletion();
      setSummary((prev) => (prev ? { ...prev, deletion: status } : prev));
      toast.success("Account deletion cancelled.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to cancel deletion request"));
    } finally {
      setCancelling(false);
      setConfirmCancelOpen(false);
    }
  }

  if (loading) return <p>Loading...</p>;
  if (!summary) return <p className="error">Unable to load your privacy settings.</p>;

  return (
    <div style={{ maxWidth: 720, margin: "0 auto" }}>
      <PageHeader
        title={<><ShieldCheck size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Privacy & Data</>}
        subtitle="Review your privacy settings, download a copy of your data, and manage account deletion."
      />

      <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <h3>Profile visibility</h3>
        {summary.profileVisibility ? (
          <p className="hint" style={{ marginTop: "0.5rem" }}>
            Currently: <strong>{summary.profileVisibility}</strong>. <Link to="/profile">Manage on your profile →</Link>
          </p>
        ) : (
          <p className="hint" style={{ marginTop: "0.5rem" }}>Profile visibility only applies to candidate accounts.</p>
        )}
      </Card>

      <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <h3>Notification preferences</h3>
        <ul className="hint" style={{ marginTop: "0.5rem" }}>
          <li>Messages: {summary.messagesEnabled ? "On" : "Off"}</li>
          <li>Applications: {summary.applicationsEnabled ? "On" : "Off"}</li>
          <li>Interviews: {summary.interviewsEnabled ? "On" : "Off"}</li>
          <li>Invitations: {summary.invitationsEnabled ? "On" : "Off"}</li>
        </ul>
        <p className="hint" style={{ marginTop: "0.5rem" }}><Link to="/settings">Manage in Settings →</Link></p>
      </Card>

      <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <h3>Download your data</h3>
        <p className="hint" style={{ marginTop: "0.5rem" }}>
          Get a copy of your account data — profile details, applications, saved jobs, saved searches, followed companies, and reviews you've written.
          This never includes your password, security tokens, or another user's data.
        </p>
        <div style={{ display: "flex", gap: "0.75rem", marginTop: "0.75rem" }}>
          <Button variant="secondary" icon={<Download size={16} />} onClick={handleExportJson} loading={exporting === "json"}>
            Download as JSON
          </Button>
          <Button variant="secondary" icon={<Download size={16} />} onClick={handleExportCsv} loading={exporting === "csv"}>
            Download as CSV
          </Button>
        </div>
      </Card>

      <Card className="ui-card-padded">
        <h3>Account deletion</h3>
        {summary.deletion.isPending ? (
          <>
            <p className="error" style={{ marginTop: "0.5rem" }}>
              Your account is scheduled to be deactivated on{" "}
              {summary.deletion.scheduledDeactivationAtUtc && new Date(summary.deletion.scheduledDeactivationAtUtc).toLocaleDateString()}
              {" "}({summary.deletion.daysRemaining} day{summary.deletion.daysRemaining === 1 ? "" : "s"} remaining).
            </p>
            <Button variant="secondary" style={{ marginTop: "0.5rem" }} onClick={() => setConfirmCancelOpen(true)}>
              Cancel deletion
            </Button>
          </>
        ) : (
          <p className="hint" style={{ marginTop: "0.5rem" }}>
            No deletion is currently scheduled. You can request account deletion from <Link to="/settings">Settings</Link>.
          </p>
        )}
      </Card>

      <ConfirmDialog
        open={confirmCancelOpen}
        onCancel={() => setConfirmCancelOpen(false)}
        onConfirm={handleCancelDeletion}
        title="Cancel account deletion"
        confirmLabel="Cancel deletion"
        loading={cancelling}
      >
        <p>Your account will remain active and this scheduled deactivation will be cancelled. Continue?</p>
      </ConfirmDialog>
    </div>
  );
}
