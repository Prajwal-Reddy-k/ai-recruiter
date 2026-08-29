import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Bookmark, BookmarkCheck, Briefcase, Clock, MapPin, Wallet } from "lucide-react";
import { getJobById, getOpenJobs, reportJob } from "../api/jobs";
import { applyToJob } from "../api/applications";
import { getSavedJobs, saveJob, unsaveJob } from "../api/savedJobs";
import type { JobPosting } from "../types";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { getErrorMessage } from "../utils/errors";
import { formatExperienceRange, formatRelativeTime, formatSalaryRange } from "../utils/format";
import { findSimilarJobs } from "../utils/jobFilters";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Modal from "../components/ui/Modal";
import FormField from "../components/ui/FormField";
import JobCard from "../components/JobCard";

type ApplyState = "idle" | "applying" | "applied" | "error";

export default function JobDetailPage() {
  const { id } = useParams();
  const { isAuthenticated, user } = useAuth();
  const toast = useToast();
  const [job, setJob] = useState<JobPosting | null>(null);
  const [similarJobs, setSimilarJobs] = useState<JobPosting[]>([]);
  const [loading, setLoading] = useState(true);
  const [applyState, setApplyState] = useState<ApplyState>("idle");
  const [applyError, setApplyError] = useState<string | null>(null);
  const [reportOpen, setReportOpen] = useState(false);
  const [reportReason, setReportReason] = useState("");
  const [reportSubmitting, setReportSubmitting] = useState(false);
  const [reportSubmitted, setReportSubmitted] = useState(false);
  const [saved, setSaved] = useState(false);
  const [savePending, setSavePending] = useState(false);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setApplyState("idle");
    Promise.all([getJobById(Number(id)), getOpenJobs()])
      .then(([jobData, allJobs]) => {
        setJob(jobData);
        setSimilarJobs(findSimilarJobs(jobData, allJobs));
      })
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    if (!id || !isAuthenticated || user?.role !== "Candidate") return;
    getSavedJobs()
      .then((entries) => setSaved(entries.some((e) => e.job.id === Number(id))))
      .catch(() => setSaved(false));
  }, [id, isAuthenticated, user]);

  async function handleToggleSave() {
    if (!job || savePending) return;
    setSavePending(true);
    try {
      if (saved) {
        await unsaveJob(job.id);
        setSaved(false);
      } else {
        await saveJob(job.id);
        setSaved(true);
      }
    } catch (err) {
      toast.error(getErrorMessage(err, "Couldn't update saved jobs"));
    } finally {
      setSavePending(false);
    }
  }

  async function handleApply() {
    if (!job) return;
    setApplyState("applying");
    setApplyError(null);
    try {
      await applyToJob(job.id);
      setApplyState("applied");
      toast.success("Application submitted.");
    } catch (err) {
      setApplyError(getErrorMessage(err, "Failed to apply"));
      setApplyState("error");
    }
  }

  async function handleSubmitReport() {
    if (!job || !reportReason.trim()) return;
    setReportSubmitting(true);
    try {
      await reportJob(job.id, reportReason.trim());
      setReportSubmitted(true);
      toast.success("Report submitted. Our moderation team will review it.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to submit report"));
    } finally {
      setReportSubmitting(false);
    }
  }

  if (loading) return <p>Loading...</p>;
  if (!job) return <p>Job not found.</p>;

  const skills = (job.requiredSkillsCsv ?? "").split(",").map((s) => s.trim()).filter(Boolean);

  return (
    <div className="job-detail-layout">
      <div className="job-detail-main">
        <div className="job-detail-header">
          <h1>{job.title}</h1>
          <p className="job-detail-company">
            <Link to={`/companies/${job.companyId}`}>{job.companyName}</Link>
          </p>
        </div>

        <div className="job-detail-facts">
          <span className="job-detail-fact"><MapPin size={16} /> {job.displayLocation}</span>
          <span className="job-detail-fact"><Briefcase size={16} /> {formatExperienceRange(job.minExperienceYears, job.maxExperienceYears)}</span>
          <span className="job-detail-fact"><Wallet size={16} /> {formatSalaryRange(job.minSalary, job.maxSalary)}</span>
          <span className="job-detail-fact"><Clock size={16} /> Posted {formatRelativeTime(job.createdAt)}</span>
        </div>

        {skills.length > 0 && (
          <div className="chip-list" style={{ marginBottom: "1.5rem" }}>
            {skills.map((skill) => (
              <span key={skill} className="chip">{skill}</span>
            ))}
          </div>
        )}

        <h3 style={{ marginBottom: "0.75rem" }}>About this role</h3>
        <p className="job-detail-body">{job.description}</p>
      </div>

      <div className="job-detail-sidebar">
        <Card>
          <h3 style={{ marginBottom: "1rem" }}>Job summary</h3>
          <div className="job-detail-facts" style={{ flexDirection: "column", alignItems: "flex-start", gap: "0.75rem", border: "none", padding: 0, margin: "0 0 1.25rem" }}>
            <span className="job-detail-fact"><MapPin size={16} /> {job.displayLocation}</span>
            <span className="job-detail-fact"><Briefcase size={16} /> {formatExperienceRange(job.minExperienceYears, job.maxExperienceYears)}</span>
            <span className="job-detail-fact"><Wallet size={16} /> {formatSalaryRange(job.minSalary, job.maxSalary)}</span>
          </div>

          {isAuthenticated && user?.role === "Candidate" ? (
            applyState === "applied" ? (
              <p className="success">
                Applied! <Link to="/applications">View your applications</Link>.
              </p>
            ) : (
              <>
                <Button onClick={handleApply} loading={applyState === "applying"} fullWidth>
                  Apply to this job
                </Button>
                {applyState === "error" && <p className="error" style={{ marginTop: "0.75rem" }}>{applyError}</p>}
              </>
            )
          ) : !isAuthenticated ? (
            <p className="hint">
              <Link to="/login">Sign in</Link> as a candidate to apply.
            </p>
          ) : (
            <p className="hint">Only candidates can apply to jobs.</p>
          )}

          {isAuthenticated && user?.role === "Candidate" && (
            <Button
              variant="secondary"
              icon={saved ? <BookmarkCheck size={16} /> : <Bookmark size={16} />}
              onClick={handleToggleSave}
              loading={savePending}
              fullWidth
              style={{ marginTop: "0.75rem" }}
            >
              {saved ? "Saved" : "Save job"}
            </Button>
          )}

          {isAuthenticated && (
            <button
              type="button"
              className="link-button"
              style={{ marginTop: "1rem" }}
              onClick={() => setReportOpen(true)}
            >
              Report this job
            </button>
          )}
        </Card>

        {similarJobs.length > 0 && (
          <Card>
            <h3 style={{ marginBottom: "1rem" }}>Similar jobs</h3>
            <div className="similar-jobs-list">
              {similarJobs.map((similar) => (
                <JobCard key={similar.id} job={similar} compact />
              ))}
            </div>
          </Card>
        )}
      </div>

      <Modal
        open={reportOpen}
        onClose={() => {
          setReportOpen(false);
          setReportReason("");
          setReportSubmitted(false);
        }}
        title="Report this job"
        footer={
          !reportSubmitted && (
            <>
              <Button variant="secondary" onClick={() => setReportOpen(false)}>Cancel</Button>
              <Button onClick={handleSubmitReport} loading={reportSubmitting} disabled={!reportReason.trim()}>
                Submit report
              </Button>
            </>
          )
        }
      >
        {reportSubmitted ? (
          <p className="success">Thanks — your report has been submitted for review.</p>
        ) : (
          <FormField label="Reason" htmlFor="report-reason" required>
            <textarea
              id="report-reason"
              rows={4}
              value={reportReason}
              onChange={(e) => setReportReason(e.target.value)}
              placeholder="Tell us what's wrong with this listing..."
            />
          </FormField>
        )}
      </Modal>
    </div>
  );
}
