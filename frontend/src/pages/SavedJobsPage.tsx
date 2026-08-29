import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Bookmark } from "lucide-react";
import { getSavedJobs, unsaveJob } from "../api/savedJobs";
import type { SavedJobEntry } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { formatExperienceRange, formatRelativeTime, formatSalaryRange } from "../utils/format";
import EmptyState from "../components/ui/EmptyState";

export default function SavedJobsPage() {
  const toast = useToast();
  const [entries, setEntries] = useState<SavedJobEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [removingId, setRemovingId] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setEntries(await getSavedJobs());
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load saved jobs"));
    } finally {
      setLoading(false);
    }
  }

  async function handleRemove(jobId: number) {
    setRemovingId(jobId);
    try {
      await unsaveJob(jobId);
      setEntries((prev) => prev.filter((e) => e.job.id !== jobId));
      toast.success("Removed from saved jobs.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to remove saved job"));
    } finally {
      setRemovingId(null);
    }
  }

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <div className="page-header">
        <h1><Bookmark size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Saved Jobs</h1>
        <p>Roles you've bookmarked for later.</p>
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      {entries.length === 0 ? (
        <EmptyState
          icon={<Bookmark size={32} />}
          title="No saved jobs yet"
          description="Save a role from the job listings to find it here later."
          action={<Link to="/jobs" className="btn btn-primary">Browse open roles</Link>}
        />
      ) : (
        <ul className="job-list">
          {entries.map(({ job, savedAt }) => (
            <li key={job.id} className="job-card">
              <Link to={`/jobs/${job.id}`} className="job-card-title">
                {job.title}
              </Link>
              <p className="job-card-company">{job.companyName}</p>
              <div className="job-card-meta">
                <span>{job.displayLocation}</span>
                <span>{formatExperienceRange(job.minExperienceYears, job.maxExperienceYears)}</span>
                <span>{formatSalaryRange(job.minSalary, job.maxSalary)}</span>
              </div>
              <p className="hint" style={{ marginTop: "0.5rem" }}>Saved {formatRelativeTime(savedAt)}</p>
              <div className="job-card-actions">
                <button
                  type="button"
                  className="link-button"
                  disabled={removingId === job.id}
                  onClick={() => handleRemove(job.id)}
                >
                  {removingId === job.id ? "Removing..." : "Remove"}
                </button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
