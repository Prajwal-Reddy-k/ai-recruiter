import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Download, Search, Send, Users } from "lucide-react";
import {
  downloadCandidateResume,
  exportCandidatesCsv,
  getCandidateDetail,
  getDiscoverableCandidates,
  searchCandidates,
  type CandidateSearchFilters,
  type DiscoverCandidatesFilters,
} from "../api/recruiterCandidates";
import { updateApplicationStatus, type ApplicationStatusValue } from "../api/applications";
import { inviteCandidate } from "../api/invitations";
import { getMyJobs } from "../api/jobs";
import type { CandidateSearchDetail, CandidateSearchResult, CandidateSortOption, DiscoverableCandidate, RecruiterJobSummary } from "../types";
import { getErrorMessage } from "../utils/errors";
import { saveBlobAsFile } from "../utils/download";
import { toIST } from "../utils/format";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import Modal from "../components/ui/Modal";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";

const STATUS_OPTIONS: ApplicationStatusValue[] = [
  "Applied", "Screening", "Shortlisted", "InterviewScheduled", "InterviewCompleted", "Offer", "Hired", "Rejected", "Withdrawn",
];

const SORT_OPTIONS: { value: CandidateSortOption; label: string }[] = [
  { value: "NewestApplication", label: "Newest application" },
  { value: "HighestMatchScore", label: "Highest match score" },
  { value: "ExperienceDesc", label: "Most experience" },
  { value: "NameAlphabetical", label: "Name (A–Z)" },
];

type ViewMode = "applicants" | "discover";

export default function CandidateSearchPage() {
  const toast = useToast();
  const [view, setView] = useState<ViewMode>("applicants");
  const [results, setResults] = useState<CandidateSearchResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  const [filters, setFilters] = useState<CandidateSearchFilters>({ sort: "NewestApplication" });

  const [detail, setDetail] = useState<CandidateSearchDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  const [discoverFilters, setDiscoverFilters] = useState<DiscoverCandidatesFilters>({});
  const [discoverResults, setDiscoverResults] = useState<DiscoverableCandidate[]>([]);
  const [discoverLoading, setDiscoverLoading] = useState(false);
  const [discoverError, setDiscoverError] = useState<string | null>(null);

  const [inviteTarget, setInviteTarget] = useState<DiscoverableCandidate | null>(null);
  const [openJobs, setOpenJobs] = useState<RecruiterJobSummary[]>([]);
  const [inviteJobId, setInviteJobId] = useState<number | "">("");
  const [inviteMessage, setInviteMessage] = useState("");
  const [inviteError, setInviteError] = useState<string | null>(null);
  const [inviteSending, setInviteSending] = useState(false);

  useEffect(() => {
    void runSearch();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function runSearch() {
    setLoading(true);
    setError(null);
    try {
      setResults(await searchCandidates(filters));
    } catch (err) {
      setError(getErrorMessage(err, "Failed to search candidates"));
    } finally {
      setLoading(false);
    }
  }

  async function handleExport() {
    setExporting(true);
    try {
      const blob = await exportCandidatesCsv(filters);
      saveBlobAsFile(blob, `candidates-${new Date().toISOString().slice(0, 10)}.csv`);
      toast.success("Export downloaded.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to export candidates"));
    } finally {
      setExporting(false);
    }
  }

  async function openDetail(candidateProfileId: number) {
    setDetail(null);
    setDetailError(null);
    setDetailLoading(true);
    try {
      setDetail(await getCandidateDetail(candidateProfileId));
    } catch (err) {
      setDetailError(getErrorMessage(err, "Failed to load candidate"));
    } finally {
      setDetailLoading(false);
    }
  }

  async function handleDownloadResume(applicationId: number, candidateName: string) {
    try {
      const blob = await downloadCandidateResume(applicationId);
      saveBlobAsFile(blob, `${candidateName}-resume`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to download resume"));
    }
  }

  async function handleStatusChange(applicationId: number, status: ApplicationStatusValue) {
    try {
      await updateApplicationStatus(applicationId, status);
      toast.success(`Status updated to ${status}.`);
      if (detail) await openDetail(detail.candidateProfileId);
      setResults((prev) => prev.map((r) => (r.applicationId === applicationId ? { ...r, applicationStatus: status } : r)));
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update status"));
    }
  }

  async function runDiscoverSearch() {
    setDiscoverLoading(true);
    setDiscoverError(null);
    try {
      setDiscoverResults(await getDiscoverableCandidates(discoverFilters));
    } catch (err) {
      setDiscoverError(getErrorMessage(err, "Failed to load discoverable candidates"));
    } finally {
      setDiscoverLoading(false);
    }
  }

  useEffect(() => {
    if (view === "discover" && discoverResults.length === 0 && !discoverLoading) {
      void runDiscoverSearch();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [view]);

  async function openInviteModal(candidate: DiscoverableCandidate) {
    setInviteTarget(candidate);
    setInviteJobId("");
    setInviteMessage("");
    setInviteError(null);
    try {
      const jobs = await getMyJobs();
      setOpenJobs(jobs.filter((j) => j.job.status === "Open"));
    } catch {
      setOpenJobs([]);
    }
  }

  async function handleSendInvite() {
    if (!inviteTarget || !inviteJobId) {
      setInviteError("Choose a job to invite the candidate to.");
      return;
    }
    setInviteSending(true);
    setInviteError(null);
    try {
      await inviteCandidate(Number(inviteJobId), inviteTarget.candidateProfileId, inviteMessage.trim() || undefined);
      toast.success(`Invitation sent to ${inviteTarget.fullName}.`);
      setInviteTarget(null);
    } catch (err) {
      setInviteError(getErrorMessage(err, "Failed to send invitation"));
    } finally {
      setInviteSending(false);
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1><Users size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Candidate Search</h1>
        <p>Search everyone who has applied to your company's jobs, or discover candidates open to being invited.</p>
      </div>

      <div className="admin-tabs" role="tablist" style={{ marginBottom: "1.5rem" }}>
        <button type="button" role="tab" aria-selected={view === "applicants"} className={`admin-tab ${view === "applicants" ? "admin-tab-active" : ""}`} onClick={() => setView("applicants")}>
          Applicants
        </button>
        <button type="button" role="tab" aria-selected={view === "discover"} className={`admin-tab ${view === "discover" ? "admin-tab-active" : ""}`} onClick={() => setView("discover")}>
          Discover candidates
        </button>
      </div>

      {view === "discover" ? (
        <div>
          <div className="ui-card ui-card-padded" style={{ marginBottom: "1.5rem" }}>
            <div className="form-row">
              <input
                placeholder="Skills (comma separated)"
                value={discoverFilters.skills ?? ""}
                onChange={(e) => setDiscoverFilters((f) => ({ ...f, skills: e.target.value }))}
              />
              <input
                placeholder="City"
                value={discoverFilters.city ?? ""}
                onChange={(e) => setDiscoverFilters((f) => ({ ...f, city: e.target.value }))}
              />
              <input
                placeholder="State"
                value={discoverFilters.state ?? ""}
                onChange={(e) => setDiscoverFilters((f) => ({ ...f, state: e.target.value }))}
              />
            </div>
            <div className="form-actions">
              <Button icon={<Search size={16} />} onClick={runDiscoverSearch} loading={discoverLoading}>Search</Button>
            </div>
          </div>

          {discoverError && <p className="error" style={{ marginBottom: "1rem" }}>{discoverError}</p>}

          {discoverLoading ? (
            <p>Loading...</p>
          ) : discoverResults.length === 0 ? (
            <EmptyState icon={<Users size={32} />} title="No discoverable candidates match these filters" description="Candidates appear here once they set their profile visibility to 'Visible to recruiters'." />
          ) : (
            <div className="table-scroll">
              <table className="dashboard-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Skills</th>
                    <th>Location</th>
                    <th>Experience</th>
                    <th>Availability</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {discoverResults.map((c) => (
                    <tr key={c.candidateProfileId}>
                      <td>{c.fullName}</td>
                      <td>{c.skillsCsv ?? "—"}</td>
                      <td>{c.displayLocation}</td>
                      <td>{c.totalExperienceYears != null ? `${c.totalExperienceYears} yrs` : "—"}</td>
                      <td><StatusBadge status={c.availabilityStatus} /></td>
                      <td>
                        <button type="button" className="link-button" onClick={() => openInviteModal(c)}>
                          <Send size={13} style={{ verticalAlign: "-2px", marginRight: "0.2rem" }} />Invite to Apply
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      ) : (
      <>
      <div className="ui-card ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <div className="form-row">
          <input
            placeholder="Skills (comma separated)"
            value={filters.skills ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, skills: e.target.value }))}
          />
          <input
            placeholder="City"
            value={filters.city ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, city: e.target.value }))}
          />
          <input
            placeholder="State"
            value={filters.state ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, state: e.target.value }))}
          />
        </div>
        <div className="form-row">
          <input
            type="number"
            placeholder="Min experience (yrs)"
            value={filters.minExperienceYears ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, minExperienceYears: e.target.value ? Number(e.target.value) : undefined }))}
          />
          <input
            type="number"
            placeholder="Max experience (yrs)"
            value={filters.maxExperienceYears ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, maxExperienceYears: e.target.value ? Number(e.target.value) : undefined }))}
          />
          <input
            placeholder="Education contains..."
            value={filters.education ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, education: e.target.value }))}
          />
        </div>
        <div className="form-row">
          <select value={filters.status ?? ""} onChange={(e) => setFilters((f) => ({ ...f, status: (e.target.value || undefined) as ApplicationStatusValue | undefined }))}>
            <option value="">Any application status</option>
            {STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
          <input
            type="number"
            placeholder="Min match score"
            min={0}
            max={100}
            value={filters.minMatchScore ?? ""}
            onChange={(e) => setFilters((f) => ({ ...f, minMatchScore: e.target.value ? Number(e.target.value) : undefined }))}
          />
          <select value={filters.sort} onChange={(e) => setFilters((f) => ({ ...f, sort: e.target.value as CandidateSortOption }))}>
            {SORT_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
          </select>
        </div>
        <div className="form-actions">
          <Button icon={<Search size={16} />} onClick={runSearch} loading={loading}>Search</Button>
          <Button variant="secondary" icon={<Download size={16} />} onClick={handleExport} loading={exporting} disabled={results.length === 0}>
            Export CSV
          </Button>
        </div>
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      {loading ? (
        <p>Loading...</p>
      ) : results.length === 0 ? (
        <EmptyState icon={<Users size={32} />} title="No candidates match these filters" description="Try widening your search criteria." />
      ) : (
        <div className="table-scroll">
          <table className="dashboard-table">
            <thead>
              <tr>
                <th>Name</th>
                <th>Skills</th>
                <th>Location</th>
                <th>Experience</th>
                <th>Job</th>
                <th>Status</th>
                <th>Applied</th>
                <th>Match</th>
              </tr>
            </thead>
            <tbody>
              {results.map((r) => (
                <tr key={r.applicationId} style={{ cursor: "pointer" }} onClick={() => openDetail(r.candidateProfileId)}>
                  <td>{r.fullName}</td>
                  <td>{r.skillsCsv ?? "—"}</td>
                  <td>{r.city ? `${r.city}${r.state ? `, ${r.state}` : ""}` : "—"}</td>
                  <td>{r.totalExperienceYears != null ? `${r.totalExperienceYears} yrs` : "—"}</td>
                  <td>{r.jobTitle}</td>
                  <td><StatusBadge status={r.applicationStatus} /></td>
                  <td>{new Date(r.appliedAt).toLocaleDateString()}</td>
                  <td>{r.matchScore != null ? `${r.matchScore}/100` : "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      </>
      )}

      <Modal
        open={detailLoading || detail !== null || detailError !== null}
        onClose={() => { setDetail(null); setDetailError(null); }}
        title={detail?.fullName ?? "Candidate"}
      >
        {detailLoading && <p>Loading...</p>}
        {detailError && <p className="error">{detailError}</p>}
        {detail && (
          <div>
            {detail.headline && <p className="hint">{detail.headline}</p>}
            <p style={{ marginTop: "0.5rem" }}>{detail.displayLocation} {detail.totalExperienceYears != null && `· ${detail.totalExperienceYears} yrs experience`}</p>
            {detail.summary && <p style={{ marginTop: "0.75rem" }}>{detail.summary}</p>}
            {detail.education && (
              <>
                <h4 style={{ margin: "1rem 0 0.25rem" }}>Education</h4>
                <p>{detail.education}</p>
              </>
            )}
            {detail.skillsCsv && (
              <>
                <h4 style={{ margin: "1rem 0 0.5rem" }}>Skills</h4>
                <div className="chip-list">
                  {detail.skillsCsv.split(",").map((s) => s.trim()).filter(Boolean).map((s) => (
                    <span key={s} className="chip">{s}</span>
                  ))}
                </div>
              </>
            )}

            <h4 style={{ margin: "1rem 0 0.5rem" }}>Applications at your company</h4>
            {detail.applications.map((app) => (
              <div key={app.applicationId} style={{ borderTop: "1px solid var(--border)", paddingTop: "0.75rem", marginTop: "0.75rem" }}>
                <p>
                  <Link to={`/jobs/${app.jobId}`}>{app.jobTitle}</Link>{" "}
                  <StatusBadge status={app.status} />
                </p>
                <p className="hint">
                  Applied {new Date(app.appliedAt).toLocaleDateString()}
                  {app.matchScore != null && ` · Match ${app.matchScore}/100`}
                </p>

                {app.interviews.length > 0 && (
                  <p className="hint">
                    {app.interviews.map((iv) => (
                      <span key={iv.interviewId}>
                        Interview: {iv.status}{iv.nextSlotUtc && ` at ${toIST(iv.nextSlotUtc)}`}
                      </span>
                    ))}
                  </p>
                )}

                <div style={{ display: "flex", gap: "0.75rem", marginTop: "0.5rem", flexWrap: "wrap" }}>
                  {app.hasResumeOnFile && app.canManage && (
                    <button type="button" className="link-button" onClick={() => handleDownloadResume(app.applicationId, detail.fullName)}>
                      Download resume
                    </button>
                  )}
                  {app.canManage ? (
                    <select value={app.status as ApplicationStatusValue} onChange={(e) => handleStatusChange(app.applicationId, e.target.value as ApplicationStatusValue)}>
                      {STATUS_OPTIONS.map((s) => <option key={s} value={s}>{s}</option>)}
                    </select>
                  ) : (
                    <span className="hint">Managed by another recruiter at your company</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </Modal>

      <Modal
        open={inviteTarget !== null}
        onClose={() => setInviteTarget(null)}
        title={`Invite ${inviteTarget?.fullName ?? "candidate"} to apply`}
        footer={
          <>
            <Button variant="secondary" onClick={() => setInviteTarget(null)}>Cancel</Button>
            <Button onClick={handleSendInvite} loading={inviteSending}>Send invitation</Button>
          </>
        }
      >
        <FormField label="Job" htmlFor="invite-job" required error={!inviteJobId ? inviteError ?? undefined : undefined}>
          <select id="invite-job" value={inviteJobId} onChange={(e) => setInviteJobId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Select an open job...</option>
            {openJobs.map((s) => <option key={s.job.id} value={s.job.id}>{s.job.title}</option>)}
          </select>
        </FormField>
        <FormField label="Message" htmlFor="invite-message" hint="Optional — up to 2000 characters.">
          <textarea
            id="invite-message"
            rows={4}
            value={inviteMessage}
            onChange={(e) => setInviteMessage(e.target.value)}
            maxLength={2000}
            placeholder="Tell them why you think they'd be a great fit..."
          />
        </FormField>
        {inviteJobId && inviteError && <p className="error">{inviteError}</p>}
      </Modal>
    </div>
  );
}
