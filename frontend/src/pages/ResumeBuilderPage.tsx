import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { ArrowDown, ArrowUp, Download, Eye, FileText, Pencil, Plus, Trash2 } from "lucide-react";
import {
  getMyResume, upsertResumeSummary,
  addExperience, updateExperience, deleteExperience, reorderExperience,
  addEducation, updateEducation, deleteEducation, reorderEducation,
  addCertification, updateCertification, deleteCertification, reorderCertification,
  addProject, updateProject, deleteProject, reorderProject,
} from "../api/resumeBuilder";
import { generateResumePdf } from "../utils/resumePdf";
import { saveBlobAsFile } from "../utils/download";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type {
  Resume, WorkExperience, EducationEntry, Certification, ResumeProject,
} from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import Modal from "../components/ui/Modal";
import ConfirmDialog from "../components/ui/ConfirmDialog";

function dateInput(value: string | null): string {
  return value ? value.slice(0, 10) : "";
}

function StrengthMeter({ resume }: { resume: Resume }) {
  return (
    <Card className="ui-card-padded">
      <h2>Profile strength</h2>
      <div className="progress-bar" style={{ margin: "0.75rem 0" }}>
        <div className="progress-bar-fill" style={{ width: `${resume.strength.score}%` }} />
      </div>
      <p className="hint" style={{ marginBottom: "0.75rem" }}>{resume.strength.score}% complete</p>
      {resume.strength.missingItems.length > 0 && (
        <ul className="job-list-compact">
          {resume.strength.missingItems.map((item) => (
            <li key={item.label} className="job-card job-card-compact">
              <h4>{item.label}</h4>
              <p className="hint">{item.tip}</p>
              {item.linkPath !== "/resume-builder" && <Link to={item.linkPath}>Go there →</Link>}
            </li>
          ))}
        </ul>
      )}
    </Card>
  );
}

function SummaryLinksCard({ resume, onSaved }: { resume: Resume; onSaved: (r: Resume) => void }) {
  const toast = useToast();
  const [summary, setSummary] = useState(resume.summary ?? "");
  const [skillsCsv, setSkillsCsv] = useState(resume.skillsCsv ?? "");
  const [linkedInUrl, setLinkedInUrl] = useState(resume.linkedInUrl ?? "");
  const [githubUrl, setGithubUrl] = useState(resume.githubUrl ?? "");
  const [portfolioUrl, setPortfolioUrl] = useState(resume.portfolioUrl ?? "");
  const [achievementsText, setAchievementsText] = useState(resume.achievementsText ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      const updated = await upsertResumeSummary({
        summary: summary || undefined,
        skillsCsv: skillsCsv || undefined,
        linkedInUrl: linkedInUrl || undefined,
        githubUrl: githubUrl || undefined,
        portfolioUrl: portfolioUrl || undefined,
        achievementsText: achievementsText || undefined,
      });
      onSaved(updated);
      toast.success("Resume summary saved.");
    } catch (err) {
      const fieldErrors = getFieldErrors(err);
      if (fieldErrors) setErrors(fieldErrors);
      else toast.error(getErrorMessage(err, "Failed to save"));
    } finally {
      setSaving(false);
    }
  }

  return (
    <Card className="ui-card-padded">
      <h2>Summary, skills & links</h2>
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Professional summary" htmlFor="rb-summary" error={errors.summary}>
          <textarea id="rb-summary" rows={4} value={summary} onChange={(e) => setSummary(e.target.value)} />
        </FormField>
        <FormField label="Skills" htmlFor="rb-skills" hint="Comma separated — shared with your main profile.">
          <input id="rb-skills" value={skillsCsv} onChange={(e) => setSkillsCsv(e.target.value)} />
        </FormField>
        <div className="form-row">
          <FormField label="LinkedIn URL" htmlFor="rb-linkedin" error={errors.linkedInUrl}>
            <input id="rb-linkedin" value={linkedInUrl} onChange={(e) => setLinkedInUrl(e.target.value)} placeholder="https://linkedin.com/in/..." />
          </FormField>
          <FormField label="GitHub URL" htmlFor="rb-github" error={errors.githubUrl}>
            <input id="rb-github" value={githubUrl} onChange={(e) => setGithubUrl(e.target.value)} placeholder="https://github.com/..." />
          </FormField>
        </div>
        <FormField label="Portfolio URL" htmlFor="rb-portfolio" error={errors.portfolioUrl}>
          <input id="rb-portfolio" value={portfolioUrl} onChange={(e) => setPortfolioUrl(e.target.value)} placeholder="https://..." />
        </FormField>
        <FormField label="Achievements" htmlFor="rb-achievements" hint="One per line." error={errors.achievementsText}>
          <textarea id="rb-achievements" rows={3} value={achievementsText} onChange={(e) => setAchievementsText(e.target.value)} />
        </FormField>
        <div className="form-actions">
          <Button type="submit" loading={saving}>Save</Button>
        </div>
      </form>
    </Card>
  );
}

// --- Experience ---

function ExperienceModal({ initial, onClose, onSave }: { initial: WorkExperience | null; onClose: () => void; onSave: (data: { title: string; company: string; location?: string; startDate: string; endDate?: string | null; description?: string }) => Promise<void> }) {
  const [title, setTitle] = useState(initial?.title ?? "");
  const [company, setCompany] = useState(initial?.company ?? "");
  const [location, setLocation] = useState(initial?.location ?? "");
  const [startDate, setStartDate] = useState(dateInput(initial?.startDate ?? null));
  const [endDate, setEndDate] = useState(dateInput(initial?.endDate ?? null));
  const [current, setCurrent] = useState(!initial?.endDate && !!initial);
  const [description, setDescription] = useState(initial?.description ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      await onSave({ title, company, location: location || undefined, startDate, endDate: current ? null : (endDate || null), description: description || undefined });
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe); else setErrors({ general: getErrorMessage(err, "Failed to save") });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal open onClose={onClose} title={initial ? "Edit experience" : "Add experience"} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save</Button></>}>
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Job title" htmlFor="exp-title" required error={errors.title}><input id="exp-title" value={title} onChange={(e) => setTitle(e.target.value)} /></FormField>
        <FormField label="Company" htmlFor="exp-company" required error={errors.company}><input id="exp-company" value={company} onChange={(e) => setCompany(e.target.value)} /></FormField>
        <FormField label="Location" htmlFor="exp-location" error={errors.location}><input id="exp-location" value={location} onChange={(e) => setLocation(e.target.value)} /></FormField>
        <div className="form-row">
          <FormField label="Start date" htmlFor="exp-start" required><input id="exp-start" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></FormField>
          <FormField label="End date" htmlFor="exp-end" error={errors.endDate}>
            <input id="exp-end" type="date" value={endDate} disabled={current} onChange={(e) => setEndDate(e.target.value)} />
          </FormField>
        </div>
        <label className="filter-option"><input type="checkbox" checked={current} onChange={(e) => setCurrent(e.target.checked)} /> I currently work here</label>
        <FormField label="Description" htmlFor="exp-desc" error={errors.description}><textarea id="exp-desc" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} /></FormField>
        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}

// --- Education ---

function EducationModal({ initial, onClose, onSave }: { initial: EducationEntry | null; onClose: () => void; onSave: (data: { institution: string; degree: string; fieldOfStudy?: string; startDate?: string | null; endDate?: string | null; gradeOrGpa?: string; description?: string }) => Promise<void> }) {
  const [institution, setInstitution] = useState(initial?.institution ?? "");
  const [degree, setDegree] = useState(initial?.degree ?? "");
  const [fieldOfStudy, setFieldOfStudy] = useState(initial?.fieldOfStudy ?? "");
  const [startDate, setStartDate] = useState(dateInput(initial?.startDate ?? null));
  const [endDate, setEndDate] = useState(dateInput(initial?.endDate ?? null));
  const [gradeOrGpa, setGradeOrGpa] = useState(initial?.gradeOrGpa ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      await onSave({ institution, degree, fieldOfStudy: fieldOfStudy || undefined, startDate: startDate || null, endDate: endDate || null, gradeOrGpa: gradeOrGpa || undefined, description: description || undefined });
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe); else setErrors({ general: getErrorMessage(err, "Failed to save") });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal open onClose={onClose} title={initial ? "Edit education" : "Add education"} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save</Button></>}>
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Institution" htmlFor="edu-institution" required error={errors.institution}><input id="edu-institution" value={institution} onChange={(e) => setInstitution(e.target.value)} /></FormField>
        <FormField label="Degree" htmlFor="edu-degree" required error={errors.degree}><input id="edu-degree" value={degree} onChange={(e) => setDegree(e.target.value)} /></FormField>
        <FormField label="Field of study" htmlFor="edu-field" error={errors.fieldOfStudy}><input id="edu-field" value={fieldOfStudy} onChange={(e) => setFieldOfStudy(e.target.value)} /></FormField>
        <div className="form-row">
          <FormField label="Start date" htmlFor="edu-start"><input id="edu-start" type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} /></FormField>
          <FormField label="End date" htmlFor="edu-end" error={errors.endDate}><input id="edu-end" type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} /></FormField>
        </div>
        <FormField label="Grade / GPA" htmlFor="edu-grade" error={errors.gradeOrGpa}><input id="edu-grade" value={gradeOrGpa} onChange={(e) => setGradeOrGpa(e.target.value)} /></FormField>
        <FormField label="Description" htmlFor="edu-desc" error={errors.description}><textarea id="edu-desc" rows={2} value={description} onChange={(e) => setDescription(e.target.value)} /></FormField>
        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}

// --- Certification ---

function CertificationModal({ initial, onClose, onSave }: { initial: Certification | null; onClose: () => void; onSave: (data: { name: string; issuingOrganization?: string; issueDate?: string | null; expiryDate?: string | null; credentialUrl?: string }) => Promise<void> }) {
  const [name, setName] = useState(initial?.name ?? "");
  const [issuingOrganization, setIssuingOrganization] = useState(initial?.issuingOrganization ?? "");
  const [issueDate, setIssueDate] = useState(dateInput(initial?.issueDate ?? null));
  const [expiryDate, setExpiryDate] = useState(dateInput(initial?.expiryDate ?? null));
  const [credentialUrl, setCredentialUrl] = useState(initial?.credentialUrl ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      await onSave({ name, issuingOrganization: issuingOrganization || undefined, issueDate: issueDate || null, expiryDate: expiryDate || null, credentialUrl: credentialUrl || undefined });
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe); else setErrors({ general: getErrorMessage(err, "Failed to save") });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal open onClose={onClose} title={initial ? "Edit certification" : "Add certification"} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save</Button></>}>
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Certification name" htmlFor="cert-name" required error={errors.name}><input id="cert-name" value={name} onChange={(e) => setName(e.target.value)} /></FormField>
        <FormField label="Issuing organization" htmlFor="cert-org" error={errors.issuingOrganization}><input id="cert-org" value={issuingOrganization} onChange={(e) => setIssuingOrganization(e.target.value)} /></FormField>
        <div className="form-row">
          <FormField label="Issue date" htmlFor="cert-issue"><input id="cert-issue" type="date" value={issueDate} onChange={(e) => setIssueDate(e.target.value)} /></FormField>
          <FormField label="Expiry date" htmlFor="cert-expiry" error={errors.expiryDate}><input id="cert-expiry" type="date" value={expiryDate} onChange={(e) => setExpiryDate(e.target.value)} /></FormField>
        </div>
        <FormField label="Credential URL" htmlFor="cert-url" error={errors.credentialUrl}><input id="cert-url" value={credentialUrl} onChange={(e) => setCredentialUrl(e.target.value)} /></FormField>
        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}

// --- Project ---

function ProjectModal({ initial, onClose, onSave }: { initial: ResumeProject | null; onClose: () => void; onSave: (data: { title: string; description?: string; projectUrl?: string; technologiesCsv?: string }) => Promise<void> }) {
  const [title, setTitle] = useState(initial?.title ?? "");
  const [description, setDescription] = useState(initial?.description ?? "");
  const [projectUrl, setProjectUrl] = useState(initial?.projectUrl ?? "");
  const [technologiesCsv, setTechnologiesCsv] = useState(initial?.technologiesCsv ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      await onSave({ title, description: description || undefined, projectUrl: projectUrl || undefined, technologiesCsv: technologiesCsv || undefined });
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe); else setErrors({ general: getErrorMessage(err, "Failed to save") });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal open onClose={onClose} title={initial ? "Edit project" : "Add project"} footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save</Button></>}>
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Project title" htmlFor="proj-title" required error={errors.title}><input id="proj-title" value={title} onChange={(e) => setTitle(e.target.value)} /></FormField>
        <FormField label="Project URL" htmlFor="proj-url" error={errors.projectUrl}><input id="proj-url" value={projectUrl} onChange={(e) => setProjectUrl(e.target.value)} /></FormField>
        <FormField label="Technologies" htmlFor="proj-tech" hint="Comma separated." error={errors.technologiesCsv}><input id="proj-tech" value={technologiesCsv} onChange={(e) => setTechnologiesCsv(e.target.value)} /></FormField>
        <FormField label="Description" htmlFor="proj-desc" error={errors.description}><textarea id="proj-desc" rows={3} value={description} onChange={(e) => setDescription(e.target.value)} /></FormField>
        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}

export default function ResumeBuilderPage() {
  const toast = useToast();
  const [resume, setResume] = useState<Resume | null>(null);
  const [loading, setLoading] = useState(true);
  const [preview, setPreview] = useState(false);

  const [editingExperience, setEditingExperience] = useState<WorkExperience | "new" | null>(null);
  const [editingEducation, setEditingEducation] = useState<EducationEntry | "new" | null>(null);
  const [editingCertification, setEditingCertification] = useState<Certification | "new" | null>(null);
  const [editingProject, setEditingProject] = useState<ResumeProject | "new" | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<{ type: string; id: number; label: string } | null>(null);

  useEffect(() => {
    getMyResume().then(setResume).catch((err) => toast.error(getErrorMessage(err, "Failed to load your resume"))).finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleDownloadPdf() {
    if (!resume) return;
    const blob = generateResumePdf(resume);
    saveBlobAsFile(blob, `${resume.fullName.replace(/\s+/g, "_")}_resume.pdf`);
  }

  async function handleDeleteConfirm() {
    if (!resume || !deleteTarget) return;
    try {
      let updated: Resume;
      switch (deleteTarget.type) {
        case "experience": updated = await deleteExperience(deleteTarget.id); break;
        case "education": updated = await deleteEducation(deleteTarget.id); break;
        case "certification": updated = await deleteCertification(deleteTarget.id); break;
        default: updated = await deleteProject(deleteTarget.id); break;
      }
      setResume(updated);
      toast.success("Removed.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to remove"));
    } finally {
      setDeleteTarget(null);
    }
  }

  async function moveExperience(id: number, direction: -1 | 1) {
    if (!resume) return;
    const ids = resume.workExperiences.map((e) => e.id);
    const idx = ids.indexOf(id);
    const next = idx + direction;
    if (next < 0 || next >= ids.length) return;
    [ids[idx], ids[next]] = [ids[next], ids[idx]];
    setResume(await reorderExperience({ orderedIds: ids }));
  }
  async function moveEducation(id: number, direction: -1 | 1) {
    if (!resume) return;
    const ids = resume.educations.map((e) => e.id);
    const idx = ids.indexOf(id);
    const next = idx + direction;
    if (next < 0 || next >= ids.length) return;
    [ids[idx], ids[next]] = [ids[next], ids[idx]];
    setResume(await reorderEducation({ orderedIds: ids }));
  }
  async function moveCertification(id: number, direction: -1 | 1) {
    if (!resume) return;
    const ids = resume.certifications.map((e) => e.id);
    const idx = ids.indexOf(id);
    const next = idx + direction;
    if (next < 0 || next >= ids.length) return;
    [ids[idx], ids[next]] = [ids[next], ids[idx]];
    setResume(await reorderCertification({ orderedIds: ids }));
  }
  async function moveProject(id: number, direction: -1 | 1) {
    if (!resume) return;
    const ids = resume.projects.map((e) => e.id);
    const idx = ids.indexOf(id);
    const next = idx + direction;
    if (next < 0 || next >= ids.length) return;
    [ids[idx], ids[next]] = [ids[next], ids[idx]];
    setResume(await reorderProject({ orderedIds: ids }));
  }

  if (loading) return <p>Loading...</p>;
  if (!resume) return <p className="error">Couldn't load your resume.</p>;

  return (
    <div>
      <PageHeader
        title="Resume Builder"
        subtitle="Build a structured resume from your profile — separate from any resume file you've uploaded."
        action={
          <div style={{ display: "flex", gap: "0.5rem" }}>
            <Button variant="secondary" icon={<Eye size={16} />} onClick={() => setPreview((v) => !v)}>{preview ? "Edit" : "Preview"}</Button>
            <Button icon={<Download size={16} />} onClick={handleDownloadPdf}>Download PDF</Button>
          </div>
        }
      />

      <StrengthMeter resume={resume} />

      {preview ? (
        <Card className="ui-card-padded" style={{ marginTop: "1.5rem" }}>
          <h1>{resume.fullName}</h1>
          {resume.headline && <p className="hint">{resume.headline}</p>}
          {resume.summary && <p style={{ marginTop: "1rem" }}>{resume.summary}</p>}
          {resume.skillsCsv && (
            <div className="chip-list" style={{ marginTop: "1rem" }}>
              {resume.skillsCsv.split(",").map((s) => s.trim()).filter(Boolean).map((s) => <span key={s} className="chip">{s}</span>)}
            </div>
          )}
          {resume.workExperiences.length > 0 && (
            <>
              <h3 style={{ marginTop: "1.5rem" }}>Work Experience</h3>
              {resume.workExperiences.map((e) => (
                <div key={e.id} style={{ marginTop: "0.75rem" }}>
                  <strong>{e.title} — {e.company}</strong>
                  <p className="hint">{dateInput(e.startDate)} – {e.endDate ? dateInput(e.endDate) : "Present"}</p>
                  {e.description && <p>{e.description}</p>}
                </div>
              ))}
            </>
          )}
          {resume.educations.length > 0 && (
            <>
              <h3 style={{ marginTop: "1.5rem" }}>Education</h3>
              {resume.educations.map((e) => (
                <div key={e.id} style={{ marginTop: "0.75rem" }}>
                  <strong>{e.degree} — {e.institution}</strong>
                </div>
              ))}
            </>
          )}
          {resume.projects.length > 0 && (
            <>
              <h3 style={{ marginTop: "1.5rem" }}>Projects</h3>
              {resume.projects.map((p) => <div key={p.id} style={{ marginTop: "0.5rem" }}><strong>{p.title}</strong></div>)}
            </>
          )}
        </Card>
      ) : (
        <div style={{ display: "flex", flexDirection: "column", gap: "1.5rem", marginTop: "1.5rem" }}>
          <SummaryLinksCard resume={resume} onSaved={setResume} />

          <Card className="ui-card-padded">
            <div className="section-header">
              <h2><FileText size={18} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />Work experience</h2>
              <Button size="sm" icon={<Plus size={14} />} onClick={() => setEditingExperience("new")}>Add</Button>
            </div>
            {resume.workExperiences.length === 0 ? <p className="hint">No experience added yet.</p> : (
              <ul className="job-list-compact">
                {resume.workExperiences.map((e, i) => (
                  <li key={e.id} className="job-card job-card-compact">
                    <h4>{e.title} — {e.company}</h4>
                    <p className="hint">{dateInput(e.startDate)} – {e.endDate ? dateInput(e.endDate) : "Present"}</p>
                    <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.4rem" }}>
                      <button type="button" className="link-button" onClick={() => moveExperience(e.id, -1)} disabled={i === 0} aria-label="Move up"><ArrowUp size={14} /></button>
                      <button type="button" className="link-button" onClick={() => moveExperience(e.id, 1)} disabled={i === resume.workExperiences.length - 1} aria-label="Move down"><ArrowDown size={14} /></button>
                      <button type="button" className="link-button" onClick={() => setEditingExperience(e)}><Pencil size={14} /> Edit</button>
                      <button type="button" className="link-button" onClick={() => setDeleteTarget({ type: "experience", id: e.id, label: e.title })}><Trash2 size={14} /> Remove</button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>

          <Card className="ui-card-padded">
            <div className="section-header">
              <h2>Education</h2>
              <Button size="sm" icon={<Plus size={14} />} onClick={() => setEditingEducation("new")}>Add</Button>
            </div>
            {resume.educations.length === 0 ? <p className="hint">No education added yet.</p> : (
              <ul className="job-list-compact">
                {resume.educations.map((e, i) => (
                  <li key={e.id} className="job-card job-card-compact">
                    <h4>{e.degree} — {e.institution}</h4>
                    <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.4rem" }}>
                      <button type="button" className="link-button" onClick={() => moveEducation(e.id, -1)} disabled={i === 0} aria-label="Move up"><ArrowUp size={14} /></button>
                      <button type="button" className="link-button" onClick={() => moveEducation(e.id, 1)} disabled={i === resume.educations.length - 1} aria-label="Move down"><ArrowDown size={14} /></button>
                      <button type="button" className="link-button" onClick={() => setEditingEducation(e)}><Pencil size={14} /> Edit</button>
                      <button type="button" className="link-button" onClick={() => setDeleteTarget({ type: "education", id: e.id, label: e.institution })}><Trash2 size={14} /> Remove</button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>

          <Card className="ui-card-padded">
            <div className="section-header">
              <h2>Certifications</h2>
              <Button size="sm" icon={<Plus size={14} />} onClick={() => setEditingCertification("new")}>Add</Button>
            </div>
            {resume.certifications.length === 0 ? <p className="hint">No certifications added yet.</p> : (
              <ul className="job-list-compact">
                {resume.certifications.map((c, i) => (
                  <li key={c.id} className="job-card job-card-compact">
                    <h4>{c.name}</h4>
                    <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.4rem" }}>
                      <button type="button" className="link-button" onClick={() => moveCertification(c.id, -1)} disabled={i === 0} aria-label="Move up"><ArrowUp size={14} /></button>
                      <button type="button" className="link-button" onClick={() => moveCertification(c.id, 1)} disabled={i === resume.certifications.length - 1} aria-label="Move down"><ArrowDown size={14} /></button>
                      <button type="button" className="link-button" onClick={() => setEditingCertification(c)}><Pencil size={14} /> Edit</button>
                      <button type="button" className="link-button" onClick={() => setDeleteTarget({ type: "certification", id: c.id, label: c.name })}><Trash2 size={14} /> Remove</button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>

          <Card className="ui-card-padded">
            <div className="section-header">
              <h2>Projects</h2>
              <Button size="sm" icon={<Plus size={14} />} onClick={() => setEditingProject("new")}>Add</Button>
            </div>
            {resume.projects.length === 0 ? <p className="hint">No projects added yet.</p> : (
              <ul className="job-list-compact">
                {resume.projects.map((p, i) => (
                  <li key={p.id} className="job-card job-card-compact">
                    <h4>{p.title}</h4>
                    <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.4rem" }}>
                      <button type="button" className="link-button" onClick={() => moveProject(p.id, -1)} disabled={i === 0} aria-label="Move up"><ArrowUp size={14} /></button>
                      <button type="button" className="link-button" onClick={() => moveProject(p.id, 1)} disabled={i === resume.projects.length - 1} aria-label="Move down"><ArrowDown size={14} /></button>
                      <button type="button" className="link-button" onClick={() => setEditingProject(p)}><Pencil size={14} /> Edit</button>
                      <button type="button" className="link-button" onClick={() => setDeleteTarget({ type: "project", id: p.id, label: p.title })}><Trash2 size={14} /> Remove</button>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </Card>
        </div>
      )}

      {editingExperience && (
        <ExperienceModal
          initial={editingExperience === "new" ? null : editingExperience}
          onClose={() => setEditingExperience(null)}
          onSave={async (data) => {
            const updated = editingExperience === "new" ? await addExperience(data) : await updateExperience(editingExperience.id, data);
            setResume(updated);
            setEditingExperience(null);
            toast.success("Experience saved.");
          }}
        />
      )}
      {editingEducation && (
        <EducationModal
          initial={editingEducation === "new" ? null : editingEducation}
          onClose={() => setEditingEducation(null)}
          onSave={async (data) => {
            const updated = editingEducation === "new" ? await addEducation(data) : await updateEducation(editingEducation.id, data);
            setResume(updated);
            setEditingEducation(null);
            toast.success("Education saved.");
          }}
        />
      )}
      {editingCertification && (
        <CertificationModal
          initial={editingCertification === "new" ? null : editingCertification}
          onClose={() => setEditingCertification(null)}
          onSave={async (data) => {
            const updated = editingCertification === "new" ? await addCertification(data) : await updateCertification(editingCertification.id, data);
            setResume(updated);
            setEditingCertification(null);
            toast.success("Certification saved.");
          }}
        />
      )}
      {editingProject && (
        <ProjectModal
          initial={editingProject === "new" ? null : editingProject}
          onClose={() => setEditingProject(null)}
          onSave={async (data) => {
            const updated = editingProject === "new" ? await addProject(data) : await updateProject(editingProject.id, data);
            setResume(updated);
            setEditingProject(null);
            toast.success("Project saved.");
          }}
        />
      )}

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Remove this entry?"
        confirmLabel="Remove"
        danger
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      >
        <p>Remove <strong>{deleteTarget?.label}</strong> from your resume? This can't be undone.</p>
      </ConfirmDialog>
    </div>
  );
}
