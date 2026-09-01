import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { Copy, FileStack, Pencil, Plus, Trash2 } from "lucide-react";
import {
  createJobTemplate,
  createJobFromTemplate,
  deleteJobTemplate,
  duplicateJobTemplate,
  getMyJobTemplates,
  updateJobTemplate,
} from "../api/jobTemplates";
import { jobTypeToNumber, type JobTypeValue } from "../api/jobs";
import type { JobTemplate } from "../types";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import Button from "../components/ui/Button";
import Modal from "../components/ui/Modal";
import FormField from "../components/ui/FormField";
import EmptyState from "../components/ui/EmptyState";

interface FieldErrors {
  title?: string;
  description?: string;
  general?: string;
}

const JOB_TYPE_OPTIONS: { value: JobTypeValue; label: string }[] = [
  { value: "FullTime", label: "Full-time" },
  { value: "PartTime", label: "Part-time" },
  { value: "Contract", label: "Contract" },
  { value: "Internship", label: "Internship" },
  { value: "Freelance", label: "Freelance" },
];

function TemplateForm({
  initial,
  onCancel,
  onSaved,
}: {
  initial: JobTemplate | null;
  onCancel: () => void;
  onSaved: (t: JobTemplate) => void;
}) {
  const [title, setTitle] = useState(initial?.title ?? "");
  const [department, setDepartment] = useState(initial?.department ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [responsibilities, setResponsibilities] = useState(initial?.responsibilities ?? "");
  const [requiredSkillsCsv, setRequiredSkillsCsv] = useState(initial?.requiredSkillsCsv ?? "");
  const [preferredSkillsCsv, setPreferredSkillsCsv] = useState(initial?.preferredSkillsCsv ?? "");
  const [minExperienceYears, setMinExperienceYears] = useState(initial?.minExperienceYears?.toString() ?? "");
  const [maxExperienceYears, setMaxExperienceYears] = useState(initial?.maxExperienceYears?.toString() ?? "");
  const [employmentType, setEmploymentType] = useState<JobTypeValue>((initial?.employmentType as JobTypeValue) ?? "FullTime");
  const [salaryVisible, setSalaryVisible] = useState(initial?.salaryVisible ?? false);
  const [minSalary, setMinSalary] = useState(initial?.minSalary?.toString() ?? "");
  const [maxSalary, setMaxSalary] = useState(initial?.maxSalary?.toString() ?? "");
  const [defaultCity, setDefaultCity] = useState(initial?.defaultCity ?? "");
  const [defaultState, setDefaultState] = useState(initial?.defaultState ?? "");
  const [defaultIsRemote, setDefaultIsRemote] = useState(initial?.defaultIsRemote ?? false);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const toast = useToast();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const clientErrors: FieldErrors = {};
    if (!title.trim()) clientErrors.title = "Title is required.";
    if (!description.trim()) clientErrors.description = "Description is required.";
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setSaving(true);
    try {
      const payload = {
        title: title.trim(),
        department: department || undefined,
        description,
        responsibilities: responsibilities || undefined,
        requiredSkillsCsv: requiredSkillsCsv || undefined,
        preferredSkillsCsv: preferredSkillsCsv || undefined,
        minExperienceYears: minExperienceYears ? Number(minExperienceYears) : undefined,
        maxExperienceYears: maxExperienceYears ? Number(maxExperienceYears) : undefined,
        employmentType: jobTypeToNumber[employmentType],
        salaryVisible,
        minSalary: salaryVisible && minSalary ? Number(minSalary) : undefined,
        maxSalary: salaryVisible && maxSalary ? Number(maxSalary) : undefined,
        defaultCity: defaultIsRemote ? undefined : defaultCity || undefined,
        defaultState: defaultIsRemote ? undefined : defaultState || undefined,
        defaultIsRemote,
      };
      const saved = initial ? await updateJobTemplate(initial.id, payload) : await createJobTemplate(payload);
      toast.success(initial ? "Template updated." : "Template created.");
      onSaved(saved);
    } catch (err) {
      const serverFieldErrors = getFieldErrors(err);
      if (serverFieldErrors) {
        setFieldErrors(serverFieldErrors as FieldErrors);
      } else {
        setFieldErrors({ general: getErrorMessage(err, "Failed to save template") });
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} noValidate>
      <FormField label="Title" htmlFor="tpl-title" required error={fieldErrors.title}>
        <input id="tpl-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Backend Engineer" />
      </FormField>
      <FormField label="Department" htmlFor="tpl-department">
        <input id="tpl-department" value={department} onChange={(e) => setDepartment(e.target.value)} placeholder="e.g. Engineering" />
      </FormField>
      <FormField label="Description" htmlFor="tpl-description" required error={fieldErrors.description}>
        <textarea id="tpl-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={4} />
      </FormField>
      <FormField label="Responsibilities" htmlFor="tpl-responsibilities">
        <textarea id="tpl-responsibilities" value={responsibilities} onChange={(e) => setResponsibilities(e.target.value)} rows={3} />
      </FormField>
      <FormField label="Required skills" htmlFor="tpl-required-skills" hint="Comma separated">
        <input id="tpl-required-skills" value={requiredSkillsCsv} onChange={(e) => setRequiredSkillsCsv(e.target.value)} />
      </FormField>
      <FormField label="Preferred skills" htmlFor="tpl-preferred-skills" hint="Comma separated">
        <input id="tpl-preferred-skills" value={preferredSkillsCsv} onChange={(e) => setPreferredSkillsCsv(e.target.value)} />
      </FormField>
      <div className="form-row">
        <FormField label="Min experience (years)" htmlFor="tpl-min-exp">
          <input id="tpl-min-exp" type="number" min={0} value={minExperienceYears} onChange={(e) => setMinExperienceYears(e.target.value)} />
        </FormField>
        <FormField label="Max experience (years)" htmlFor="tpl-max-exp">
          <input id="tpl-max-exp" type="number" min={0} value={maxExperienceYears} onChange={(e) => setMaxExperienceYears(e.target.value)} />
        </FormField>
      </div>
      <FormField label="Employment type" htmlFor="tpl-employment-type">
        <select id="tpl-employment-type" value={employmentType} onChange={(e) => setEmploymentType(e.target.value as JobTypeValue)}>
          {JOB_TYPE_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
        </select>
      </FormField>
      <label className="filter-option" style={{ marginBottom: "1rem" }}>
        <input type="checkbox" checked={salaryVisible} onChange={(e) => setSalaryVisible(e.target.checked)} />
        Show a default salary range
      </label>
      {salaryVisible && (
        <div className="form-row">
          <FormField label="Min salary" htmlFor="tpl-min-salary">
            <input id="tpl-min-salary" type="number" min={0} value={minSalary} onChange={(e) => setMinSalary(e.target.value)} />
          </FormField>
          <FormField label="Max salary" htmlFor="tpl-max-salary">
            <input id="tpl-max-salary" type="number" min={0} value={maxSalary} onChange={(e) => setMaxSalary(e.target.value)} />
          </FormField>
        </div>
      )}
      <label className="filter-option" style={{ marginBottom: "1rem" }}>
        <input type="checkbox" checked={defaultIsRemote} onChange={(e) => setDefaultIsRemote(e.target.checked)} />
        Default to Remote — India
      </label>
      {!defaultIsRemote && (
        <div className="form-row">
          <FormField label="Default state" htmlFor="tpl-state">
            <input id="tpl-state" value={defaultState} onChange={(e) => setDefaultState(e.target.value)} />
          </FormField>
          <FormField label="Default city" htmlFor="tpl-city">
            <input id="tpl-city" value={defaultCity} onChange={(e) => setDefaultCity(e.target.value)} />
          </FormField>
        </div>
      )}

      {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

      <div className="form-actions">
        <Button type="submit" loading={saving}>{initial ? "Save changes" : "Create template"}</Button>
        <Button type="button" variant="secondary" onClick={onCancel}>Cancel</Button>
      </div>
    </form>
  );
}

export default function RecruiterJobTemplatesPage() {
  const toast = useToast();
  const navigate = useNavigate();
  const [templates, setTemplates] = useState<JobTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [editorOpen, setEditorOpen] = useState(false);
  const [editing, setEditing] = useState<JobTemplate | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<JobTemplate | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);

  useEffect(() => {
    void load();
  }, []);

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setTemplates(await getMyJobTemplates());
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load templates"));
    } finally {
      setLoading(false);
    }
  }

  const filtered = search.trim()
    ? templates.filter((t) => t.title.toLowerCase().includes(search.trim().toLowerCase()) || (t.department ?? "").toLowerCase().includes(search.trim().toLowerCase()))
    : templates;

  function openCreate() {
    setEditing(null);
    setEditorOpen(true);
  }

  function openEdit(t: JobTemplate) {
    setEditing(t);
    setEditorOpen(true);
  }

  function handleSaved(t: JobTemplate) {
    setEditorOpen(false);
    setTemplates((prev) => {
      const exists = prev.some((p) => p.id === t.id);
      return exists ? prev.map((p) => (p.id === t.id ? t : p)) : [t, ...prev];
    });
  }

  async function handleDuplicate(t: JobTemplate) {
    setBusyId(t.id);
    try {
      const copy = await duplicateJobTemplate(t.id);
      setTemplates((prev) => [copy, ...prev]);
      toast.success("Template duplicated.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to duplicate template"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleUseTemplate(t: JobTemplate) {
    setBusyId(t.id);
    try {
      const job = await createJobFromTemplate(t.id);
      toast.success("Draft job created from template.");
      navigate(`/jobs/${job.id}/edit`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to create job from template"));
    } finally {
      setBusyId(null);
    }
  }

  async function handleDelete() {
    if (!deleteTarget) return;
    setBusyId(deleteTarget.id);
    try {
      await deleteJobTemplate(deleteTarget.id);
      setTemplates((prev) => prev.filter((p) => p.id !== deleteTarget.id));
      toast.success("Template deleted.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to delete template"));
    } finally {
      setBusyId(null);
      setDeleteTarget(null);
    }
  }

  return (
    <div>
      <div className="page-header">
        <h1><FileStack size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Job Templates</h1>
        <p>Reusable starting points for your team's job postings.</p>
      </div>

      <div className="jobs-search-bar" style={{ marginBottom: "1.5rem" }}>
        <div className="jobs-search-field">
          <input placeholder="Search templates" value={search} onChange={(e) => setSearch(e.target.value)} aria-label="Search templates" />
        </div>
        <Button onClick={openCreate} icon={<Plus size={16} />}>New Template</Button>
      </div>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      {loading ? (
        <p>Loading...</p>
      ) : filtered.length === 0 ? (
        <EmptyState
          icon={<FileStack size={32} />}
          title={templates.length === 0 ? "No templates yet" : "No templates match your search"}
          description={templates.length === 0 ? "Create a template, or save an existing job as one from Manage Jobs." : "Try a different search term."}
          action={templates.length === 0 ? <Button onClick={openCreate}>New Template</Button> : undefined}
        />
      ) : (
        <ul className="job-list">
          {filtered.map((t) => (
            <li key={t.id} className="job-card">
              <div className="job-card-header">
                <div>
                  <h3>{t.title}</h3>
                  <p className="hint">{t.department ?? "No department"} · Created by {t.createdByName}</p>
                </div>
              </div>
              <p>{t.description}</p>
              {t.requiredSkillsCsv && (
                <div className="chip-list">
                  {t.requiredSkillsCsv.split(",").map((s) => s.trim()).filter(Boolean).map((s) => (
                    <span key={s} className="chip">{s}</span>
                  ))}
                </div>
              )}
              <div className="form-actions" style={{ marginTop: "0.75rem" }}>
                <Button size="sm" loading={busyId === t.id} onClick={() => handleUseTemplate(t)}>Use Template</Button>
                <Button size="sm" variant="secondary" icon={<Copy size={14} />} loading={busyId === t.id} onClick={() => handleDuplicate(t)}>Duplicate</Button>
                {t.canManage && (
                  <>
                    <Button size="sm" variant="ghost" icon={<Pencil size={14} />} onClick={() => openEdit(t)}>Edit</Button>
                    <Button size="sm" variant="danger" icon={<Trash2 size={14} />} onClick={() => setDeleteTarget(t)}>Delete</Button>
                  </>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      <Modal open={editorOpen} onClose={() => setEditorOpen(false)} title={editing ? "Edit Template" : "New Template"}>
        <TemplateForm initial={editing} onCancel={() => setEditorOpen(false)} onSaved={handleSaved} />
      </Modal>

      <Modal
        open={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        title="Delete template?"
        footer={
          <>
            <Button variant="danger" loading={busyId === deleteTarget?.id} onClick={handleDelete}>Delete</Button>
            <Button variant="secondary" onClick={() => setDeleteTarget(null)}>Cancel</Button>
          </>
        }
      >
        <p>This will permanently delete "{deleteTarget?.title}". This cannot be undone.</p>
      </Modal>
    </div>
  );
}
