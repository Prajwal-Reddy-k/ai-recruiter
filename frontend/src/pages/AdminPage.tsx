import { useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import {
  addReportNote,
  getAdminCompanies,
  getAdminJobs,
  getAdminReports,
  getAdminUsers,
  getAuditLog,
  moderateJob,
  reactivateUser,
  setReportStatus,
  suspendUser,
  type ModerationStatusValue,
  type ReportStatusValue,
} from "../api/admin";
import { getAdminFeedback, setFeedbackStatus, type FeedbackStatusValue } from "../api/feedback";
import type { AdminCompany, AdminJob, AdminUser, AuditLogEntry, FeedbackSubmission, Report } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { Badge, StatusBadge, type BadgeTone } from "../components/ui/Badge";
import Modal from "../components/ui/Modal";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";
import ConfirmDialog from "../components/ui/ConfirmDialog";

type Tab = "users" | "companies" | "jobs" | "reports" | "feedback" | "audit";

const REPORT_STATUS_TONES: Record<string, BadgeTone> = {
  Open: "warning",
  UnderReview: "info",
  Resolved: "success",
  Dismissed: "neutral",
};

const FEEDBACK_STATUS_TONES: Record<string, BadgeTone> = {
  New: "warning",
  InProgress: "info",
  Resolved: "success",
};

const TABS: { key: Tab; label: string }[] = [
  { key: "users", label: "Users" },
  { key: "companies", label: "Companies" },
  { key: "jobs", label: "Jobs" },
  { key: "reports", label: "Reports" },
  { key: "feedback", label: "Feedback" },
  { key: "audit", label: "Audit Log" },
];

export default function AdminPage() {
  const toast = useToast();
  const [tab, setTab] = useState<Tab>("users");
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [jobs, setJobs] = useState<AdminJob[]>([]);
  const [reports, setReports] = useState<Report[]>([]);
  const [feedback, setFeedback] = useState<FeedbackSubmission[]>([]);
  const [auditLog, setAuditLog] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [suspendTarget, setSuspendTarget] = useState<AdminUser | null>(null);
  const [noteTarget, setNoteTarget] = useState<Report | null>(null);
  const [noteText, setNoteText] = useState("");
  const [noteError, setNoteError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    const load =
      tab === "users" ? getAdminUsers().then(setUsers) :
      tab === "companies" ? getAdminCompanies().then(setCompanies) :
      tab === "jobs" ? getAdminJobs().then(setJobs) :
      tab === "reports" ? getAdminReports().then(setReports) :
      tab === "feedback" ? getAdminFeedback().then(setFeedback) :
      getAuditLog().then(setAuditLog);

    load
      .catch((err) => setError(getErrorMessage(err, "Failed to load admin data")))
      .finally(() => setLoading(false));
  }, [tab]);

  async function handleModerate(jobId: number, status: ModerationStatusValue) {
    try {
      const updated = await moderateJob(jobId, status);
      setJobs((prev) => prev.map((j) => (j.id === updated.id ? updated : j)));
      toast.success(`Job ${status.toLowerCase()}.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to moderate job"));
    }
  }

  async function handleSetReportStatus(reportId: number, status: ReportStatusValue) {
    try {
      await setReportStatus(reportId, status);
      setReports((prev) => prev.map((r) => (r.id === reportId ? { ...r, status } : r)));
      toast.success(`Report marked ${status.toLowerCase()}.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update report"));
    }
  }

  function openNoteModal(report: Report) {
    setNoteTarget(report);
    setNoteText(report.moderationNote ?? "");
    setNoteError(null);
  }

  async function handleSaveNote() {
    if (!noteTarget) return;
    if (!noteText.trim()) {
      setNoteError("Note cannot be empty.");
      return;
    }
    try {
      await addReportNote(noteTarget.id, noteText.trim());
      setReports((prev) => prev.map((r) => (r.id === noteTarget.id ? { ...r, moderationNote: noteText.trim() } : r)));
      toast.success("Note saved.");
      setNoteTarget(null);
    } catch (err) {
      setNoteError(getErrorMessage(err, "Failed to save note"));
    }
  }

  async function handleSuspendConfirm() {
    if (!suspendTarget) return;
    try {
      await suspendUser(suspendTarget.id);
      setUsers((prev) => prev.map((u) => (u.id === suspendTarget.id ? { ...u, isActive: false } : u)));
      toast.success(`${suspendTarget.fullName} suspended.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to suspend user"));
    } finally {
      setSuspendTarget(null);
    }
  }

  async function handleSetFeedbackStatus(id: number, status: FeedbackStatusValue) {
    try {
      await setFeedbackStatus(id, status);
      setFeedback((prev) => prev.map((f) => (f.id === id ? { ...f, status } : f)));
      toast.success(`Feedback marked ${status === "InProgress" ? "in progress" : status.toLowerCase()}.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update feedback status"));
    }
  }

  async function handleReactivate(user: AdminUser) {
    try {
      await reactivateUser(user.id);
      setUsers((prev) => prev.map((u) => (u.id === user.id ? { ...u, isActive: true } : u)));
      toast.success(`${user.fullName} reactivated.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to reactivate user"));
    }
  }

  return (
    <div>
      <PageHeader
        title={<><ShieldCheck size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Admin</>}
        subtitle="Manage users, companies, job moderation, reports, feedback, and the audit trail."
      />

      <div className="admin-tabs" role="tablist">
        {TABS.map((t) => (
          <button
            key={t.key}
            type="button"
            role="tab"
            aria-selected={tab === t.key}
            className={`admin-tab ${tab === t.key ? "admin-tab-active" : ""}`}
            onClick={() => setTab(t.key)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}
      {loading ? (
        <p>Loading...</p>
      ) : (
        <div className="table-scroll">
          {tab === "users" && (
            <table className="dashboard-table">
              <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Active</th><th>Joined</th><th>Actions</th></tr></thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id}>
                    <td>{u.fullName}</td>
                    <td>{u.email}</td>
                    <td>{u.role}</td>
                    <td><Badge tone={u.isActive ? "success" : "danger"}>{u.isActive ? "Active" : "Suspended"}</Badge></td>
                    <td>{new Date(u.createdAt).toLocaleDateString()}</td>
                    <td>
                      {u.role !== "Admin" && (
                        u.isActive ? (
                          <button type="button" className="link-button" onClick={() => setSuspendTarget(u)}>Suspend</button>
                        ) : (
                          <button type="button" className="link-button" onClick={() => handleReactivate(u)}>Reactivate</button>
                        )
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {tab === "companies" && (
            <table className="dashboard-table">
              <thead><tr><th>Name</th><th>Industry</th><th>Jobs</th><th>Recruiters</th><th>Created</th></tr></thead>
              <tbody>
                {companies.map((c) => (
                  <tr key={c.id}>
                    <td>{c.name}</td>
                    <td>{c.industry ?? "—"}</td>
                    <td>{c.jobCount}</td>
                    <td>{c.recruiterCount}</td>
                    <td>{new Date(c.createdAt).toLocaleDateString()}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {tab === "jobs" && (
            <table className="dashboard-table">
              <thead><tr><th>Title</th><th>Company</th><th>Status</th><th>Moderation</th><th>Applications</th><th>Actions</th></tr></thead>
              <tbody>
                {jobs.map((j) => (
                  <tr key={j.id}>
                    <td>{j.title}</td>
                    <td>{j.companyName}</td>
                    <td><StatusBadge status={j.status} /></td>
                    <td><StatusBadge status={j.moderationStatus} /></td>
                    <td>{j.applicationCount}</td>
                    <td style={{ display: "flex", gap: "0.5rem" }}>
                      {j.moderationStatus !== "Approved" && (
                        <button type="button" className="link-button" onClick={() => handleModerate(j.id, "Approved")}>Approve</button>
                      )}
                      {j.moderationStatus !== "Hidden" && (
                        <button type="button" className="link-button" onClick={() => handleModerate(j.id, "Hidden")}>Hide</button>
                      )}
                      {j.moderationStatus !== "Removed" && (
                        <button type="button" className="link-button" onClick={() => handleModerate(j.id, "Removed")}>Remove</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {tab === "reports" && (
            <table className="dashboard-table">
              <thead><tr><th>Reported entity</th><th>Reported by</th><th>Reason</th><th>Details</th><th>Status</th><th>Note</th><th>Actions</th></tr></thead>
              <tbody>
                {reports.map((r) => (
                  <tr key={r.id}>
                    <td>{r.entityType} — {r.entityLabel ?? `#${r.entityId}`}</td>
                    <td>{r.reportedByName}</td>
                    <td>{r.reason}</td>
                    <td>{r.details ?? "—"}</td>
                    <td><Badge tone={REPORT_STATUS_TONES[r.status] ?? "neutral"}>{r.status}</Badge></td>
                    <td>{r.moderationNote ?? "—"}</td>
                    <td style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
                      {r.status === "Open" && (
                        <button type="button" className="link-button" onClick={() => handleSetReportStatus(r.id, "UnderReview")}>Start review</button>
                      )}
                      {r.status !== "Resolved" && (
                        <button type="button" className="link-button" onClick={() => handleSetReportStatus(r.id, "Resolved")}>Resolve</button>
                      )}
                      {r.status !== "Dismissed" && (
                        <button type="button" className="link-button" onClick={() => handleSetReportStatus(r.id, "Dismissed")}>Dismiss</button>
                      )}
                      <button type="button" className="link-button" onClick={() => openNoteModal(r)}>Add note</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {tab === "feedback" && (
            <table className="dashboard-table">
              <thead><tr><th>From</th><th>Category</th><th>Message</th><th>Status</th><th>Submitted</th><th>Actions</th></tr></thead>
              <tbody>
                {feedback.map((f) => (
                  <tr key={f.id}>
                    <td>{f.name} {f.submittedByName && <span className="hint">({f.submittedByName})</span>}<br /><span className="hint">{f.email}</span></td>
                    <td>{f.category}</td>
                    <td style={{ maxWidth: 320 }}>{f.message}</td>
                    <td><Badge tone={FEEDBACK_STATUS_TONES[f.status] ?? "neutral"}>{f.status}</Badge></td>
                    <td>{new Date(f.createdAt).toLocaleDateString()}</td>
                    <td style={{ display: "flex", gap: "0.5rem", flexWrap: "wrap" }}>
                      {f.status !== "InProgress" && (
                        <button type="button" className="link-button" onClick={() => handleSetFeedbackStatus(f.id, "InProgress")}>In progress</button>
                      )}
                      {f.status !== "Resolved" && (
                        <button type="button" className="link-button" onClick={() => handleSetFeedbackStatus(f.id, "Resolved")}>Resolve</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}

          {tab === "audit" && (
            <table className="dashboard-table">
              <thead><tr><th>When</th><th>Actor</th><th>Action</th><th>Entity</th></tr></thead>
              <tbody>
                {auditLog.map((entry) => (
                  <tr key={entry.id}>
                    <td>{new Date(entry.timestampUtc).toLocaleString()}</td>
                    <td>{entry.actorName ?? "System"} {entry.actorRole ? `(${entry.actorRole})` : ""}</td>
                    <td>{entry.actionType}</td>
                    <td>{entry.entityType} #{entry.entityId ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      <ConfirmDialog
        open={suspendTarget !== null}
        onCancel={() => setSuspendTarget(null)}
        onConfirm={handleSuspendConfirm}
        title="Suspend user"
        confirmLabel="Suspend"
        danger
      >
        <p>
          Suspend <strong>{suspendTarget?.fullName}</strong>? They will be immediately signed out and unable to log in
          or use the platform until reactivated.
        </p>
      </ConfirmDialog>

      <Modal
        open={noteTarget !== null}
        onClose={() => setNoteTarget(null)}
        title="Moderation note"
        footer={
          <>
            <button type="button" className="btn btn-secondary" onClick={() => setNoteTarget(null)}>Cancel</button>
            <button type="button" className="btn btn-primary" onClick={handleSaveNote}>Save note</button>
          </>
        }
      >
        <FormField label="Internal note" htmlFor="moderation-note" error={noteError ?? undefined}>
          <textarea
            id="moderation-note"
            rows={4}
            value={noteText}
            onChange={(e) => setNoteText(e.target.value)}
          />
        </FormField>
      </Modal>
    </div>
  );
}
