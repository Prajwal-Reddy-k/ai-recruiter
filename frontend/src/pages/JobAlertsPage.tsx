import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { Bell, Pencil } from "lucide-react";
import { createAlert, deleteAlert, getMyAlerts, setAlertActive, updateAlert } from "../api/savedJobs";
import type { JobAlert } from "../types";
import type { JobTypeValue } from "../api/jobs";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import IndiaLocationSelector from "../components/IndiaLocationSelector";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import EmptyState from "../components/ui/EmptyState";

const JOB_TYPE_OPTIONS: { value: JobTypeValue; label: string }[] = [
  { value: "FullTime", label: "Full-time" },
  { value: "PartTime", label: "Part-time" },
  { value: "Contract", label: "Contract" },
  { value: "Internship", label: "Internship" },
  { value: "Freelance", label: "Freelance" },
];

interface AlertFormState {
  skillsCsv: string;
  state: string;
  city: string;
  isRemote: boolean;
  jobType: JobTypeValue | "";
  minExperienceYears: string;
}

const EMPTY_FORM: AlertFormState = { skillsCsv: "", state: "", city: "", isRemote: false, jobType: "", minExperienceYears: "" };

export default function JobAlertsPage() {
  const toast = useToast();
  const [alerts, setAlerts] = useState<JobAlert[]>([]);
  const [loading, setLoading] = useState(true);
  const [form, setForm] = useState<AlertFormState>(EMPTY_FORM);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadAlerts();
  }, []);

  async function loadAlerts() {
    setLoading(true);
    try {
      setAlerts(await getMyAlerts());
    } finally {
      setLoading(false);
    }
  }

  function startEdit(alert: JobAlert) {
    setEditingId(alert.id);
    setForm({
      skillsCsv: alert.skillsCsv ?? "",
      state: alert.state ?? "",
      city: alert.city ?? "",
      isRemote: alert.isRemote ?? false,
      jobType: (alert.jobType as JobTypeValue) ?? "",
      minExperienceYears: alert.minExperienceYears?.toString() ?? "",
    });
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function cancelEdit() {
    setEditingId(null);
    setForm(EMPTY_FORM);
    setError(null);
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSaving(true);
    try {
      const payload = {
        skillsCsv: form.skillsCsv || undefined,
        state: form.isRemote ? undefined : form.state || undefined,
        city: form.isRemote ? undefined : form.city || undefined,
        isRemote: form.isRemote || undefined,
        jobType: form.jobType || undefined,
        minExperienceYears: form.minExperienceYears ? Number(form.minExperienceYears) : undefined,
        isActive: true,
      };
      if (editingId) {
        await updateAlert(editingId, payload);
        toast.success("Alert updated.");
      } else {
        await createAlert(payload);
        toast.success("Alert created.");
      }
      cancelEdit();
      await loadAlerts();
    } catch (err) {
      setError(getErrorMessage(err, editingId ? "Failed to update alert" : "Failed to create alert"));
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleActive(alert: JobAlert) {
    try {
      const updated = await setAlertActive(alert.id, !alert.isActive);
      setAlerts((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      toast.success(updated.isActive ? "Alert activated." : "Alert deactivated.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update alert"));
    }
  }

  async function handleDelete(alertId: number) {
    try {
      await deleteAlert(alertId);
      setAlerts((prev) => prev.filter((a) => a.id !== alertId));
      toast.success("Alert deleted.");
      if (editingId === alertId) cancelEdit();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to delete alert"));
    }
  }

  function describeAlert(alert: JobAlert): string {
    const parts: string[] = [];
    if (alert.skillsCsv) parts.push(alert.skillsCsv);
    if (alert.isRemote) parts.push("Remote — India");
    else if (alert.city) parts.push(`${alert.city}${alert.state ? `, ${alert.state}` : ""}`);
    else if (alert.state) parts.push(alert.state);
    if (alert.jobType) parts.push(alert.jobType);
    if (alert.minExperienceYears) parts.push(`${alert.minExperienceYears}+ yrs`);
    return parts.length > 0 ? parts.join(" · ") : "Any job";
  }

  return (
    <div style={{ maxWidth: 720, margin: "0 auto", display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div className="page-header">
        <h1><Bell size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Job Alerts</h1>
        <p>Get notified in-app when new roles match your criteria — checked live, no email involved.</p>
      </div>

      <Card className="ui-card-padded">
        <form onSubmit={handleSubmit} noValidate>
          <h3 className="form-section-title">{editingId ? "Edit alert" : "Create a new alert"}</h3>

          <FormField label="Skills" htmlFor="alert-skills" hint="Comma separated">
            <input id="alert-skills" value={form.skillsCsv} onChange={(e) => setForm((f) => ({ ...f, skillsCsv: e.target.value }))} placeholder="C#, SQL Server" />
          </FormField>

          <IndiaLocationSelector
            state={form.state}
            city={form.city}
            locality=""
            isRemote={form.isRemote}
            onStateChange={(state) => setForm((f) => ({ ...f, state }))}
            onCityChange={(city) => setForm((f) => ({ ...f, city }))}
            onLocalityChange={() => {}}
            onIsRemoteChange={(isRemote) => setForm((f) => ({ ...f, isRemote }))}
          />

          <FormField label="Job type" htmlFor="alert-job-type">
            <select id="alert-job-type" value={form.jobType} onChange={(e) => setForm((f) => ({ ...f, jobType: e.target.value as JobTypeValue | "" }))}>
              <option value="">Any</option>
              {JOB_TYPE_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </FormField>

          <FormField label="Minimum experience (years)" htmlFor="alert-min-exp">
            <input
              id="alert-min-exp"
              type="number"
              min={0}
              max={40}
              value={form.minExperienceYears}
              onChange={(e) => setForm((f) => ({ ...f, minExperienceYears: e.target.value }))}
            />
          </FormField>

          {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

          <div className="form-actions">
            <Button type="submit" loading={saving}>{editingId ? "Save changes" : "Create alert"}</Button>
            {editingId && (
              <Button type="button" variant="secondary" onClick={cancelEdit}>Cancel</Button>
            )}
          </div>
        </form>
      </Card>

      <Card>
        <h2>Your alerts</h2>
        {loading ? (
          <p>Loading...</p>
        ) : alerts.length === 0 ? (
          <EmptyState icon={<Bell size={28} />} title="No alerts yet" description="Create one above to get matched to new roles automatically." />
        ) : (
          <ul className="job-list-compact">
            {alerts.map((alert) => (
              <li key={alert.id} className="job-card job-card-compact">
                <h4>
                  {describeAlert(alert)}
                  {!alert.isActive && <span className="sample-data-badge" style={{ marginLeft: "0.5rem" }}>Paused</span>}
                </h4>
                <p>
                  {alert.isActive
                    ? `${alert.matchingJobCount} matching ${alert.matchingJobCount === 1 ? "job" : "jobs"} right now`
                    : "Paused — not checking for matches"}
                </p>

                {alert.matchingJobs.length > 0 && (
                  <ul style={{ marginTop: "0.5rem", paddingLeft: "1.1rem" }}>
                    {alert.matchingJobs.map((job) => (
                      <li key={job.id}>
                        <Link to={`/jobs/${job.id}`}>{job.title}</Link> — {job.companyName}
                      </li>
                    ))}
                  </ul>
                )}

                <div style={{ display: "flex", gap: "1rem", marginTop: "0.5rem" }}>
                  <button type="button" className="link-button" onClick={() => startEdit(alert)}>
                    <Pencil size={14} style={{ verticalAlign: "-2px", marginRight: "0.25rem" }} />Edit
                  </button>
                  <button type="button" className="link-button" onClick={() => handleToggleActive(alert)}>
                    {alert.isActive ? "Deactivate" : "Activate"}
                  </button>
                  <button type="button" className="link-button" onClick={() => handleDelete(alert.id)}>
                    Delete
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
