import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { Plus, Users } from "lucide-react";
import { getMyTalentPools, createTalentPool, renameTalentPool, deleteTalentPool } from "../api/talentPools";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { TalentPool } from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import Modal from "../components/ui/Modal";
import ConfirmDialog from "../components/ui/ConfirmDialog";
import EmptyState from "../components/ui/EmptyState";

function PoolModal({ initial, onClose, onSave }: { initial: TalentPool | null; onClose: () => void; onSave: (name: string) => Promise<void> }) {
  const [name, setName] = useState(initial?.name ?? "");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      await onSave(name);
    } catch (err) {
      setError(getErrorMessage(err, "Failed to save pool"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      title={initial ? "Rename pool" : "New talent pool"}
      footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save</Button></>}
    >
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Pool name" htmlFor="pool-name" required hint="e.g. Frontend Talent, Immediate Joiners" error={error ?? undefined}>
          <input id="pool-name" value={name} onChange={(e) => setName(e.target.value)} maxLength={100} />
        </FormField>
      </form>
    </Modal>
  );
}

export default function RecruiterTalentPoolsPage() {
  const toast = useToast();
  const [pools, setPools] = useState<TalentPool[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<TalentPool | "new" | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<TalentPool | null>(null);

  useEffect(() => {
    getMyTalentPools()
      .then(setPools)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load talent pools")))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleDeleteConfirm() {
    if (!deleteTarget) return;
    try {
      await deleteTalentPool(deleteTarget.id);
      setPools((prev) => prev.filter((p) => p.id !== deleteTarget.id));
      toast.success("Pool removed.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to remove pool"));
    } finally {
      setDeleteTarget(null);
    }
  }

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <PageHeader
        title={<><Users size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Talent Pools</>}
        subtitle="Private, company-wide shortlists of candidates you've saved for future roles."
        action={<Button icon={<Plus size={16} />} onClick={() => setEditing("new")}>New pool</Button>}
      />

      {pools.length === 0 ? (
        <EmptyState
          icon={<Users size={32} />}
          title="No talent pools yet"
          description="Create a pool like 'Frontend Talent' or 'Immediate Joiners', then save candidates into it from Candidate Search or your applicant lists."
          action={<Button icon={<Plus size={16} />} onClick={() => setEditing("new")}>New pool</Button>}
        />
      ) : (
        <div className="dashboard-grid">
          {pools.map((pool) => (
            <Card key={pool.id} className="ui-card-padded">
              <h3>{pool.name}</h3>
              <p className="hint">{pool.candidateCount} candidate{pool.candidateCount === 1 ? "" : "s"}</p>
              <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.75rem" }}>
                <Link to={`/recruiter/talent-pools/${pool.id}`} className="btn btn-secondary btn-sm">Open</Link>
                <button type="button" className="link-button" onClick={() => setEditing(pool)}>Rename</button>
                <button type="button" className="link-button" onClick={() => setDeleteTarget(pool)}>Delete</button>
              </div>
            </Card>
          ))}
        </div>
      )}

      {editing && (
        <PoolModal
          initial={editing === "new" ? null : editing}
          onClose={() => setEditing(null)}
          onSave={async (name) => {
            const updated = editing === "new" ? await createTalentPool(name) : await renameTalentPool(editing.id, name);
            setPools((prev) => editing === "new" ? [...prev, updated] : prev.map((p) => (p.id === updated.id ? updated : p)));
            setEditing(null);
            toast.success("Saved.");
          }}
        />
      )}

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Delete this pool?"
        confirmLabel="Delete"
        danger
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      >
        <p>Delete <strong>{deleteTarget?.name}</strong>? All candidates saved in this pool will be removed from it. This can't be undone.</p>
      </ConfirmDialog>
    </div>
  );
}
