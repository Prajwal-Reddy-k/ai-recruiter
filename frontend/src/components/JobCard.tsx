import { useState } from "react";
import { Link } from "react-router-dom";
import { Bookmark, BookmarkCheck, Briefcase, Clock, MapPin, Wallet } from "lucide-react";
import type { JobPosting } from "../types";
import { formatExperienceRange, formatRelativeTime, formatSalaryRange } from "../utils/format";
import { saveJob, unsaveJob } from "../api/savedJobs";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";

export default function JobCard({
  job,
  compact = false,
  initiallySaved = false,
  compareChecked,
  onToggleCompare,
}: {
  job: JobPosting;
  compact?: boolean;
  initiallySaved?: boolean;
  /** Undefined hides the compare checkbox entirely — only JobsPage's main results list
   * passes these, so other JobCard call sites (dashboards, saved jobs, similar jobs) are
   * unaffected by default. */
  compareChecked?: boolean;
  onToggleCompare?: (jobId: number) => void;
}) {
  const { user } = useAuth();
  const { error: showError } = useToast();
  const [saved, setSaved] = useState(initiallySaved);
  const [pending, setPending] = useState(false);
  const skills = (job.requiredSkillsCsv ?? "")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean)
    .slice(0, compact ? 3 : 5);

  async function handleToggleSave() {
    if (pending) return;
    setPending(true);
    try {
      if (saved) {
        await unsaveJob(job.id);
        setSaved(false);
      } else {
        await saveJob(job.id);
        setSaved(true);
      }
    } catch {
      showError("Couldn't update saved jobs. Please try again.");
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="job-card">
      <div className="job-card-top">
        <div>
          <Link to={`/jobs/${job.id}`} className="job-card-title">
            {job.title}
          </Link>
          <p className="job-card-company">{job.companyName}</p>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
          {onToggleCompare && (
            <label className="filter-option" style={{ padding: 0 }}>
              <input
                type="checkbox"
                checked={compareChecked ?? false}
                onChange={() => onToggleCompare(job.id)}
                aria-label={`Compare ${job.title}`}
              />
              Compare
            </label>
          )}
          {!compact && user?.role === "Candidate" && (
            <button
              type="button"
              className={`save-btn ${saved ? "save-btn-active" : ""}`}
              onClick={handleToggleSave}
              disabled={pending}
              aria-pressed={saved}
              aria-label={saved ? "Remove from saved jobs" : "Save job"}
            >
              {saved ? <BookmarkCheck size={17} /> : <Bookmark size={17} />}
            </button>
          )}
        </div>
      </div>

      <div className="job-card-meta">
        <span><MapPin size={14} /> {job.displayLocation}</span>
        <span><Briefcase size={14} /> {formatExperienceRange(job.minExperienceYears, job.maxExperienceYears)}</span>
        <span><Wallet size={14} /> {formatSalaryRange(job.minSalary, job.maxSalary)}</span>
        <span><Clock size={14} /> {formatRelativeTime(job.createdAt)}</span>
      </div>

      {skills.length > 0 && (
        <div className="job-card-footer">
          <div className="chip-list">
            {skills.map((skill) => (
              <span key={skill} className="chip">{skill}</span>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
