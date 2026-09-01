import { useEffect, useState, type FormEvent } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { Building2, FileStack } from "lucide-react";
import { createJob, getJobById, updateJob, type JobTypeValue } from "../api/jobs";
import { getMyJobTemplates } from "../api/jobTemplates";
import { getOnboardingStatus } from "../api/recruiters";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { JobTemplate } from "../types";
import Button from "../components/ui/Button";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import EmptyState from "../components/ui/EmptyState";
import IndiaLocationSelector from "../components/IndiaLocationSelector";

interface FieldErrors {
  title?: string;
  description?: string;
  state?: string;
  city?: string;
  general?: string;
}

const JOB_TYPE_OPTIONS: { value: JobTypeValue; label: string }[] = [
  { value: "FullTime", label: "Full-time" },
  { value: "PartTime", label: "Part-time" },
  { value: "Contract", label: "Contract" },
  { value: "Internship", label: "Internship" },
  { value: "Freelance", label: "Freelance" },
];

export default function PostJobPage() {
  const { id } = useParams();
  const isEditMode = Boolean(id);

  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [skills, setSkills] = useState("");
  const [state, setState] = useState("");
  const [city, setCity] = useState("");
  const [locality, setLocality] = useState("");
  const [isRemote, setIsRemote] = useState(false);
  const [jobType, setJobType] = useState<JobTypeValue>("FullTime");
  const [minExperienceYears, setMinExperienceYears] = useState("");
  const [maxExperienceYears, setMaxExperienceYears] = useState("");
  const [minSalary, setMinSalary] = useState("");
  const [maxSalary, setMaxSalary] = useState("");
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [loading, setLoading] = useState<"draft" | "publish" | "save" | null>(null);
  const [checkingOnboarding, setCheckingOnboarding] = useState(true);
  const [isOnboarded, setIsOnboarded] = useState(false);
  const [loadingJob, setLoadingJob] = useState(isEditMode);
  const [templates, setTemplates] = useState<JobTemplate[]>([]);
  const [selectedTemplateId, setSelectedTemplateId] = useState("");
  const navigate = useNavigate();
  const toast = useToast();

  useEffect(() => {
    getOnboardingStatus()
      .then((status) => setIsOnboarded(status.isOnboarded))
      .finally(() => setCheckingOnboarding(false));
    if (!isEditMode) {
      getMyJobTemplates().then(setTemplates).catch(() => setTemplates([]));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function applyTemplate(templateId: string) {
    setSelectedTemplateId(templateId);
    const template = templates.find((t) => t.id === Number(templateId));
    if (!template) return;

    setTitle(template.title);
    setDescription(template.description);
    setSkills(template.requiredSkillsCsv ?? "");
    setState(template.defaultIsRemote ? "" : template.defaultState ?? "");
    setCity(template.defaultIsRemote ? "" : template.defaultCity ?? "");
    setLocality(template.defaultIsRemote ? "" : template.defaultLocality ?? "");
    setIsRemote(template.defaultIsRemote);
    setJobType(template.employmentType as JobTypeValue);
    setMinExperienceYears(template.minExperienceYears?.toString() ?? "");
    setMaxExperienceYears(template.maxExperienceYears?.toString() ?? "");
    if (template.salaryVisible) {
      setMinSalary(template.minSalary?.toString() ?? "");
      setMaxSalary(template.maxSalary?.toString() ?? "");
    }
    toast.success(`Prefilled from "${template.title}" template — review and edit before publishing.`);
  }

  useEffect(() => {
    if (!id) return;
    getJobById(Number(id))
      .then((job) => {
        setTitle(job.title);
        setDescription(job.description);
        setSkills(job.requiredSkillsCsv ?? "");
        setState(job.state ?? "");
        setCity(job.city ?? "");
        setLocality(job.locality ?? "");
        setIsRemote(job.isRemote);
        setJobType(job.jobType as JobTypeValue);
        setMinExperienceYears(job.minExperienceYears?.toString() ?? "");
        setMaxExperienceYears(job.maxExperienceYears?.toString() ?? "");
        setMinSalary(job.minSalary?.toString() ?? "");
        setMaxSalary(job.maxSalary?.toString() ?? "");
      })
      .finally(() => setLoadingJob(false));
  }, [id]);

  function validate(requireLocation: boolean): FieldErrors {
    const clientErrors: FieldErrors = {};
    if (!title.trim()) clientErrors.title = "Title is required.";
    if (!description.trim()) clientErrors.description = "Description is required.";
    if (requireLocation && !isRemote) {
      if (!state.trim()) clientErrors.state = "State is required.";
      if (!city.trim()) clientErrors.city = "City is required.";
    }
    return clientErrors;
  }

  function buildPayload() {
    return {
      title,
      description,
      requiredSkillsCsv: skills || undefined,
      state: isRemote ? undefined : state || undefined,
      city: isRemote ? undefined : city || undefined,
      locality: isRemote ? undefined : locality || undefined,
      isRemote,
      jobType,
      minExperienceYears: minExperienceYears ? Number(minExperienceYears) : undefined,
      maxExperienceYears: maxExperienceYears ? Number(maxExperienceYears) : undefined,
      minSalary: minSalary ? Number(minSalary) : undefined,
      maxSalary: maxSalary ? Number(maxSalary) : undefined,
    };
  }

  async function handleSaveEdit(e: FormEvent) {
    e.preventDefault();
    if (!id) return;
    setFieldErrors({});
    const clientErrors = validate(true);
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setLoading("save");
    try {
      await updateJob(Number(id), buildPayload());
      toast.success("Job updated.");
      navigate("/jobs/mine");
    } catch (err) {
      setFieldErrors({ general: getErrorMessage(err, "Failed to update job") });
    } finally {
      setLoading(null);
    }
  }

  async function handleCreate(e: FormEvent, saveAsDraft: boolean) {
    e.preventDefault();
    setFieldErrors({});
    const clientErrors = validate(!saveAsDraft);
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setLoading(saveAsDraft ? "draft" : "publish");
    try {
      const job = await createJob({ ...buildPayload(), saveAsDraft });
      toast.success(saveAsDraft ? "Draft saved." : "Job posted successfully.");
      navigate(saveAsDraft ? "/jobs/mine" : `/jobs/${job.id}`);
    } catch (err) {
      setFieldErrors({ general: getErrorMessage(err, "Failed to post job") });
    } finally {
      setLoading(null);
    }
  }

  if (checkingOnboarding || loadingJob) return <p>Loading...</p>;

  if (!isOnboarded) {
    return (
      <Card>
        <EmptyState
          icon={<Building2 size={28} />}
          title="Complete your company profile first"
          description="We need your company details before a job posting can go live."
          action={<Link to="/onboarding" className="btn btn-primary">Complete company onboarding</Link>}
        />
      </Card>
    );
  }

  return (
    <div style={{ maxWidth: 720, margin: "0 auto" }}>
      <div className="page-header">
        <h1>{isEditMode ? "Edit Job" : "Post a Job"}</h1>
        <p>
          {isEditMode
            ? "Update the details below — status changes (publish, close, archive) happen from Manage Jobs."
            : "Fill in the details below, then save as a draft or publish right away."}
        </p>
      </div>

      {!isEditMode && templates.length > 0 && (
        <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
          <h3 className="form-section-title"><FileStack size={16} style={{ verticalAlign: "-3px", marginRight: "0.35rem" }} />Use Template</h3>
          <FormField label="Template" htmlFor="job-template" hint="Prefills the fields below — you can still edit everything before publishing.">
            <select id="job-template" value={selectedTemplateId} onChange={(e) => applyTemplate(e.target.value)}>
              <option value="">Start from scratch</option>
              {templates.map((t) => (
                <option key={t.id} value={t.id}>{t.title}{t.department ? ` — ${t.department}` : ""}</option>
              ))}
            </select>
          </FormField>
        </Card>
      )}

      <Card className="ui-card-padded">
        <form onSubmit={isEditMode ? handleSaveEdit : (e) => handleCreate(e, false)} noValidate>
          <div className="form-section">
            <h3 className="form-section-title">Role details</h3>
            <p className="form-section-desc">What's the position and what will they be doing?</p>

            <FormField label="Job title" htmlFor="job-title" required error={fieldErrors.title}>
              <input id="job-title" value={title} onChange={(e) => setTitle(e.target.value)} placeholder="e.g. Senior Backend Engineer" />
            </FormField>
            <FormField label="Description" htmlFor="job-description" required error={fieldErrors.description}>
              <textarea
                id="job-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={7}
                placeholder="Responsibilities, team context, what success looks like..."
              />
            </FormField>
            <FormField label="Job type" htmlFor="job-type" required>
              <select id="job-type" value={jobType} onChange={(e) => setJobType(e.target.value as JobTypeValue)}>
                {JOB_TYPE_OPTIONS.map((opt) => (
                  <option key={opt.value} value={opt.value}>{opt.label}</option>
                ))}
              </select>
            </FormField>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Requirements</h3>
            <p className="form-section-desc">Used to match candidates and power search filters.</p>

            <FormField label="Required skills" htmlFor="job-skills" hint="Comma separated, e.g. C#, SQL Server, React">
              <input id="job-skills" value={skills} onChange={(e) => setSkills(e.target.value)} placeholder="C#, SQL Server, React" />
            </FormField>
            <div className="form-row">
              <FormField label="Minimum experience (years)" htmlFor="job-min-exp">
                <input id="job-min-exp" type="number" min={0} max={40} value={minExperienceYears} onChange={(e) => setMinExperienceYears(e.target.value)} />
              </FormField>
              <FormField label="Maximum experience (years)" htmlFor="job-max-exp">
                <input id="job-max-exp" type="number" min={0} max={40} value={maxExperienceYears} onChange={(e) => setMaxExperienceYears(e.target.value)} />
              </FormField>
            </div>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Compensation & location</h3>
            <p className="form-section-desc">Salary is optional — leave blank to show "Not disclosed." Location is India-only.</p>

            <div className="form-row">
              <FormField label="Minimum salary" htmlFor="job-min-salary">
                <input id="job-min-salary" type="number" min={0} value={minSalary} onChange={(e) => setMinSalary(e.target.value)} />
              </FormField>
              <FormField label="Maximum salary" htmlFor="job-max-salary">
                <input id="job-max-salary" type="number" min={0} value={maxSalary} onChange={(e) => setMaxSalary(e.target.value)} />
              </FormField>
            </div>

            <IndiaLocationSelector
              state={state}
              city={city}
              locality={locality}
              isRemote={isRemote}
              onStateChange={setState}
              onCityChange={setCity}
              onLocalityChange={setLocality}
              onIsRemoteChange={setIsRemote}
              stateError={fieldErrors.state}
              cityError={fieldErrors.city}
              required={!isEditMode}
            />
          </div>

          {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

          <div className="form-actions">
            {isEditMode ? (
              <>
                <Button type="submit" loading={loading === "save"}>Save changes</Button>
                <Button type="button" variant="secondary" onClick={() => navigate("/jobs/mine")}>Cancel</Button>
              </>
            ) : (
              <>
                <Button type="submit" loading={loading === "publish"}>Publish</Button>
                <Button type="button" variant="secondary" loading={loading === "draft"} onClick={(e) => handleCreate(e as unknown as FormEvent, true)}>
                  Save as draft
                </Button>
                <Button type="button" variant="ghost" onClick={() => navigate(-1)}>Cancel</Button>
              </>
            )}
          </div>
        </form>
      </Card>
    </div>
  );
}
