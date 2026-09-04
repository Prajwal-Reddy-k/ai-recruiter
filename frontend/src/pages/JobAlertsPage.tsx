import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { Bell, Copy, Pencil, Star } from "lucide-react";
import { createAlert, deleteAlert, duplicateAlert, getMyAlerts, setAlertActive, setDefaultAlert, updateAlert } from "../api/savedJobs";
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

const SORT_OPTIONS = [
  { value: "Newest", label: "Newest first" },
  { value: "SalaryHigh", label: "Salary: high to low" },
  { value: "ExperienceLow", label: "Experience: low to high" },
];

interface AlertFormState {
  name: string;
  keyword: string;
  skillsCsv: string;
  state: string;
  city: string;
  isRemote: boolean;
  jobType: JobTypeValue | "";
  minExperienceYears: string;
  minSalary: string;
  maxSalary: string;
  sortOption: string;
}

const EMPTY_FORM: AlertFormState = {
  name: "", keyword: "", skillsCsv: "", state: "", city: "", isRemote: false,
  jobType: "", minExperienceYears: "", minSalary: "", maxSalary: "", sortOption: "",
};

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
      name: alert.name ?? "",
      keyword: alert.keyword ?? "",
      skillsCsv: alert.skillsCsv ?? "",
      state: alert.state ?? "",
      city: alert.city ?? "",
      isRemote: alert.isRemote ?? false,
      jobType: (alert.jobType as JobTypeValue) ?? "",
      minExperienceYears: alert.minExperienceYears?.toString() ?? "",
      minSalary: alert.minSalary?.toString() ?? "",
      maxSalary: alert.maxSalary?.toString() ?? "",
      sortOption: alert.sortOption ?? "",
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
        name: form.name || undefined,
        keyword: form.keyword || undefined,
        skillsCsv: form.skillsCsv || undefined,
        state: form.isRemote ? undefined : form.state || undefined,
        city: form.isRemote ? undefined : form.city || undefined,
        isRemote: form.isRemote || undefined,
        jobType: form.jobType || undefined,
        minExperienceYears: form.minExperienceYears ? Number(form.minExperienceYears) : undefined,
        minSalary: form.minSalary ? Number(form.minSalary) : undefined,
        maxSalary: form.maxSalary ? Number(form.maxSalary) : undefined,
        sortOption: form.sortOption || undefined,
        isActive: true,
      };
      if (editingId) {
        await updateAlert(editingId, payload);
        toast.success("Saved search updated.");
      } else {
        await createAlert(payload);
        toast.success("Saved search created.");
      }
      cancelEdit();
      await loadAlerts();
    } catch (err) {
      setError(getErrorMessage(err, editingId ? "Failed to update saved search" : "Failed to create saved search"));
    } finally {
      setSaving(false);
    }
  }

  async function handleToggleActive(alert: JobAlert) {
    try {
      const updated = await setAlertActive(alert.id, !alert.isActive);
      setAlerts((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      toast.success(updated.isActive ? "Saved search activated." : "Saved search deactivated.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update saved search"));
    }
  }

  async function handleDelete(alertId: number) {
    try {
      await deleteAlert(alertId);
      setAlerts((prev) => prev.filter((a) => a.id !== alertId));
      toast.success("Saved search deleted.");
      if (editingId === alertId) cancelEdit();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to delete saved search"));
    }
  }

  async function handleDuplicate(alertId: number) {
    try {
      await duplicateAlert(alertId);
      toast.success("Saved search duplicated.");
      await loadAlerts();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to duplicate saved search"));
    }
  }

  async function handleSetDefault(alertId: number) {
    try {
      await setDefaultAlert(alertId);
      toast.success("Set as your default dashboard search.");
      await loadAlerts();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to set default saved search"));
    }
  }

  function summaryChips(alert: JobAlert): string[] {
    const chips: string[] = [];
    if (alert.keyword) chips.push(`"${alert.keyword}"`);
    if (alert.skillsCsv) chips.push(alert.skillsCsv);
    if (alert.isRemote) chips.push("Remote — India");
    else if (alert.city) chips.push(`${alert.city}${alert.state ? `, ${alert.state}` : ""}`);
    else if (alert.state) chips.push(alert.state);
    if (alert.jobType) chips.push(alert.jobType);
    if (alert.minExperienceYears) chips.push(`${alert.minExperienceYears}+ yrs`);
    if (alert.minSalary || alert.maxSalary) {
      chips.push(`₹${alert.minSalary?.toLocaleString("en-IN") ?? "0"} – ₹${alert.maxSalary?.toLocaleString("en-IN") ?? "any"}`);
    }
    return chips;
  }

  return (
    <div style={{ maxWidth: 720, margin: "0 auto", display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div className="page-header">
        <h1><Bell size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Saved Searches</h1>
        <p>Save a complete search, get matched to new roles automatically, and pick one as your default dashboard search.</p>
      </div>

      <Card className="ui-card-padded">
        <form onSubmit={handleSubmit} noValidate>
          <h3 className="form-section-title">{editingId ? "Edit saved search" : "Create a new saved search"}</h3>

          <FormField label="Name" htmlFor="alert-name" hint="Optional — helps you tell searches apart.">
            <input id="alert-name" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} placeholder="e.g. Bengaluru Backend Roles" />
          </FormField>

          <FormField label="Keyword" htmlFor="alert-keyword" hint="Matches job title or description">
            <input id="alert-keyword" value={form.keyword} onChange={(e) => setForm((f) => ({ ...f, keyword: e.target.value }))} placeholder="e.g. backend" />
          </FormField>

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

          <div className="form-row">
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
          </div>

          <div className="form-row">
            <FormField label="Minimum salary" htmlFor="alert-min-salary">
              <input id="alert-min-salary" type="number" min={0} value={form.minSalary} onChange={(e) => setForm((f) => ({ ...f, minSalary: e.target.value }))} />
            </FormField>
            <FormField label="Maximum salary" htmlFor="alert-max-salary">
              <input id="alert-max-salary" type="number" min={0} value={form.maxSalary} onChange={(e) => setForm((f) => ({ ...f, maxSalary: e.target.value }))} />
            </FormField>
          </div>

          <FormField label="Sort matches by" htmlFor="alert-sort">
            <select id="alert-sort" value={form.sortOption} onChange={(e) => setForm((f) => ({ ...f, sortOption: e.target.value }))}>
              <option value="">Default</option>
              {SORT_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </FormField>

          {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

          <div className="form-actions">
            <Button type="submit" loading={saving}>{editingId ? "Save changes" : "Create saved search"}</Button>
            {editingId && (
              <Button type="button" variant="secondary" onClick={cancelEdit}>Cancel</Button>
            )}
          </div>
        </form>
      </Card>

      <Card>
        <h2>Your saved searches</h2>
        {loading ? (
          <p>Loading...</p>
        ) : alerts.length === 0 ? (
          <EmptyState icon={<Bell size={28} />} title="No saved searches yet" description="Create one above to get matched to new roles automatically." />
        ) : (
          <ul className="job-list-compact">
            {alerts.map((alert) => (
              <li key={alert.id} className="job-card job-card-compact">
                <h4>
                  {alert.name || "Untitled search"}
                  {alert.isDefault && <span className="chip" style={{ marginLeft: "0.5rem" }}><Star size={11} style={{ verticalAlign: "-1px" }} /> Default</span>}
                  {!alert.isActive && <span className="sample-data-badge" style={{ marginLeft: "0.5rem" }}>Paused</span>}
                </h4>
                <div className="chip-list" style={{ margin: "0.4rem 0" }}>
                  {summaryChips(alert).map((chip) => <span key={chip} className="chip">{chip}</span>)}
                </div>
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

                <div style={{ display: "flex", gap: "1rem", marginTop: "0.5rem", flexWrap: "wrap" }}>
                  <button type="button" className="link-button" onClick={() => startEdit(alert)}>
                    <Pencil size={14} style={{ verticalAlign: "-2px", marginRight: "0.25rem" }} />Edit
                  </button>
                  <button type="button" className="link-button" onClick={() => handleDuplicate(alert.id)}>
                    <Copy size={14} style={{ verticalAlign: "-2px", marginRight: "0.25rem" }} />Duplicate
                  </button>
                  {!alert.isDefault && (
                    <button type="button" className="link-button" onClick={() => handleSetDefault(alert.id)}>
                      Set as default
                    </button>
                  )}
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
