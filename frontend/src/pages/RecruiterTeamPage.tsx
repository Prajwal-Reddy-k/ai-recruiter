import { useEffect, useState, type FormEvent } from "react";
import { ShieldAlert, Trash2, UserPlus, Users } from "lucide-react";
import { addTeamMember, getMyTeam, removeTeamMember, updateTeamMemberRole } from "../api/team";
import type { CompanyRoleValue, TeamMember } from "../types";
import { getErrorCode, getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import Modal from "../components/ui/Modal";
import FormField from "../components/ui/FormField";
import { Badge } from "../components/ui/Badge";

const ROLE_OPTIONS: { value: CompanyRoleValue; label: string; description: string }[] = [
  { value: "Owner", label: "Company Owner", description: "Manages company, jobs, team, and all applicants." },
  { value: "Recruiter", label: "Recruiter", description: "Manages assigned jobs and applicants." },
  { value: "HiringManager", label: "Hiring Manager", description: "Reviews assigned jobs/applicants and adds feedback." },
  { value: "Interviewer", label: "Interviewer", description: "Accesses assigned interviews/applicants and submits scorecards only." },
];

const ROLE_TONE: Record<string, "success" | "accent" | "info" | "neutral"> = {
  Owner: "success",
  Recruiter: "accent",
  HiringManager: "info",
  Interviewer: "neutral",
};

export default function RecruiterTeamPage() {
  const toast = useToast();
  const [members, setMembers] = useState<TeamMember[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [forbidden, setForbidden] = useState(false);
  const [addOpen, setAddOpen] = useState(false);
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<CompanyRoleValue>("Recruiter");
  const [saving, setSaving] = useState(false);
  const [addError, setAddError] = useState<string | null>(null);
  const [removeTarget, setRemoveTarget] = useState<TeamMember | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setLoading(true);
    setError(null);
    setForbidden(false);
    try {
      setMembers(await getMyTeam());
    } catch (err) {
      if (getErrorCode(err) === "FORBIDDEN") {
        setForbidden(true);
      } else {
        setError(getErrorMessage(err, "Failed to load your team"));
      }
    } finally {
      setLoading(false);
    }
  }

  async function handleAdd(e: FormEvent) {
    e.preventDefault();
    setAddError(null);
    if (!email.trim()) {
      setAddError("Email is required.");
      return;
    }

    setSaving(true);
    try {
      const member = await addTeamMember(email.trim(), role);
      setMembers((prev) => {
        const exists = prev.some((m) => m.recruiterProfileId === member.recruiterProfileId);
        return exists ? prev.map((m) => (m.recruiterProfileId === member.recruiterProfileId ? member : m)) : [...prev, member];
      });
      toast.success(`${member.fullName} added to the team.`);
      setAddOpen(false);
      setEmail("");
      setRole("Recruiter");
    } catch (err) {
      setAddError(getErrorMessage(err, "Failed to add team member"));
    } finally {
      setSaving(false);
    }
  }

  async function handleRoleChange(member: TeamMember, newRole: CompanyRoleValue) {
    setBusyId(member.recruiterProfileId);
    try {
      const updated = await updateTeamMemberRole(member.recruiterProfileId, newRole);
      setMembers((prev) => prev.map((m) => (m.recruiterProfileId === updated.recruiterProfileId ? updated : m)));
      toast.success(`${updated.fullName}'s role updated to ${newRole}.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update role"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleRemove() {
    if (!removeTarget) return;
    setBusyId(removeTarget.recruiterProfileId);
    try {
      await removeTeamMember(removeTarget.recruiterProfileId);
      setMembers((prev) => prev.filter((m) => m.recruiterProfileId !== removeTarget.recruiterProfileId));
      toast.success(`${removeTarget.fullName} removed from the team.`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to remove team member"));
    } finally {
      setBusyId(null);
      setRemoveTarget(null);
    }
  }

  if (loading) return <p>Loading...</p>;

  if (forbidden) {
    return (
      <Card className="ui-card-padded">
        <ShieldAlert size={28} />
        <h2 style={{ marginTop: "0.75rem" }}>You don't have permission to manage the hiring team</h2>
        <p className="hint">Only a Company Owner can view and manage team members. Ask your Owner to add you or change your role.</p>
      </Card>
    );
  }

  return (
    <div>
      <div className="page-header">
        <h1><Users size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Hiring Team</h1>
        <p>Manage who at your company can access jobs, applicants, and interviews.</p>
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      <div style={{ marginBottom: "1.5rem" }}>
        <Button icon={<UserPlus size={16} />} onClick={() => setAddOpen(true)}>Add team member</Button>
      </div>

      <div className="table-scroll">
        <table className="dashboard-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Joined</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {members.map((m) => (
              <tr key={m.recruiterProfileId}>
                <td>{m.fullName}{m.designation ? ` · ${m.designation}` : ""}</td>
                <td>{m.email}</td>
                <td>
                  <select
                    value={m.companyRole}
                    disabled={busyId === m.recruiterProfileId}
                    onChange={(e) => handleRoleChange(m, e.target.value as CompanyRoleValue)}
                    aria-label={`Role for ${m.fullName}`}
                  >
                    {ROLE_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                  </select>
                  {" "}
                  <Badge tone={ROLE_TONE[m.companyRole] ?? "neutral"}>{m.companyRole}</Badge>
                </td>
                <td>{new Date(m.joinedAt).toLocaleDateString()}</td>
                <td>
                  <Button size="sm" variant="danger" icon={<Trash2 size={14} />} loading={busyId === m.recruiterProfileId} onClick={() => setRemoveTarget(m)}>
                    Remove
                  </Button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Modal open={addOpen} onClose={() => setAddOpen(false)} title="Add team member">
        <form onSubmit={handleAdd} noValidate>
          <FormField label="Email" htmlFor="team-email" hint="Must already be registered as a Recruiter." error={addError ?? undefined}>
            <input id="team-email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="colleague@company.com" />
          </FormField>
          <FormField label="Role" htmlFor="team-role">
            <select id="team-role" value={role} onChange={(e) => setRole(e.target.value as CompanyRoleValue)}>
              {ROLE_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
            </select>
          </FormField>
          <p className="hint" style={{ marginBottom: "1rem" }}>{ROLE_OPTIONS.find((o) => o.value === role)?.description}</p>
          <div className="form-actions">
            <Button type="submit" loading={saving}>Add member</Button>
            <Button type="button" variant="secondary" onClick={() => setAddOpen(false)}>Cancel</Button>
          </div>
        </form>
      </Modal>

      <Modal
        open={removeTarget !== null}
        onClose={() => setRemoveTarget(null)}
        title="Remove team member?"
        footer={
          <>
            <Button variant="danger" loading={busyId === removeTarget?.recruiterProfileId} onClick={handleRemove}>Remove</Button>
            <Button variant="secondary" onClick={() => setRemoveTarget(null)}>Cancel</Button>
          </>
        }
      >
        <p>Remove {removeTarget?.fullName} from the hiring team? They will lose access to company jobs and applicants.</p>
      </Modal>
    </div>
  );
}
