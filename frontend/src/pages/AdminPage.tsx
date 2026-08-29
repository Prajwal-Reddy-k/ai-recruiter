import { useEffect, useState } from "react";
import { ShieldCheck } from "lucide-react";
import {
  getAdminCompanies,
  getAdminJobs,
  getAdminReports,
  getAdminUsers,
  getAuditLog,
  moderateJob,
  resolveReport,
  type ModerationStatusValue,
} from "../api/admin";
import type { AdminCompany, AdminJob, AdminUser, AuditLogEntry, JobReport } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";

type Tab = "users" | "companies" | "jobs" | "reports" | "audit";

const TABS: { key: Tab; label: string }[] = [
  { key: "users", label: "Users" },
  { key: "companies", label: "Companies" },
  { key: "jobs", label: "Jobs" },
  { key: "reports", label: "Reports" },
  { key: "audit", label: "Audit Log" },
];

export default function AdminPage() {
  const toast = useToast();
  const [tab, setTab] = useState<Tab>("users");
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [companies, setCompanies] = useState<AdminCompany[]>([]);
  const [jobs, setJobs] = useState<AdminJob[]>([]);
  const [reports, setReports] = useState<JobReport[]>([]);
  const [auditLog, setAuditLog] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setLoading(true);
    setError(null);
    const load =
      tab === "users" ? getAdminUsers().then(setUsers) :
      tab === "companies" ? getAdminCompanies().then(setCompanies) :
      tab === "jobs" ? getAdminJobs().then(setJobs) :
      tab === "reports" ? getAdminReports().then(setReports) :
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

  async function handleResolveReport(reportId: number) {
    try {
      await resolveReport(reportId, "Reviewed");
      setReports((prev) => prev.map((r) => (r.id === reportId ? { ...r, status: "Reviewed" } : r)));
      toast.success("Report marked reviewed.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to resolve report"));
    }
  }

  async function handleDismissReport(reportId: number) {
    try {
      await resolveReport(reportId, "Dismissed");
      setReports((prev) => prev.map((r) => (r.id === reportId ? { ...r, status: "Dismissed" } : r)));
      toast.success("Report dismissed.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to dismiss report"));
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1><ShieldCheck size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Admin</h1>
        <p>Manage users, companies, job moderation, reports, and the audit trail.</p>
      </div>

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
              <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Active</th><th>Joined</th></tr></thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id}>
                    <td>{u.fullName}</td>
                    <td>{u.email}</td>
                    <td>{u.role}</td>
                    <td>{u.isActive ? "Yes" : "No"}</td>
                    <td>{new Date(u.createdAt).toLocaleDateString()}</td>
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
              <thead><tr><th>Job</th><th>Reported by</th><th>Reason</th><th>Status</th><th>Actions</th></tr></thead>
              <tbody>
                {reports.map((r) => (
                  <tr key={r.id}>
                    <td>{r.jobTitle}</td>
                    <td>{r.reportedByName}</td>
                    <td>{r.reason}</td>
                    <td><StatusBadge status={r.status} /></td>
                    <td style={{ display: "flex", gap: "0.5rem" }}>
                      {r.status === "Pending" && (
                        <>
                          <button type="button" className="link-button" onClick={() => handleResolveReport(r.id)}>Mark reviewed</button>
                          <button type="button" className="link-button" onClick={() => handleDismissReport(r.id)}>Dismiss</button>
                        </>
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
    </div>
  );
}
