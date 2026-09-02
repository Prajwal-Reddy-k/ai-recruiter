import { useEffect, useState } from "react";
import { getMyTalentPools, createTalentPool, addCandidateToPool } from "../api/talentPools";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { TalentPool } from "../types";
import Modal from "./ui/Modal";
import Button from "./ui/Button";
import FormField from "./ui/FormField";

interface SaveToPoolModalProps {
  candidateProfileId: number;
  candidateName: string;
  onClose: () => void;
}

export default function SaveToPoolModal({ candidateProfileId, candidateName, onClose }: SaveToPoolModalProps) {
  const toast = useToast();
  const [pools, setPools] = useState<TalentPool[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedPoolId, setSelectedPoolId] = useState<number | "">("");
  const [newPoolName, setNewPoolName] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMyTalentPools().then(setPools).catch(() => setPools([])).finally(() => setLoading(false));
  }, []);

  async function handleSave() {
    setSaving(true);
    setError(null);
    try {
      const poolId = selectedPoolId || (await createTalentPool(newPoolName.trim())).id;
      await addCandidateToPool(Number(poolId), { candidateProfileId });
      toast.success(`Saved ${candidateName} to pool.`);
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Failed to save candidate to pool"));
    } finally {
      setSaving(false);
    }
  }

  const canSave = (selectedPoolId !== "" || newPoolName.trim().length > 0) && !saving;

  return (
    <Modal
      open
      onClose={onClose}
      title={`Save ${candidateName} to a pool`}
      footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSave} loading={saving} disabled={!canSave}>Save</Button></>}
    >
      {loading ? (
        <p>Loading...</p>
      ) : (
        <>
          {pools.length > 0 && (
            <FormField label="Choose an existing pool" htmlFor="save-pool-select">
              <select id="save-pool-select" value={selectedPoolId} onChange={(e) => { setSelectedPoolId(e.target.value ? Number(e.target.value) : ""); setNewPoolName(""); }}>
                <option value="">Select a pool...</option>
                {pools.map((p) => <option key={p.id} value={p.id}>{p.name}</option>)}
              </select>
            </FormField>
          )}
          <FormField label="Or create a new pool" htmlFor="save-pool-new">
            <input
              id="save-pool-new"
              value={newPoolName}
              onChange={(e) => { setNewPoolName(e.target.value); setSelectedPoolId(""); }}
              placeholder="e.g. Frontend Talent"
            />
          </FormField>
          {error && <p className="error">{error}</p>}
        </>
      )}
    </Modal>
  );
}
