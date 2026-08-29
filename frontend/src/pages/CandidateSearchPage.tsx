import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Download, Search, Users } from "lucide-react";
import {
  downloadCandidateResume,
  exportCandidatesCsv,
  getCandidateDetail,
  searchCandidates,
  type CandidateSearchFilters,
} from "../api/recruiterCandidates";
import { updateApplicationStatus, type ApplicationStatusValue } from "../api/applications";
import type { CandidateSearchDetail, CandidateSearchResult, CandidateSortOption } from "../types";
import { getErrorMessage } from "../utils/errors";
import { saveBlobAsFile } from "../utils/download";
import { toIST } from "../utils/format";
import { useToast } from "../context/ToastContext";
import { StatusBadge } from "../components/ui/Badge";
import EmptyState from "../components/ui/EmptyState";
import Modal from "../components/ui/Modal";
import Button from "../components/ui/Button";

const STATUS_OPTIONS: ApplicationStatusValue[] = [
  "Applied", "Screening", "Shortlisted", "InterviewScheduled", "InterviewCompleted", "Offer", "Hired", "Rejected", "Withdrawn",
];

const SORT_OPTIONS: { value: CandidateSortOption; label: string }[] = [
  { value: "NewestApplication", label: "Newest application" },
  { value: "HighestMatchScore", label: "Highest match score" },
  { value: "ExperienceDesc", label: "Most experience" },
  { value: "NameAlphabetical", label: "Name (A–Z)" },
];

export default function CandidateSearchPage() {
  const toast = useToast();
  const [results, setResults] = useState<CandidateSearchResult[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState(false);

  const [filters, setFilters] = useState<CandidateSearchFilters>({ sort: "NewestApplication" });

  const [detail, setDetail] = useState<CandidateSearchDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

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

  return (
    <div>
      <div className="page-header">
        <h1><Users size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Candidate Search</h1>
        <p>Search everyone who has applied to your company's jobs.</p>
      </div>

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
    </div>
  );
}
