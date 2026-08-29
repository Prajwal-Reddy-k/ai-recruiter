import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { ClipboardList } from "lucide-react";
import { getMyApplications, withdrawApplication } from "../api/applications";
import type { JobApplication } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import Modal from "../components/ui/Modal";
import Button from "../components/ui/Button";

const TERMINAL_STATUSES = new Set(["Withdrawn", "Rejected", "Hired"]);

export default function ApplicationsPage() {
  const toast = useToast();
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [withdrawingId, setWithdrawingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<JobApplication | null>(null);

  useEffect(() => {
    getMyApplications()
      .then(setApplications)
      .finally(() => setLoading(false));
  }, []);

  async function handleWithdraw() {
    if (!confirmTarget) return;
    const app = confirmTarget;
    setConfirmTarget(null);

    setError(null);
    setWithdrawingId(app.id);
    try {
      const updated = await withdrawApplication(app.id);
      setApplications((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      toast.success("Application withdrawn.");
    } catch (err) {
      setError(getErrorMessage(err, "Failed to withdraw application"));
    } finally {
      setWithdrawingId(null);
    }
  }

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <div className="page-header">
        <h1>My Applications</h1>
        <p>Track the status of every role you've applied to.</p>
      </div>
      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}
      {applications.length === 0 ? (
        <EmptyState
          icon={<ClipboardList size={32} />}
          title="No applications yet"
          description="Once you apply to a role, you can track its status here."
          action={<Link to="/jobs" className="btn btn-primary">Browse open roles</Link>}
        />
      ) : (
        <ul className="job-list">
          {applications.map((app) => (
            <li key={app.id} className="job-card">
              <Link to={`/applications/${app.id}`} className="job-card-title">
                {app.jobTitle}
              </Link>
              <p className="job-card-company">{app.companyName}</p>
              <p className="job-card-meta">
                <StatusBadge status={app.status} />
                <span>Applied {new Date(app.createdAt).toLocaleDateString()}</span>
                {app.matchScore !== null && <span>Match score: {app.matchScore}/100</span>}
              </p>
              {!TERMINAL_STATUSES.has(app.status) && (
                <button
                  type="button"
                  className="link-button"
                  style={{ marginTop: "0.75rem" }}
                  disabled={withdrawingId === app.id}
                  onClick={() => setConfirmTarget(app)}
                >
                  {withdrawingId === app.id ? "Withdrawing..." : "Withdraw application"}
                </button>
              )}
            </li>
          ))}
        </ul>
      )}

      <Modal
        open={confirmTarget !== null}
        onClose={() => setConfirmTarget(null)}
        title="Withdraw application"
        footer={
          <>
            <Button variant="secondary" onClick={() => setConfirmTarget(null)}>Cancel</Button>
            <Button variant="danger" onClick={handleWithdraw}>Withdraw</Button>
          </>
        }
      >
        Withdraw your application for "{confirmTarget?.jobTitle}"? This can't be undone.
      </Modal>
    </div>
  );
}
