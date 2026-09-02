import { useEffect, useState, type FormEvent } from "react";
import { FileText, Pencil, Plus, Trash2 } from "lucide-react";
import {
  getMyCoverLetterTemplates, createCoverLetterTemplate, updateCoverLetterTemplate, deleteCoverLetterTemplate,
} from "../api/coverLetterTemplates";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { CoverLetterTemplate } from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import Modal from "../components/ui/Modal";
import ConfirmDialog from "../components/ui/ConfirmDialog";
import EmptyState from "../components/ui/EmptyState";

interface TemplateFormData {
  title: string;
  introduction?: string;
  skillsHighlights?: string;
  projectAchievements?: string;
  closingMessage?: string;
}

function TemplateModal({ initial, onClose, onSave }: { initial: CoverLetterTemplate | null; onClose: () => void; onSave: (data: TemplateFormData) => Promise<void> }) {
  const [title, setTitle] = useState(initial?.title ?? "");
  const [introduction, setIntroduction] = useState(initial?.introduction ?? "");
  const [skillsHighlights, setSkillsHighlights] = useState(initial?.skillsHighlights ?? "");
  const [projectAchievements, setProjectAchievements] = useState(initial?.projectAchievements ?? "");
  const [closingMessage, setClosingMessage] = useState(initial?.closingMessage ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      await onSave({
        title,
        introduction: introduction || undefined,
        skillsHighlights: skillsHighlights || undefined,
        projectAchievements: projectAchievements || undefined,
        closingMessage: closingMessage || undefined,
      });
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe);
      else setErrors({ general: getErrorMessage(err, "Failed to save template") });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      title={initial ? "Edit template" : "New cover letter template"}
      footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save</Button></>}
    >
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Template title" htmlFor="clt-title" required hint="e.g. 'Backend roles' — must be unique." error={errors.title}>
          <input id="clt-title" value={title} onChange={(e) => setTitle(e.target.value)} maxLength={100} />
        </FormField>
        <FormField label="Introduction" htmlFor="clt-intro" error={errors.introduction}>
          <textarea id="clt-intro" rows={3} value={introduction} onChange={(e) => setIntroduction(e.target.value)} maxLength={1500} placeholder="A short opening about who you are." />
        </FormField>
        <FormField label="Skills & experience highlights" htmlFor="clt-skills" error={errors.skillsHighlights}>
          <textarea id="clt-skills" rows={3} value={skillsHighlights} onChange={(e) => setSkillsHighlights(e.target.value)} maxLength={1500} />
        </FormField>
        <FormField label="Project achievements" htmlFor="clt-achievements" error={errors.projectAchievements}>
          <textarea id="clt-achievements" rows={3} value={projectAchievements} onChange={(e) => setProjectAchievements(e.target.value)} maxLength={1500} />
        </FormField>
        <FormField label="Closing message" htmlFor="clt-closing" error={errors.closingMessage}>
          <textarea id="clt-closing" rows={2} value={closingMessage} onChange={(e) => setClosingMessage(e.target.value)} maxLength={500} />
        </FormField>
        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}

export default function CoverLetterTemplatesPage() {
  const toast = useToast();
  const [templates, setTemplates] = useState<CoverLetterTemplate[]>([]);
  const [loading, setLoading] = useState(true);
  const [editing, setEditing] = useState<CoverLetterTemplate | "new" | null>(null);
  const [previewing, setPreviewing] = useState<CoverLetterTemplate | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<CoverLetterTemplate | null>(null);

  useEffect(() => {
    getMyCoverLetterTemplates()
      .then(setTemplates)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load your cover letter templates")))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleDeleteConfirm() {
    if (!deleteTarget) return;
    try {
      await deleteCoverLetterTemplate(deleteTarget.id);
      setTemplates((prev) => prev.filter((t) => t.id !== deleteTarget.id));
      toast.success("Template removed.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to remove template"));
    } finally {
      setDeleteTarget(null);
    }
  }

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <PageHeader
        title="Cover Letter Templates"
        subtitle="Build reusable templates, then pick one to personalize when you apply to a job."
        action={<Button icon={<Plus size={16} />} onClick={() => setEditing("new")}>New template</Button>}
      />

      {templates.length === 0 ? (
        <EmptyState
          icon={<FileText size={32} />}
          title="No templates yet"
          description="Create a reusable template with your introduction, skills, achievements, and closing message."
          action={<Button icon={<Plus size={16} />} onClick={() => setEditing("new")}>New template</Button>}
        />
      ) : (
        <ul className="job-list-compact">
          {templates.map((t) => (
            <li key={t.id} className="job-card job-card-compact">
              <h4>{t.title}</h4>
              {t.introduction && <p className="hint">{t.introduction.slice(0, 120)}{t.introduction.length > 120 ? "…" : ""}</p>}
              <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.4rem" }}>
                <button type="button" className="link-button" onClick={() => setPreviewing(t)}>Preview</button>
                <button type="button" className="link-button" onClick={() => setEditing(t)}><Pencil size={14} /> Edit</button>
                <button type="button" className="link-button" onClick={() => setDeleteTarget(t)}><Trash2 size={14} /> Remove</button>
              </div>
            </li>
          ))}
        </ul>
      )}

      {editing && (
        <TemplateModal
          initial={editing === "new" ? null : editing}
          onClose={() => setEditing(null)}
          onSave={async (data) => {
            const updated = editing === "new" ? await createCoverLetterTemplate(data) : await updateCoverLetterTemplate(editing.id, data);
            setTemplates((prev) => editing === "new" ? [...prev, updated] : prev.map((t) => (t.id === updated.id ? updated : t)));
            setEditing(null);
            toast.success("Template saved.");
          }}
        />
      )}

      <Modal open={previewing !== null} onClose={() => setPreviewing(null)} title={previewing?.title ?? "Preview"}>
        {previewing && (
          <Card className="ui-card-padded">
            {previewing.introduction && <p>{previewing.introduction}</p>}
            {previewing.skillsHighlights && <p style={{ marginTop: "0.75rem" }}>{previewing.skillsHighlights}</p>}
            {previewing.projectAchievements && <p style={{ marginTop: "0.75rem" }}>{previewing.projectAchievements}</p>}
            {previewing.closingMessage && <p style={{ marginTop: "0.75rem" }}>{previewing.closingMessage}</p>}
          </Card>
        )}
      </Modal>

      <ConfirmDialog
        open={deleteTarget !== null}
        title="Remove this template?"
        confirmLabel="Remove"
        danger
        onConfirm={handleDeleteConfirm}
        onCancel={() => setDeleteTarget(null)}
      >
        <p>Remove <strong>{deleteTarget?.title}</strong>? This can't be undone.</p>
      </ConfirmDialog>
    </div>
  );
}
