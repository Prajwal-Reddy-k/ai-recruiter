import { useEffect, useState, type FormEvent } from "react";
import { Target, Plus, Pencil, Trash2, PartyPopper } from "lucide-react";
import { getMyCareerGoals, createCareerGoal, updateCareerGoal, deleteCareerGoal } from "../api/careerGoals";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { CareerGoal, CareerGoalStatusName } from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import Modal from "../components/ui/Modal";
import ConfirmDialog from "../components/ui/ConfirmDialog";
import EmptyState from "../components/ui/EmptyState";
import IndiaLocationSelector from "../components/IndiaLocationSelector";
import { StatusBadge } from "../components/ui/Badge";

interface GoalFormData {
  targetRole?: string;
  targetSkill?: string;
  targetCompanyType?: string;
  preferredState?: string;
  preferredCity?: string;
  isLocationRemote: boolean;
  targetCompletionDate?: string | null;
  progressPercent: number;
  status: CareerGoalStatusName;
  notes?: string;
}

function dateInput(value: string | null): string {
  return value ? value.slice(0, 10) : "";
}

function GoalModal({ initial, onClose, onSave }: { initial: CareerGoal | null; onClose: () => void; onSave: (data: GoalFormData) => Promise<void> }) {
  const [targetRole, setTargetRole] = useState(initial?.targetRole ?? "");
  const [targetSkill, setTargetSkill] = useState(initial?.targetSkill ?? "");
  const [targetCompanyType, setTargetCompanyType] = useState(initial?.targetCompanyType ?? "");
  const [state, setState] = useState(initial?.preferredState ?? "");
  const [city, setCity] = useState(initial?.preferredCity ?? "");
  const [locality, setLocality] = useState("");
  const [isRemote, setIsRemote] = useState(initial?.isLocationRemote ?? false);
  const [targetCompletionDate, setTargetCompletionDate] = useState(dateInput(initial?.targetCompletionDate ?? null));
  const [progressPercent, setProgressPercent] = useState(initial?.progressPercent ?? 0);
  const [status, setStatus] = useState<CareerGoalStatusName>(initial?.status ?? "InProgress");
  const [notes, setNotes] = useState(initial?.notes ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      await onSave({
        targetRole: targetRole || undefined,
        targetSkill: targetSkill || undefined,
        targetCompanyType: targetCompanyType || undefined,
        preferredState: state || undefined,
        preferredCity: city || undefined,
        isLocationRemote: isRemote,
        targetCompletionDate: targetCompletionDate || null,
        progressPercent,
        status,
        notes: notes || undefined,
      });
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe);
      else setErrors({ general: getErrorMessage(err, "Failed to save goal") });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      title={initial ? "Edit career goal" : "New career goal"}
      footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save</Button></>}
    >
      <form onSubmit={handleSubmit} noValidate>
        <p className="hint" style={{ marginBottom: "0.75rem" }}>Set at least one target below.</p>
        <FormField label="Target role" htmlFor="goal-role" error={errors.targetRole}>
          <input id="goal-role" value={targetRole} onChange={(e) => setTargetRole(e.target.value)} maxLength={150} />
        </FormField>
        <FormField label="Target skill" htmlFor="goal-skill" error={errors.targetSkill}>
          <input id="goal-skill" value={targetSkill} onChange={(e) => setTargetSkill(e.target.value)} maxLength={150} />
        </FormField>
        <FormField label="Target company type" htmlFor="goal-company-type" hint="e.g. Startup, FinTech, Product company" error={errors.targetCompanyType}>
          <input id="goal-company-type" value={targetCompanyType} onChange={(e) => setTargetCompanyType(e.target.value)} maxLength={150} />
        </FormField>

        <IndiaLocationSelector
          state={state}
          city={city}
          locality={locality}
          isRemote={isRemote}
          onStateChange={setState}
          onCityChange={setCity}
          onLocalityChange={setLocality}
          onIsRemoteChange={setIsRemote}
          stateError={errors.preferredState}
          cityError={errors.preferredCity}
          showRemoteOption
        />

        <div className="form-row">
          <FormField label="Target completion date" htmlFor="goal-date">
            <input id="goal-date" type="date" value={targetCompletionDate} onChange={(e) => setTargetCompletionDate(e.target.value)} />
          </FormField>
          <FormField label="Status" htmlFor="goal-status">
            <select id="goal-status" value={status} onChange={(e) => setStatus(e.target.value as CareerGoalStatusName)}>
              <option value="InProgress">In Progress</option>
              <option value="Completed">Completed</option>
              <option value="Paused">Paused</option>
            </select>
          </FormField>
        </div>

        <FormField label={`Progress: ${progressPercent}%`} htmlFor="goal-progress" error={errors.progressPercent}>
          <input id="goal-progress" type="range" min={0} max={100} value={progressPercent} onChange={(e) => setProgressPercent(Number(e.target.value))} />
        </FormField>

        <FormField label="Notes" htmlFor="goal-notes" error={errors.notes}>
          <textarea id="goal-notes" rows={3} value={notes} onChange={(e) => setNotes(e.target.value)} maxLength={1000} />
        </FormField>
        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}

const STATUS_TABS: { value: CareerGoalStatusName | "all"; label: string }[] = [
  { value: "all", label: "All" },
  { value: "InProgress", label: "In Progress" },
  { value: "Completed", label: "Completed" },
  { value: "Paused", label: "Paused" },
];

export default function CareerGoalsPage() {
  const toast = useToast();
  const [goals, setGoals] = useState<CareerGoal[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<CareerGoalStatusName | "all">("all");
  const [editing, setEditing] = useState<CareerGoal | "new" | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<CareerGoal | null>(null);

  async function load() {
    setLoading(true);
    try {
      const summary = await getMyCareerGoals();
      setGoals(summary.goals);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to load your career goals"));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleDeleteConfirm() {
    if (!deleteTarget) return;
    try {
      await deleteCareerGoal(deleteTarget.id);
      setGoals((prev) => prev.filter((g) => g.id !== deleteTarget.id));
      toast.success("Goal removed.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to remove goal"));
    } finally {
      setDeleteTarget(null);
    }
  }

  const filtered = filter === "all" ? goals : goals.filter((g) => g.status === filter);
  const goalLabel = (g: CareerGoal) => g.targetRole || g.targetSkill || g.targetCompanyType || "Career goal";

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <PageHeader
        title={<><Target size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Career Goals</>}
        subtitle="Set goals for where you want your career to go, and track your progress toward them."
        action={<Button icon={<Plus size={16} />} onClick={() => setEditing("new")}>New goal</Button>}
      />

      <div className="admin-tabs" role="tablist" style={{ marginBottom: "1.5rem" }}>
        {STATUS_TABS.map((tab) => (
          <button
            key={tab.value}
            type="button"
            role="tab"
            aria-selected={filter === tab.value}
            className={`admin-tab ${filter === tab.value ? "admin-tab-active" : ""}`}
            onClick={() => setFilter(tab.value)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {filtered.length === 0 ? (
        <EmptyState
          icon={<Target size={32} />}
          title="No career goals yet"
          description="Set a goal — a target role, a skill to build, or a company type — and track your progress."
          action={<Button icon={<Plus size={16} />} onClick={() => setEditing("new")}>New goal</Button>}
        />
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          {filtered.map((g) => (
            <Card key={g.id} className="ui-card-padded">
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "0.5rem" }}>
                <div>
                  <h3>
                    {g.status === "Completed" && <PartyPopper size={16} style={{ verticalAlign: "-2px", marginRight: "0.3rem", color: "var(--primary)" }} />}
                    {goalLabel(g)}
                  </h3>
                  <p className="hint">
                    {[g.targetSkill && g.targetRole ? g.targetSkill : null, g.targetCompanyType, g.preferredCity || g.preferredState, g.isLocationRemote ? "Remote OK" : null]
                      .filter(Boolean).join(" · ")}
                    {g.targetCompletionDate && ` · Target: ${new Date(g.targetCompletionDate).toLocaleDateString()}`}
                  </p>
                </div>
                <StatusBadge status={g.status} />
              </div>

              <div className="progress-bar" style={{ margin: "0.75rem 0" }}>
                <div className="progress-bar-fill" style={{ width: `${g.progressPercent}%` }} />
              </div>
              <p className="hint">{g.progressPercent}% complete</p>

              {g.notes && <p style={{ marginTop: "0.5rem" }}>{g.notes}</p>}

              {g.suggestions.length > 0 && (
                <div style={{ marginTop: "0.75rem" }}>
                  <p className="hint" style={{ marginBottom: "0.35rem" }}>Suggestions:</p>
                  <ul style={{ paddingLeft: "1.25rem" }}>
                    {g.suggestions.map((s) => (
                      <li key={s.label} className="hint">{s.label} — {s.tip}</li>
                    ))}
                  </ul>
                </div>
              )}

              <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.75rem" }}>
                <button type="button" className="link-button" onClick={() => setEditing(g)}><Pencil size={14} /> Edit</button>
                <button type="button" className="link-button" onClick={() => setDeleteTarget(g)}><Trash2 size={14} /> Remove</button>
              </div>
            </Card>
          ))}
        </div>
      )}

      {editing && (
        <GoalModal
          initial={editing === "new" ? null : editing}
          onClose={() => setEditing(null)}
          onSave={async (data) => {
            const wasCompleted = editing !== "new" && editing.status === "Completed";
            const updated = editing === "new" ? await createCareerGoal(data) : await updateCareerGoal(editing.id, data);
            setGoals((prev) => editing === "new" ? [updated, ...prev] : prev.map((g) => (g.id === updated.id ? updated : g)));
            setEditing(null);
            if (updated.status === "Completed" && !wasCompleted) {
              toast.success("🎉 Goal completed — congratulations!");
            } else {
              toast.success("Goal saved.");
            }
          }}
        />
      )}

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Remove this goal?"
        confirmLabel="Remove"
        danger
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      >
        <p>Remove <strong>{deleteTarget ? goalLabel(deleteTarget) : ""}</strong>? This can't be undone.</p>
      </ConfirmDialog>
    </div>
  );
}
