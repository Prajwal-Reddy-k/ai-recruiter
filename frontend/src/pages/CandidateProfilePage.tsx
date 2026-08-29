import { useEffect, useRef, useState, type FormEvent } from "react";
import { FileText, UploadCloud } from "lucide-react";
import { getMyCandidateProfile, upsertMyCandidateProfile, uploadResume, downloadMyResume } from "../api/candidates";
import type { CandidateProfile } from "../types";
import { getErrorMessage } from "../utils/errors";
import { saveBlobAsFile } from "../utils/download";
import { useToast } from "../context/ToastContext";
import IndiaLocationSelector from "../components/IndiaLocationSelector";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";

type UploadState = "idle" | "uploading" | "success" | "error";

interface FieldErrors {
  totalExperienceYears?: string;
  general?: string;
}

export default function CandidateProfilePage() {
  const toast = useToast();
  const [profile, setProfile] = useState<CandidateProfile | null>(null);
  const [headline, setHeadline] = useState("");
  const [summary, setSummary] = useState("");
  const [education, setEducation] = useState("");
  const [experienceSummary, setExperienceSummary] = useState("");
  const [totalExperienceYears, setTotalExperienceYears] = useState("");
  const [state, setState] = useState("");
  const [city, setCity] = useState("");
  const [locality, setLocality] = useState("");
  const [isRemote, setIsRemote] = useState(false);
  const [skillsCsv, setSkillsCsv] = useState("");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});

  const [uploadState, setUploadState] = useState<UploadState>("idle");
  const [uploadProgress, setUploadProgress] = useState(0);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    void loadProfile();
  }, []);

  async function loadProfile() {
    setLoading(true);
    try {
      const data = await getMyCandidateProfile();
      setProfile(data);
      setHeadline(data.headline ?? "");
      setSummary(data.summary ?? "");
      setEducation(data.education ?? "");
      setExperienceSummary(data.experienceSummary ?? "");
      setTotalExperienceYears(data.totalExperienceYears?.toString() ?? "");
      setState(data.state ?? "");
      setCity(data.city ?? "");
      setLocality(data.locality ?? "");
      setSkillsCsv(data.skillsCsv ?? "");
    } finally {
      setLoading(false);
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const clientErrors: FieldErrors = {};
    if (totalExperienceYears) {
      const years = Number(totalExperienceYears);
      if (!Number.isInteger(years) || years < 0 || years > 60) {
        clientErrors.totalExperienceYears = "Enter a whole number of years between 0 and 60.";
      }
    }
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setSaving(true);
    try {
      const updated = await upsertMyCandidateProfile({
        headline: headline || undefined,
        summary: summary || undefined,
        education: education || undefined,
        experienceSummary: experienceSummary || undefined,
        totalExperienceYears: totalExperienceYears ? Number(totalExperienceYears) : undefined,
        state: state || undefined,
        city: city || undefined,
        locality: locality || undefined,
        skillsCsv: skillsCsv || undefined,
      });
      setProfile(updated);
      toast.success("Profile saved.");
    } catch (err) {
      setFieldErrors({ general: getErrorMessage(err, "Failed to save profile") });
    } finally {
      setSaving(false);
    }
  }

  async function handleFileChange() {
    const file = fileInputRef.current?.files?.[0];
    if (!file) return;

    setUploadState("uploading");
    setUploadProgress(0);
    setUploadError(null);

    try {
      const updated = await uploadResume(file, setUploadProgress);
      setProfile(updated);
      setUploadState("success");
      toast.success("Resume uploaded successfully.");
    } catch (err) {
      setUploadError(getErrorMessage(err, "Resume upload failed"));
      setUploadState("error");
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = "";
    }
  }

  async function handleDownloadResume() {
    if (!profile?.resumeOriginalFileName) return;
    const blob = await downloadMyResume();
    saveBlobAsFile(blob, profile.resumeOriginalFileName);
  }

  if (loading) return <p>Loading...</p>;

  return (
    <div style={{ maxWidth: 720, margin: "0 auto", display: "flex", flexDirection: "column", gap: "1.5rem" }}>
      <div className="page-header">
        <h1>My Candidate Profile</h1>
        <p>Keep this up to date — it powers job recommendations and your explainable match score.</p>
      </div>

      <Card className="ui-card-padded">
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-section">
            <h3 className="form-section-title">Headline & summary</h3>
            <FormField label="Headline" htmlFor="profile-headline">
              <input id="profile-headline" value={headline} onChange={(e) => setHeadline(e.target.value)} placeholder="e.g. Senior Backend Engineer" />
            </FormField>
            <FormField label="Summary" htmlFor="profile-summary">
              <textarea id="profile-summary" value={summary} onChange={(e) => setSummary(e.target.value)} rows={3} />
            </FormField>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Background</h3>
            <FormField label="Education" htmlFor="profile-education" hint="e.g. B.Sc. Computer Science, XYZ University">
              <textarea id="profile-education" value={education} onChange={(e) => setEducation(e.target.value)} rows={2} />
            </FormField>
            <FormField label="Experience summary" htmlFor="profile-exp-summary">
              <textarea id="profile-exp-summary" value={experienceSummary} onChange={(e) => setExperienceSummary(e.target.value)} rows={3} />
            </FormField>
            <FormField label="Total years of experience" htmlFor="profile-exp-years" error={fieldErrors.totalExperienceYears}>
              <input
                id="profile-exp-years"
                type="number"
                min={0}
                max={60}
                value={totalExperienceYears}
                onChange={(e) => setTotalExperienceYears(e.target.value)}
              />
            </FormField>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Location & skills</h3>
            <IndiaLocationSelector
              state={state}
              city={city}
              locality={locality}
              isRemote={isRemote}
              onStateChange={setState}
              onCityChange={setCity}
              onLocalityChange={setLocality}
              onIsRemoteChange={setIsRemote}
              showRemoteOption={false}
            />
            <FormField label="Skills" htmlFor="profile-skills" hint="Comma separated — matched against job requirements.">
              <input id="profile-skills" value={skillsCsv} onChange={(e) => setSkillsCsv(e.target.value)} placeholder="C#, SQL Server, React" />
            </FormField>
          </div>

          {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

          <div className="form-actions">
            <Button type="submit" loading={saving}>Save profile</Button>
          </div>
        </form>
      </Card>

      <Card className="ui-card-padded resume-section">
        <h2><FileText size={18} /> Resume</h2>
        {profile?.resumeOriginalFileName ? (
          <p>
            On file: <button type="button" className="link-button" onClick={handleDownloadResume}>{profile.resumeOriginalFileName}</button>
            {" "}({Math.round((profile.resumeSizeBytes ?? 0) / 1024)} KB)
          </p>
        ) : (
          <p className="hint">No resume uploaded yet.</p>
        )}

        <label className="btn btn-secondary btn-sm" style={{ marginTop: "0.75rem", cursor: "pointer" }}>
          <UploadCloud size={16} />
          {profile?.resumeOriginalFileName ? "Replace resume" : "Upload resume"}
          <input ref={fileInputRef} type="file" accept=".pdf,.docx" onChange={handleFileChange} style={{ display: "none" }} />
        </label>

        {uploadState === "uploading" && (
          <div className="upload-progress">
            <div className="upload-progress-bar" style={{ width: `${uploadProgress}%` }} />
            <span>{uploadProgress}%</span>
          </div>
        )}
        {uploadState === "error" && <p className="error" style={{ marginTop: "0.75rem" }}>{uploadError}</p>}

        <p className="hint" style={{ marginTop: "0.75rem" }}>PDF or DOCX, up to 5 MB. Uploading a new file replaces your current resume.</p>
      </Card>
    </div>
  );
}
