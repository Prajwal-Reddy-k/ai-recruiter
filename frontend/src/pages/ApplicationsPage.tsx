import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { ClipboardList, Search } from "lucide-react";
import { getMyApplications, withdrawApplication } from "../api/applications";
import type { JobApplication } from "../types";
import { getErrorCode, getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import ConfirmDialog from "../components/ui/ConfirmDialog";
import FilterChip from "../components/ui/FilterChip";

const TERMINAL_STATUSES = new Set(["Withdrawn", "Rejected", "Hired"]);

const SORT_OPTIONS = [
  { value: "activity", label: "Latest activity" },
  { value: "applied", label: "Application date" },
  { value: "interview", label: "Interview date" },
] as const;

type SortOption = (typeof SORT_OPTIONS)[number]["value"];

export default function ApplicationsPage() {
  const toast = useToast();
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [withdrawingId, setWithdrawingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [confirmTarget, setConfirmTarget] = useState<JobApplication | null>(null);

  const [statusFilter, setStatusFilter] = useState("");
  const [companyFilter, setCompanyFilter] = useState("");
  const [locationFilter, setLocationFilter] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [sort, setSort] = useState<SortOption>("activity");

  useEffect(() => {
    getMyApplications()
      .then(setApplications)
      .catch((err) => setError(getErrorMessage(err, "Failed to load your applications")))
      .finally(() => setLoading(false));
  }, []);

  const statusOptions = useMemo(
    () => Array.from(new Set(applications.map((a) => a.status))).sort(),
    [applications],
  );

  const filtered = useMemo(() => {
    let list = applications;
    if (statusFilter) list = list.filter((a) => a.status === statusFilter);
    if (companyFilter.trim()) {
      const q = companyFilter.trim().toLowerCase();
      list = list.filter((a) => a.companyName.toLowerCase().includes(q));
    }
    if (locationFilter.trim()) {
      const q = locationFilter.trim().toLowerCase();
      list = list.filter((a) => (a.jobLocation ?? "").toLowerCase().includes(q));
    }
    if (fromDate) {
      const from = new Date(fromDate).getTime();
      list = list.filter((a) => new Date(a.createdAt).getTime() >= from);
    }
    if (toDate) {
      const to = new Date(toDate).getTime() + 24 * 60 * 60 * 1000 - 1;
      list = list.filter((a) => new Date(a.createdAt).getTime() <= to);
    }

    const sorted = [...list];
    sorted.sort((a, b) => {
      if (sort === "applied") {
        return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
      }
      if (sort === "interview") {
        const aTime = a.nextInterviewAtUtc ? new Date(a.nextInterviewAtUtc).getTime() : -Infinity;
        const bTime = b.nextInterviewAtUtc ? new Date(b.nextInterviewAtUtc).getTime() : -Infinity;
        return bTime - aTime;
      }
      const aActivity = new Date(a.updatedAt ?? a.createdAt).getTime();
      const bActivity = new Date(b.updatedAt ?? b.createdAt).getTime();
      return bActivity - aActivity;
    });
    return sorted;
  }, [applications, statusFilter, companyFilter, locationFilter, fromDate, toDate, sort]);

  const activeChips = [
    statusFilter && { key: "status", label: `Status: ${statusFilter}`, onRemove: () => setStatusFilter("") },
    companyFilter.trim() && { key: "company", label: `Company: ${companyFilter}`, onRemove: () => setCompanyFilter("") },
    locationFilter.trim() && { key: "location", label: `Location: ${locationFilter}`, onRemove: () => setLocationFilter("") },
    fromDate && { key: "from", label: `From ${fromDate}`, onRemove: () => setFromDate("") },
    toDate && { key: "to", label: `To ${toDate}`, onRemove: () => setToDate("") },
  ].filter(Boolean) as { key: string; label: string; onRemove: () => void }[];

  function clearFilters() {
    setStatusFilter("");
    setCompanyFilter("");
    setLocationFilter("");
    setFromDate("");
    setToDate("");
  }

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
      if (getErrorCode(err) === "APPLICATION_FINAL") {
        toast.error("This application has already reached a final status and can no longer be withdrawn.");
      } else {
        setError(getErrorMessage(err, "Failed to withdraw application"));
      }
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
        <>
          <div className="ui-card ui-card-padded" style={{ marginBottom: "1.5rem" }}>
            <div className="form-row">
              <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">Any status</option>
                {statusOptions.map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
              <input
                placeholder="Company"
                value={companyFilter}
                onChange={(e) => setCompanyFilter(e.target.value)}
              />
              <input
                placeholder="Location"
                value={locationFilter}
                onChange={(e) => setLocationFilter(e.target.value)}
              />
            </div>
            <div className="form-row">
              <label className="form-field">
                <span className="form-field-label">From</span>
                <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
              </label>
              <label className="form-field">
                <span className="form-field-label">To</span>
                <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
              </label>
              <select value={sort} onChange={(e) => setSort(e.target.value as SortOption)}>
                {SORT_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>Sort: {opt.label}</option>)}
              </select>
            </div>
          </div>

          {activeChips.length > 0 && (
            <div className="active-filters-row" style={{ marginBottom: "1rem" }}>
              {activeChips.map((chip) => (
                <FilterChip key={chip.key} label={chip.label} onRemove={chip.onRemove} />
              ))}
              <button type="button" className="link-button" onClick={clearFilters}>Clear all filters</button>
            </div>
          )}

          {filtered.length === 0 ? (
            <EmptyState
              icon={<Search size={28} />}
              title="No applications match these filters"
              description="Try widening your filters, or browse recommended roles."
              action={<Link to="/jobs" className="btn btn-secondary btn-sm">Browse open roles</Link>}
            />
          ) : (
            <ul className="job-list">
              {filtered.map((app) => (
                <li key={app.id} className="job-card">
                  <Link to={`/applications/${app.id}`} className="job-card-title">
                    {app.jobTitle}
                  </Link>
                  <p className="job-card-company">{app.companyName}</p>
                  <p className="job-card-meta">
                    <StatusBadge status={app.status} />
                    <span>Applied {new Date(app.createdAt).toLocaleDateString()}</span>
                    {app.jobLocation && <span>{app.jobLocation}</span>}
                    {app.matchScore !== null && <span>Match score: {app.matchScore}/100</span>}
                    {app.nextInterviewAtUtc && <span>Interview: {new Date(app.nextInterviewAtUtc).toLocaleString()}</span>}
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
        </>
      )}

      <ConfirmDialog
        open={confirmTarget !== null}
        title="Withdraw application"
        confirmLabel="Withdraw"
        danger
        loading={withdrawingId !== null}
        onConfirm={handleWithdraw}
        onCancel={() => setConfirmTarget(null)}
      >
        Withdraw your application for "{confirmTarget?.jobTitle}"? This can't be undone.
      </ConfirmDialog>
    </div>
  );
}
