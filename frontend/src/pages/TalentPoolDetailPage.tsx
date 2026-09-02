import { useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { Send, Trash2, Users } from "lucide-react";
import {
  getMyTalentPools, getTalentPoolCandidates, removeCandidateFromPool, updatePoolCandidateNotes,
} from "../api/talentPools";
import { getMyJobs } from "../api/jobs";
import { inviteCandidate } from "../api/invitations";
import { getErrorMessage } from "../utils/errors";
import { resolveAvatarUrl } from "../utils/format";
import { useToast } from "../context/ToastContext";
import type { RecruiterJobSummary, TalentPool, TalentPoolCandidate } from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import Modal from "../components/ui/Modal";
import ConfirmDialog from "../components/ui/ConfirmDialog";
import EmptyState from "../components/ui/EmptyState";
import Avatar from "../components/ui/Avatar";

type SortOption = "recent" | "name" | "experience" | "matchScore";

export default function TalentPoolDetailPage() {
  const { id } = useParams();
  const toast = useToast();
  const [pool, setPool] = useState<TalentPool | null>(null);
  const [candidates, setCandidates] = useState<TalentPoolCandidate[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [sort, setSort] = useState<SortOption>("recent");
  const [removeTarget, setRemoveTarget] = useState<TalentPoolCandidate | null>(null);
  const [editingNotes, setEditingNotes] = useState<TalentPoolCandidate | null>(null);
  const [notesDraft, setNotesDraft] = useState("");
  const [tagsDraft, setTagsDraft] = useState("");
  const [savingNotes, setSavingNotes] = useState(false);

  const [inviteTarget, setInviteTarget] = useState<TalentPoolCandidate | null>(null);
  const [openJobs, setOpenJobs] = useState<RecruiterJobSummary[]>([]);
  const [inviteJobId, setInviteJobId] = useState<number | "">("");
  const [inviteMessage, setInviteMessage] = useState("");
  const [inviteSending, setInviteSending] = useState(false);
  const [inviteError, setInviteError] = useState<string | null>(null);

  async function load() {
    if (!id) return;
    setLoading(true);
    try {
      const [pools, poolCandidates] = await Promise.all([getMyTalentPools(), getTalentPoolCandidates(Number(id))]);
      setPool(pools.find((p) => p.id === Number(id)) ?? null);
      setCandidates(poolCandidates);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to load this talent pool"));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const filtered = useMemo(() => {
    let list = candidates;
    if (search.trim()) {
      const q = search.trim().toLowerCase();
      list = list.filter((c) =>
        c.fullName.toLowerCase().includes(q) ||
        (c.headline ?? "").toLowerCase().includes(q) ||
        (c.skillsCsv ?? "").toLowerCase().includes(q) ||
        (c.tagsCsv ?? "").toLowerCase().includes(q));
    }
    const sorted = [...list];
    sorted.sort((a, b) => {
      if (sort === "name") return a.fullName.localeCompare(b.fullName);
      if (sort === "experience") return (b.totalExperienceYears ?? -1) - (a.totalExperienceYears ?? -1);
      if (sort === "matchScore") return (b.matchScore ?? -1) - (a.matchScore ?? -1);
      return new Date(b.addedAt).getTime() - new Date(a.addedAt).getTime();
    });
    return sorted;
  }, [candidates, search, sort]);

  async function handleRemoveConfirm() {
    if (!removeTarget || !id) return;
    try {
      await removeCandidateFromPool(Number(id), removeTarget.candidateProfileId);
      setCandidates((prev) => prev.filter((c) => c.candidateProfileId !== removeTarget.candidateProfileId));
      toast.success("Removed from pool.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to remove candidate"));
    } finally {
      setRemoveTarget(null);
    }
  }

  function openNotesEditor(candidate: TalentPoolCandidate) {
    setEditingNotes(candidate);
    setNotesDraft(candidate.notes ?? "");
    setTagsDraft(candidate.tagsCsv ?? "");
  }

  async function handleSaveNotes() {
    if (!editingNotes || !id) return;
    setSavingNotes(true);
    try {
      await updatePoolCandidateNotes(Number(id), editingNotes.candidateProfileId, { notes: notesDraft || undefined, tagsCsv: tagsDraft || undefined });
      setCandidates((prev) => prev.map((c) => c.candidateProfileId === editingNotes.candidateProfileId ? { ...c, notes: notesDraft || null, tagsCsv: tagsDraft || null } : c));
      toast.success("Notes saved.");
      setEditingNotes(null);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to save notes"));
    } finally {
      setSavingNotes(false);
    }
  }

  async function openInviteModal(candidate: TalentPoolCandidate) {
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

  if (loading) return <p>Loading...</p>;
  if (!pool) return <p className="error">This talent pool could not be found.</p>;

  return (
    <div>
      <PageHeader title={pool.name} subtitle={`${pool.candidateCount} candidate${pool.candidateCount === 1 ? "" : "s"} saved in this pool.`} />

      <div className="ui-card ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <div className="form-row">
          <input placeholder="Search by name, skill, headline, or tag..." value={search} onChange={(e) => setSearch(e.target.value)} />
          <select value={sort} onChange={(e) => setSort(e.target.value as SortOption)}>
            <option value="recent">Sort: Recently added</option>
            <option value="name">Sort: Name (A-Z)</option>
            <option value="experience">Sort: Most experience</option>
            <option value="matchScore">Sort: Highest match score</option>
          </select>
        </div>
      </div>

      {filtered.length === 0 ? (
        <EmptyState icon={<Users size={32} />} title="No candidates match" description="Try a different search, or save more candidates into this pool from Candidate Search." />
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          {filtered.map((c) => (
            <Card key={c.candidateProfileId} className="ui-card-padded">
              <div style={{ display: "flex", gap: "1rem" }}>
                <Avatar name={c.fullName} size={48} src={resolveAvatarUrl(c.avatarUrl)} />
                <div style={{ flex: 1 }}>
                  <h3>{c.fullName}</h3>
                  {c.headline && <p className="hint">{c.headline}</p>}
                  <p className="hint">{c.displayLocation}{c.totalExperienceYears != null && ` · ${c.totalExperienceYears} yrs experience`}</p>
                  {c.skillsCsv && (
                    <div className="chip-list" style={{ marginTop: "0.5rem" }}>
                      {c.skillsCsv.split(",").map((s) => s.trim()).filter(Boolean).slice(0, 8).map((s) => <span key={s} className="chip">{s}</span>)}
                    </div>
                  )}
                  {c.latestApplicationJobTitle && (
                    <p className="hint" style={{ marginTop: "0.5rem" }}>
                      Latest application: {c.latestApplicationJobTitle} — {c.latestApplicationStatus}
                      {c.matchScore != null && ` (Match ${c.matchScore}/100)`}
                    </p>
                  )}
                  {c.tagsCsv && (
                    <div className="chip-list" style={{ marginTop: "0.5rem" }}>
                      {c.tagsCsv.split(",").map((t) => t.trim()).filter(Boolean).map((t) => <span key={t} className="chip chip-matched">{t}</span>)}
                    </div>
                  )}
                  {c.notes && <p style={{ marginTop: "0.5rem" }} className="hint">Note: {c.notes}</p>}

                  <div style={{ display: "flex", gap: "0.75rem", marginTop: "0.75rem", flexWrap: "wrap" }}>
                    <Button size="sm" variant="secondary" icon={<Send size={14} />} onClick={() => openInviteModal(c)}>Invite to job</Button>
                    <button type="button" className="link-button" onClick={() => openNotesEditor(c)}>Edit notes/tags</button>
                    <button type="button" className="link-button" onClick={() => setRemoveTarget(c)}><Trash2 size={14} /> Remove</button>
                  </div>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      <Modal
        open={editingNotes !== null}
        onClose={() => setEditingNotes(null)}
        title={`Notes for ${editingNotes?.fullName ?? ""}`}
        footer={<><Button variant="secondary" onClick={() => setEditingNotes(null)}>Cancel</Button><Button onClick={handleSaveNotes} loading={savingNotes}>Save</Button></>}
      >
        <FormField label="Private notes" htmlFor="pool-notes" hint="Only visible to your team — never shown to the candidate.">
          <textarea id="pool-notes" rows={4} value={notesDraft} onChange={(e) => setNotesDraft(e.target.value)} />
        </FormField>
        <FormField label="Tags" htmlFor="pool-tags" hint="Comma separated, e.g. senior, remote-ok">
          <input id="pool-tags" value={tagsDraft} onChange={(e) => setTagsDraft(e.target.value)} />
        </FormField>
      </Modal>

      <Modal
        open={inviteTarget !== null}
        onClose={() => setInviteTarget(null)}
        title={`Invite ${inviteTarget?.fullName ?? "candidate"} to apply`}
        footer={<><Button variant="secondary" onClick={() => setInviteTarget(null)}>Cancel</Button><Button onClick={handleSendInvite} loading={inviteSending}>Send invitation</Button></>}
      >
        <FormField label="Job" htmlFor="pool-invite-job" required error={!inviteJobId ? inviteError ?? undefined : undefined}>
          <select id="pool-invite-job" value={inviteJobId} onChange={(e) => setInviteJobId(e.target.value ? Number(e.target.value) : "")}>
            <option value="">Select an open job...</option>
            {openJobs.map((s) => <option key={s.job.id} value={s.job.id}>{s.job.title}</option>)}
          </select>
        </FormField>
        <FormField label="Message" htmlFor="pool-invite-message" hint="Optional — up to 2000 characters.">
          <textarea id="pool-invite-message" rows={4} value={inviteMessage} onChange={(e) => setInviteMessage(e.target.value)} maxLength={2000} />
        </FormField>
        {inviteJobId && inviteError && <p className="error">{inviteError}</p>}
      </Modal>

      <ConfirmDialog
        open={removeTarget !== null}
        title="Remove from pool?"
        confirmLabel="Remove"
        danger
        onConfirm={handleRemoveConfirm}
        onCancel={() => setRemoveTarget(null)}
      >
        <p>Remove <strong>{removeTarget?.fullName}</strong> from this pool?</p>
      </ConfirmDialog>
    </div>
  );
}
