import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { Users } from "lucide-react";
import { getApplicationsForJob, updateApplicationStatus, type ApplicationStatusValue } from "../api/applications";
import type { JobApplication } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import EmptyState from "../components/ui/EmptyState";
import Avatar from "../components/ui/Avatar";
import { resolveAvatarUrl } from "../utils/format";

interface Column {
  key: string;
  label: string;
  statuses: ApplicationStatusValue[];
  primaryTarget: ApplicationStatusValue;
}

const COLUMNS: Column[] = [
  { key: "new", label: "New", statuses: ["Applied"], primaryTarget: "Applied" },
  { key: "screening", label: "Screening", statuses: ["Screening"], primaryTarget: "Screening" },
  { key: "shortlisted", label: "Shortlisted", statuses: ["Shortlisted"], primaryTarget: "Shortlisted" },
  { key: "interview", label: "Interview", statuses: ["InterviewScheduled", "InterviewCompleted"], primaryTarget: "InterviewScheduled" },
  { key: "offer", label: "Offer", statuses: ["Offer", "Hired"], primaryTarget: "Offer" },
  { key: "rejected", label: "Rejected", statuses: ["Rejected"], primaryTarget: "Rejected" },
];

const ALL_STATUSES: ApplicationStatusValue[] = [
  "Applied",
  "Screening",
  "Shortlisted",
  "InterviewScheduled",
  "InterviewCompleted",
  "Offer",
  "Hired",
  "Rejected",
];

export default function KanbanBoardPage() {
  const { id } = useParams();
  const toast = useToast();
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [movingId, setMovingId] = useState<number | null>(null);
  const [search, setSearch] = useState("");

  useEffect(() => {
    if (!id) return;
    getApplicationsForJob(Number(id))
      .then(setApplications)
      .catch(() => setError("You don't have access to this job's applicants."))
      .finally(() => setLoading(false));
  }, [id]);

  const filtered = useMemo(() => {
    if (!search.trim()) return applications;
    const q = search.trim().toLowerCase();
    return applications.filter((a) =>
      (a.candidateFullName ?? "").toLowerCase().includes(q) ||
      (a.candidateHeadline ?? "").toLowerCase().includes(q) ||
      (a.candidateSkillsCsv ?? "").toLowerCase().includes(q)
    );
  }, [applications, search]);

  async function handleMove(applicationId: number, status: ApplicationStatusValue) {
    setMovingId(applicationId);
    try {
      const updated = await updateApplicationStatus(applicationId, status);
      setApplications((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      toast.success(`Moved to ${status}.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to move applicant"));
    } finally {
      setMovingId(null);
    }
  }

  function handleDrop(e: React.DragEvent, target: ApplicationStatusValue) {
    e.preventDefault();
    const applicationId = Number(e.dataTransfer.getData("text/plain"));
    if (applicationId) void handleMove(applicationId, target);
  }

  if (loading) return <p>Loading...</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <div>
      <div className="page-header">
        <h1>Applicant Board</h1>
        <p className="hint">
          Use the "Move to…" menu on each card to change status — drag-and-drop also works with a mouse.{" "}
          {id && <Link to={`/jobs/${id}/applicants`}>Switch to list view →</Link>}
        </p>
        <input
          placeholder="Search by name, headline, or skill"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label="Search applicants"
          style={{ maxWidth: 320, marginTop: "0.75rem" }}
        />
      </div>

      {applications.length === 0 ? (
        <EmptyState icon={<Users size={32} />} title="No applicants yet" description="Check back once candidates start applying." />
      ) : (
        <div className="kanban-board">
          {COLUMNS.map((col) => {
            const cards = filtered.filter((a) => col.statuses.includes(a.status as ApplicationStatusValue));
            return (
              <div
                key={col.key}
                className="kanban-column"
                onDragOver={(e) => e.preventDefault()}
                onDrop={(e) => handleDrop(e, col.primaryTarget)}
              >
                <h3 className="kanban-column-title">{col.label} <span className="kanban-column-count">{cards.length}</span></h3>
                <div className="kanban-column-cards">
                  {cards.map((app) => {
                    const skills = (app.candidateSkillsCsv ?? "").split(",").map((s) => s.trim()).filter(Boolean).slice(0, 3);
                    return (
                      <div
                        key={app.id}
                        className="kanban-card"
                        draggable
                        onDragStart={(e) => e.dataTransfer.setData("text/plain", String(app.id))}
                      >
                        <Link to={`/applications/${app.id}`} className="kanban-card-title" style={{ display: "flex", alignItems: "center", gap: "0.5rem" }}>
                          <Avatar name={app.candidateFullName ?? "?"} size={24} src={resolveAvatarUrl(app.candidateAvatarUrl)} />
                          {app.candidateFullName ?? `Application #${app.id}`}
                        </Link>
                        {app.candidateHeadline && <p className="hint">{app.candidateHeadline}</p>}
                        {skills.length > 0 && (
                          <div className="chip-list">
                            {skills.map((s) => <span key={s} className="chip">{s}</span>)}
                          </div>
                        )}
                        <p className="hint">
                          {app.matchScore !== null && `Match ${app.matchScore}/100 · `}
                          {new Date(app.createdAt).toLocaleDateString()}
                        </p>
                        {app.requiredQuestionsTotalCount > 0 && (
                          <p className="hint">Screening: {app.requiredQuestionsAnsweredCount}/{app.requiredQuestionsTotalCount} required answered</p>
                        )}
                        <label className="hint" htmlFor={`move-${app.id}`}>Move to…</label>
                        <select
                          id={`move-${app.id}`}
                          value={app.status}
                          disabled={movingId === app.id}
                          onChange={(e) => handleMove(app.id, e.target.value as ApplicationStatusValue)}
                        >
                          {ALL_STATUSES.map((s) => (
                            <option key={s} value={s}>{s}</option>
                          ))}
                        </select>
                      </div>
                    );
                  })}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
