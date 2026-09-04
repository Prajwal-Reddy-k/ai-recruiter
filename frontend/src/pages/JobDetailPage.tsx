import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Bookmark, BookmarkCheck, Briefcase, Clock, Heart, MapPin, Wallet } from "lucide-react";
import { getJobById, getOpenJobs, reportJob } from "../api/jobs";
import { REPORT_REASON_LABELS, type ReportReasonValue } from "../api/moderationReports";
import { applyToJob } from "../api/applications";
import { getSavedJobs, saveJob, unsaveJob } from "../api/savedJobs";
import { getMyCoverLetterTemplates } from "../api/coverLetterTemplates";
import { getMyCandidateProfile } from "../api/candidates";
import { followCompany, getFollowedCompanies, unfollowCompany } from "../api/follows";
import type { CandidateProfile, CoverLetterTemplate, JobPosting } from "../types";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { getErrorMessage } from "../utils/errors";
import { formatExperienceRange, formatRelativeTime, formatSalaryRange } from "../utils/format";
import { findSimilarJobs } from "../utils/jobFilters";
import { buildCoverLetterDraft } from "../utils/coverLetterMerge";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import Modal from "../components/ui/Modal";
import FormField from "../components/ui/FormField";
import JobCard from "../components/JobCard";
import CreateReferralModal from "../components/CreateReferralModal";
import VerifiedBadge from "../components/VerifiedBadge";
import ShareMenu from "../components/ShareMenu";
import ScreeningQuestionsForm, { buildScreeningAnswerRequests, type ScreeningAnswerValue } from "../components/ScreeningQuestionsForm";
import { getFieldErrors } from "../utils/errors";

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
  const [reportReason, setReportReason] = useState<ReportReasonValue | "">("");
  const [reportDetails, setReportDetails] = useState("");
  const [reportSubmitting, setReportSubmitting] = useState(false);
  const [reportSubmitted, setReportSubmitted] = useState(false);
  const [saved, setSaved] = useState(false);
  const [savePending, setSavePending] = useState(false);
  const [followingCompany, setFollowingCompany] = useState(false);
  const [followPending, setFollowPending] = useState(false);

  const [applyModalOpen, setApplyModalOpen] = useState(false);
  const [referralModalOpen, setReferralModalOpen] = useState(false);
  const [templates, setTemplates] = useState<CoverLetterTemplate[]>([]);
  const [profile, setProfile] = useState<CandidateProfile | null>(null);
  const [selectedTemplateId, setSelectedTemplateId] = useState<number | "">("");
  const [coverLetterText, setCoverLetterText] = useState("");
  const [answers, setAnswers] = useState<Record<number, ScreeningAnswerValue>>({});
  const [answerErrors, setAnswerErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    setApplyState("idle");
    setAnswers({});
    setAnswerErrors({});
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

  useEffect(() => {
    if (!job || !isAuthenticated || user?.role !== "Candidate") return;
    getFollowedCompanies()
      .then((followed) => setFollowingCompany(followed.some((f) => f.companyId === job.companyId)))
      .catch(() => setFollowingCompany(false));
  }, [job, isAuthenticated, user]);

  async function handleToggleFollowCompany() {
    if (!job || followPending) return;
    setFollowPending(true);
    try {
      if (followingCompany) {
        await unfollowCompany(job.companyId);
        setFollowingCompany(false);
      } else {
        await followCompany(job.companyId);
        setFollowingCompany(true);
      }
    } catch (err) {
      toast.error(getErrorMessage(err, "Couldn't update followed companies"));
    } finally {
      setFollowPending(false);
    }
  }

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

  async function handleOpenApplyModal() {
    if (!job) return;
    setApplyModalOpen(true);
    try {
      const [templateList, myProfile] = await Promise.all([getMyCoverLetterTemplates(), getMyCandidateProfile()]);
      setTemplates(templateList);
      setProfile(myProfile);
      setCoverLetterText(buildCoverLetterDraft(null, myProfile, job.title, job.companyName));
    } catch {
      // Templates/profile are a convenience for pre-filling — applying still works with a
      // blank cover letter if this fails.
    }
  }

  function handleSelectTemplate(templateId: number | "") {
    setSelectedTemplateId(templateId);
    if (!job || !profile) return;
    const template = templateId === "" ? null : templates.find((t) => t.id === templateId) ?? null;
    setCoverLetterText(buildCoverLetterDraft(template, profile, job.title, job.companyName));
  }

  function validateAnswers(): boolean {
    if (!job) return true;
    const errors: Record<string, string> = {};
    for (const q of job.screeningQuestions) {
      if (!q.isRequired) continue;
      const value = answers[q.id];
      const hasAnswer =
        (value?.textValue?.trim().length ?? 0) > 0 ||
        value?.numberValue !== undefined ||
        (value?.selectedOptionIds?.length ?? 0) > 0;
      if (!hasAnswer) errors[`question_${q.id}`] = "This question is required.";
    }
    setAnswerErrors(errors);
    return Object.keys(errors).length === 0;
  }

  async function handleApply() {
    if (!job) return;
    if (!validateAnswers()) {
      setApplyError("Please answer all required questions.");
      setApplyState("error");
      return;
    }
    setApplyState("applying");
    setApplyError(null);
    setAnswerErrors({});
    try {
      // Answers (and the cover letter) are deliberately left in state on failure — the
      // candidate should never have to re-enter everything after a submission error.
      await applyToJob(job.id, coverLetterText.trim() || undefined, buildScreeningAnswerRequests(answers));
      setApplyState("applied");
      setApplyModalOpen(false);
      toast.success("Application submitted.");
    } catch (err) {
      const fieldErrors = getFieldErrors(err);
      if (fieldErrors) setAnswerErrors(fieldErrors);
      setApplyError(getErrorMessage(err, "Failed to apply"));
      setApplyState("error");
    }
  }

  async function handleSubmitReport() {
    if (!job || !reportReason) return;
    setReportSubmitting(true);
    try {
      await reportJob(job.id, reportReason, reportDetails.trim() || undefined);
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
            {job.companyIsVerified && <VerifiedBadge className="job-card-verified-badge" />}
          </p>
        </div>

        <div className="job-detail-facts">
          <span className="job-detail-fact"><MapPin size={16} /> {job.displayLocation}</span>
          <span className="job-detail-fact"><Briefcase size={16} /> {formatExperienceRange(job.minExperienceYears, job.maxExperienceYears)}</span>
          <span className="job-detail-fact"><Wallet size={16} /> {formatSalaryRange(job.minSalary, job.maxSalary)}</span>
          <span className="job-detail-fact"><Clock size={16} /> Posted {formatRelativeTime(job.createdAt)}</span>
          {job.applicationDeadlineUtc && (
            <span className="job-detail-fact">
              <Clock size={16} /> Applications close {new Date(job.applicationDeadlineUtc).toLocaleDateString()}
            </span>
          )}
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
            {job.applicationDeadlineUtc && (
              <span className="job-detail-fact">
                <Clock size={16} /> Closes {new Date(job.applicationDeadlineUtc).toLocaleDateString()}
              </span>
            )}
          </div>

          {isAuthenticated && user?.role === "Candidate" ? (
            applyState === "applied" ? (
              <p className="success">
                Applied! <Link to="/applications">View your applications</Link>.
              </p>
            ) : (
              <>
                <Button onClick={handleOpenApplyModal} loading={applyState === "applying"} fullWidth>
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

          {isAuthenticated && job.status === "Open" && (
            <Button
              variant="secondary"
              onClick={() => setReferralModalOpen(true)}
              fullWidth
              style={{ marginTop: "0.75rem" }}
            >
              Refer a friend
            </Button>
          )}

          {isAuthenticated && user?.role === "Candidate" && (
            <Button
              variant="secondary"
              icon={<Heart size={16} fill={followingCompany ? "currentColor" : "none"} />}
              onClick={handleToggleFollowCompany}
              loading={followPending}
              fullWidth
              style={{ marginTop: "0.75rem" }}
            >
              {followingCompany ? "Following company" : "Follow company"}
            </Button>
          )}

          <div style={{ marginTop: "0.75rem", display: "flex", justifyContent: "center" }}>
            <ShareMenu jobId={job.id} jobTitle={job.title} />
          </div>

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
        open={applyModalOpen}
        onClose={() => setApplyModalOpen(false)}
        title={`Apply to ${job.title}`}
        footer={
          <>
            <Button variant="secondary" onClick={() => setApplyModalOpen(false)}>Cancel</Button>
            <Button onClick={handleApply} loading={applyState === "applying"}>Submit application</Button>
          </>
        }
      >
        {templates.length > 0 && (
          <FormField label="Start from a template" htmlFor="apply-template" hint="Optional — you can edit the letter below either way.">
            <select
              id="apply-template"
              value={selectedTemplateId}
              onChange={(e) => handleSelectTemplate(e.target.value ? Number(e.target.value) : "")}
            >
              <option value="">Blank cover letter</option>
              {templates.map((t) => (
                <option key={t.id} value={t.id}>{t.title}</option>
              ))}
            </select>
          </FormField>
        )}
        <FormField label="Cover letter" htmlFor="apply-cover-letter" hint="Auto-filled from your profile — edit freely before submitting.">
          <textarea
            id="apply-cover-letter"
            rows={12}
            value={coverLetterText}
            onChange={(e) => setCoverLetterText(e.target.value)}
            maxLength={4000}
          />
        </FormField>
        {job.screeningQuestions.length > 0 && (
          <ScreeningQuestionsForm
            questions={job.screeningQuestions}
            values={answers}
            errors={answerErrors}
            onChange={(questionId, value) => setAnswers((prev) => ({ ...prev, [questionId]: value }))}
          />
        )}
        {applyState === "error" && <p className="error">{applyError}</p>}
      </Modal>

      <Modal
        open={reportOpen}
        onClose={() => {
          setReportOpen(false);
          setReportReason("");
          setReportDetails("");
          setReportSubmitted(false);
        }}
        title="Report this job"
        footer={
          !reportSubmitted && (
            <>
              <Button variant="secondary" onClick={() => setReportOpen(false)}>Cancel</Button>
              <Button onClick={handleSubmitReport} loading={reportSubmitting} disabled={!reportReason}>
                Submit report
              </Button>
            </>
          )
        }
      >
        {reportSubmitted ? (
          <p className="success">Thanks — your report has been submitted for review.</p>
        ) : (
          <>
            <FormField label="Reason" htmlFor="report-reason" required>
              <select
                id="report-reason"
                value={reportReason}
                onChange={(e) => setReportReason(e.target.value as ReportReasonValue)}
              >
                <option value="">Select a reason...</option>
                {REPORT_REASON_LABELS.map((r) => (
                  <option key={r.value} value={r.value}>{r.label}</option>
                ))}
              </select>
            </FormField>
            <FormField label="Additional details" htmlFor="report-details" hint="Optional">
              <textarea
                id="report-details"
                rows={4}
                value={reportDetails}
                onChange={(e) => setReportDetails(e.target.value)}
                placeholder="Tell us what's wrong with this listing..."
              />
            </FormField>
          </>
        )}
      </Modal>

      {referralModalOpen && (
        <CreateReferralModal
          jobPostingId={job.id}
          jobTitle={job.title}
          onClose={() => setReferralModalOpen(false)}
        />
      )}
    </div>
  );
}
