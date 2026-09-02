import { useEffect, useRef, useState, type FocusEvent, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { FileText, UploadCloud } from "lucide-react";
import { getMyCandidateProfile, upsertMyCandidateProfile, uploadResume, downloadMyResume } from "../api/candidates";
import type { CandidateProfile } from "../types";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { saveBlobAsFile } from "../utils/download";
import { useToast } from "../context/ToastContext";
import IndiaLocationSelector from "../components/IndiaLocationSelector";
import AvatarUpload from "../components/AvatarUpload";
import PublicProfileShareCard from "../components/PublicProfileShareCard";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import PageHeader from "../components/ui/PageHeader";

type UploadState = "idle" | "uploading" | "success" | "error";

interface FieldErrors {
  headline?: string;
  summary?: string;
  education?: string;
  graduationYear?: string;
  experienceSummary?: string;
  totalExperienceYears?: string;
  location?: string;
  skills?: string;
  phone?: string;
  linkedInUrl?: string;
  githubUrl?: string;
  portfolioUrl?: string;
  expectedSalaryMin?: string;
  expectedSalaryMax?: string;
  noticePeriodDays?: string;
  general?: string;
}

const AVAILABILITY_OPTIONS = [
  { value: "ActivelyLooking", label: "Actively looking" },
  { value: "OpenToOpportunities", label: "Open to opportunities" },
  { value: "NotLooking", label: "Not looking" },
];

const VISIBILITY_OPTIONS = [
  { value: "VisibleToRecruiters", label: "Visible to recruiters (discoverable, even before applying)" },
  { value: "VisibleAfterApplying", label: "Visible only after applying (default)" },
  { value: "Private", label: "Private (never shown to recruiters)" },
  { value: "PublicShareable", label: "Public shareable (anyone with the link — plus visible to recruiters)" },
];

const MAX_SKILLS = 30;
const MAX_SKILL_LENGTH = 50;

function normalizePhoneDigits(phone: string): string | null {
  const digits = phone.replace(/\D/g, "");
  if (digits.length === 10) return digits;
  if (digits.length === 11 && digits.startsWith("0")) return digits.slice(1);
  if (digits.length === 12 && digits.startsWith("91")) return digits.slice(2);
  if (digits.length === 13 && digits.startsWith("091")) return digits.slice(3);
  return null;
}

/** True if `host` is exactly `domain` or a subdomain of it — a plain substring check would
 * wrongly accept a host like "notgithub.com" as matching "github.com". */
function isHostOrSubdomain(host: string, domain: string): boolean {
  const h = host.toLowerCase();
  return h === domain || h.endsWith(`.${domain}`);
}

function validateUrl(url: string, label: string, requiredDomain?: string): string | null {
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") throw new Error("bad scheme");
    if (requiredDomain && !isHostOrSubdomain(parsed.host, requiredDomain)) {
      return `Enter a valid ${label} URL (should link to ${requiredDomain}).`;
    }
    return null;
  } catch {
    return `Enter a valid ${label} URL (starting with https://).`;
  }
}

export default function CandidateProfilePage() {
  const toast = useToast();
  const [profile, setProfile] = useState<CandidateProfile | null>(null);
  const [headline, setHeadline] = useState("");
  const [summary, setSummary] = useState("");
  const [education, setEducation] = useState("");
  const [graduationYear, setGraduationYear] = useState("");
  const [experienceSummary, setExperienceSummary] = useState("");
  const [totalExperienceYears, setTotalExperienceYears] = useState("");
  const [state, setState] = useState("");
  const [city, setCity] = useState("");
  const [locality, setLocality] = useState("");
  const [isRemote, setIsRemote] = useState(false);
  const [skillsCsv, setSkillsCsv] = useState("");
  const [phone, setPhone] = useState("");
  const [linkedInUrl, setLinkedInUrl] = useState("");
  const [githubUrl, setGithubUrl] = useState("");
  const [portfolioUrl, setPortfolioUrl] = useState("");
  const [availabilityStatus, setAvailabilityStatus] = useState("OpenToOpportunities");
  const [preferredJobTypesCsv, setPreferredJobTypesCsv] = useState("");
  const [preferredLocationsCsv, setPreferredLocationsCsv] = useState("");
  const [remotePreference, setRemotePreference] = useState("");
  const [expectedSalaryMin, setExpectedSalaryMin] = useState("");
  const [expectedSalaryMax, setExpectedSalaryMax] = useState("");
  const [noticePeriodDays, setNoticePeriodDays] = useState("");
  const [preferredRolesCsv, setPreferredRolesCsv] = useState("");
  const [profileVisibility, setProfileVisibility] = useState("VisibleAfterApplying");
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const [touched, setTouched] = useState<Set<string>>(new Set());

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
      applyProfile(data);
    } finally {
      setLoading(false);
    }
  }

  function applyProfile(data: CandidateProfile) {
    setProfile(data);
    setHeadline(data.headline ?? "");
    setSummary(data.summary ?? "");
    setEducation(data.education ?? "");
    setGraduationYear(data.graduationYear?.toString() ?? "");
    setExperienceSummary(data.experienceSummary ?? "");
    setTotalExperienceYears(data.totalExperienceYears?.toString() ?? "");
    setState(data.state ?? "");
    setCity(data.city ?? "");
    setLocality(data.locality ?? "");
    setSkillsCsv(data.skillsCsv ?? "");
    setPhone(data.phone ?? "");
    setLinkedInUrl(data.linkedInUrl ?? "");
    setGithubUrl(data.githubUrl ?? "");
    setPortfolioUrl(data.portfolioUrl ?? "");
    setAvailabilityStatus(data.availabilityStatus);
    setPreferredJobTypesCsv(data.preferredJobTypesCsv ?? "");
    setPreferredLocationsCsv(data.preferredLocationsCsv ?? "");
    setRemotePreference(data.remotePreference === null ? "" : data.remotePreference ? "yes" : "no");
    setExpectedSalaryMin(data.expectedSalaryMin?.toString() ?? "");
    setExpectedSalaryMax(data.expectedSalaryMax?.toString() ?? "");
    setNoticePeriodDays(data.noticePeriodDays?.toString() ?? "");
    setPreferredRolesCsv(data.preferredRolesCsv ?? "");
    setProfileVisibility(data.profileVisibility);
  }

  function runValidation(): FieldErrors {
    const errors: FieldErrors = {};
    const currentYear = new Date().getFullYear();

    const trimmedHeadline = headline.trim();
    if (!trimmedHeadline) {
      errors.headline = "Headline is required.";
    } else if (trimmedHeadline.length < 5 || trimmedHeadline.length > 150) {
      errors.headline = "Headline must be between 5 and 150 characters.";
    }

    if (summary.length > 2000) {
      errors.summary = "Bio must be 2000 characters or fewer.";
    }

    if (education.length > 300) {
      errors.education = "Education must be 300 characters or fewer.";
    }

    if (graduationYear) {
      const year = Number(graduationYear);
      if (!Number.isInteger(year) || year < 1950 || year > currentYear + 1) {
        errors.graduationYear = `Graduation year must be between 1950 and ${currentYear + 1}.`;
      } else if (!education.trim()) {
        errors.education = "Enter your degree/institution along with a graduation year.";
      }
    }

    if (experienceSummary.length > 2000) {
      errors.experienceSummary = "Experience summary must be 2000 characters or fewer.";
    }

    if (totalExperienceYears) {
      const years = Number(totalExperienceYears);
      if (!Number.isInteger(years) || years < 0 || years > 60) {
        errors.totalExperienceYears = "Enter a whole number of years between 0 and 60.";
      }
    }

    const skills = skillsCsv
      .split(",")
      .map((s) => s.trim())
      .filter((s) => s.length > 0);
    if (skills.length === 0) {
      errors.skills = "Add at least one skill.";
    } else if (skills.length > MAX_SKILLS) {
      errors.skills = `You can list at most ${MAX_SKILLS} skills.`;
    } else if (skills.some((s) => s.length > MAX_SKILL_LENGTH)) {
      errors.skills = `Each skill must be ${MAX_SKILL_LENGTH} characters or fewer.`;
    } else {
      const distinct = new Set(skills.map((s) => s.toLowerCase()));
      if (distinct.size !== skills.length) {
        errors.skills = "Remove duplicate skills.";
      }
    }

    if (phone.trim() && !normalizePhoneDigits(phone)) {
      errors.phone = "Enter a valid 10-digit Indian mobile number.";
    }

    if (linkedInUrl.trim()) {
      const err = validateUrl(linkedInUrl.trim(), "LinkedIn", "linkedin.com");
      if (err) errors.linkedInUrl = err;
    }
    if (githubUrl.trim()) {
      const err = validateUrl(githubUrl.trim(), "GitHub", "github.com");
      if (err) errors.githubUrl = err;
    }
    if (portfolioUrl.trim()) {
      const err = validateUrl(portfolioUrl.trim(), "portfolio/website");
      if (err) errors.portfolioUrl = err;
    }

    const minSalary = expectedSalaryMin ? Number(expectedSalaryMin) : null;
    const maxSalary = expectedSalaryMax ? Number(expectedSalaryMax) : null;
    if (expectedSalaryMin && (Number.isNaN(minSalary!) || minSalary! < 0)) {
      errors.expectedSalaryMin = "Enter a valid amount.";
    }
    if (expectedSalaryMax && (Number.isNaN(maxSalary!) || maxSalary! < 0)) {
      errors.expectedSalaryMax = "Enter a valid amount.";
    }
    if (minSalary !== null && maxSalary !== null && !Number.isNaN(minSalary) && !Number.isNaN(maxSalary) && minSalary > maxSalary) {
      errors.expectedSalaryMax = "Maximum must be greater than or equal to minimum.";
    }

    if (noticePeriodDays) {
      const days = Number(noticePeriodDays);
      if (!Number.isInteger(days) || days < 0 || days > 365) {
        errors.noticePeriodDays = "Enter a whole number of days between 0 and 365.";
      }
    }

    return errors;
  }

  function handleBlur(field: string) {
    return () => {
      setTouched((prev) => new Set(prev).add(field));
      setFieldErrors((prev) => ({ ...prev, ...runValidation() }));
    };
  }

  function visibleError(field: keyof FieldErrors): string | undefined {
    if (!touched.has(field) && field !== "general") return undefined;
    return fieldErrors[field];
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    const clientErrors = runValidation();
    setFieldErrors(clientErrors);
    setTouched(new Set(["headline", "summary", "education", "graduationYear", "experienceSummary", "totalExperienceYears", "skills", "phone", "linkedInUrl", "githubUrl", "portfolioUrl", "expectedSalaryMin", "expectedSalaryMax", "noticePeriodDays"]));

    if (Object.keys(clientErrors).length > 0) {
      return;
    }

    setSaving(true);
    try {
      const updated = await upsertMyCandidateProfile({
        headline: headline.trim() || undefined,
        summary: summary || undefined,
        education: education || undefined,
        graduationYear: graduationYear ? Number(graduationYear) : undefined,
        experienceSummary: experienceSummary || undefined,
        totalExperienceYears: totalExperienceYears ? Number(totalExperienceYears) : undefined,
        state: state || undefined,
        city: city || undefined,
        locality: locality || undefined,
        skillsCsv: skillsCsv || undefined,
        phone: phone.trim() || undefined,
        linkedInUrl: linkedInUrl.trim() || undefined,
        githubUrl: githubUrl.trim() || undefined,
        portfolioUrl: portfolioUrl.trim() || undefined,
        availabilityStatus,
        preferredJobTypesCsv: preferredJobTypesCsv || undefined,
        preferredLocationsCsv: preferredLocationsCsv || undefined,
        remotePreference: remotePreference === "" ? undefined : remotePreference === "yes",
        expectedSalaryMin: expectedSalaryMin ? Number(expectedSalaryMin) : undefined,
        expectedSalaryMax: expectedSalaryMax ? Number(expectedSalaryMax) : undefined,
        noticePeriodDays: noticePeriodDays ? Number(noticePeriodDays) : undefined,
        preferredRolesCsv: preferredRolesCsv || undefined,
        profileVisibility,
      });
      applyProfile(updated);
      setFieldErrors({});
      setTouched(new Set());
      toast.success("Profile saved.");
    } catch (err) {
      const serverFieldErrors = getFieldErrors(err);
      if (serverFieldErrors) {
        setFieldErrors(serverFieldErrors as FieldErrors);
        setTouched(new Set(Object.keys(serverFieldErrors)));
      } else {
        setFieldErrors({ general: getErrorMessage(err, "Failed to save profile") });
      }
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
      <PageHeader title="My Candidate Profile" subtitle="Keep this up to date — it powers job recommendations and your explainable match score." />

      {profile && (
        <Card className="ui-card-padded">
          <h2>Profile photo</h2>
          <AvatarUpload profile={profile} onChange={setProfile} />
        </Card>
      )}

      <Card className="ui-card-padded">
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-section">
            <h3 className="form-section-title">Headline & summary</h3>
            <FormField label="Headline" htmlFor="profile-headline" required error={visibleError("headline")}>
              <input
                id="profile-headline"
                value={headline}
                onChange={(e) => setHeadline(e.target.value)}
                onBlur={handleBlur("headline")}
                placeholder="e.g. Senior Backend Engineer"
                aria-required="true"
                aria-invalid={!!visibleError("headline")}
              />
            </FormField>
            <FormField label="Bio / summary" htmlFor="profile-summary" error={visibleError("summary")} hint="Up to 2000 characters.">
              <textarea
                id="profile-summary"
                value={summary}
                onChange={(e) => setSummary(e.target.value)}
                onBlur={handleBlur("summary") as unknown as (e: FocusEvent<HTMLTextAreaElement>) => void}
                rows={3}
                aria-invalid={!!visibleError("summary")}
              />
            </FormField>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Background</h3>
            <FormField label="Education" htmlFor="profile-education" hint="e.g. B.Sc. Computer Science, XYZ University" error={visibleError("education")}>
              <textarea
                id="profile-education"
                value={education}
                onChange={(e) => setEducation(e.target.value)}
                onBlur={handleBlur("education") as unknown as (e: FocusEvent<HTMLTextAreaElement>) => void}
                rows={2}
                aria-invalid={!!visibleError("education")}
              />
            </FormField>
            <FormField label="Graduation year" htmlFor="profile-grad-year" error={visibleError("graduationYear")}>
              <input
                id="profile-grad-year"
                type="number"
                value={graduationYear}
                onChange={(e) => setGraduationYear(e.target.value)}
                onBlur={handleBlur("graduationYear")}
                aria-invalid={!!visibleError("graduationYear")}
              />
            </FormField>
            <FormField label="Experience summary" htmlFor="profile-exp-summary" error={visibleError("experienceSummary")}>
              <textarea
                id="profile-exp-summary"
                value={experienceSummary}
                onChange={(e) => setExperienceSummary(e.target.value)}
                onBlur={handleBlur("experienceSummary") as unknown as (e: FocusEvent<HTMLTextAreaElement>) => void}
                rows={3}
                aria-invalid={!!visibleError("experienceSummary")}
              />
            </FormField>
            <FormField label="Total years of experience" htmlFor="profile-exp-years" error={visibleError("totalExperienceYears")}>
              <input
                id="profile-exp-years"
                type="number"
                min={0}
                max={60}
                value={totalExperienceYears}
                onChange={(e) => setTotalExperienceYears(e.target.value)}
                onBlur={handleBlur("totalExperienceYears")}
                aria-invalid={!!visibleError("totalExperienceYears")}
              />
            </FormField>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Contact & links</h3>
            <FormField label="Phone" htmlFor="profile-phone" hint="10-digit Indian mobile number." error={visibleError("phone")}>
              <input
                id="profile-phone"
                type="tel"
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                onBlur={handleBlur("phone")}
                placeholder="9876543210"
                aria-invalid={!!visibleError("phone")}
              />
            </FormField>
            <FormField label="LinkedIn URL" htmlFor="profile-linkedin" error={visibleError("linkedInUrl")}>
              <input
                id="profile-linkedin"
                type="url"
                value={linkedInUrl}
                onChange={(e) => setLinkedInUrl(e.target.value)}
                onBlur={handleBlur("linkedInUrl")}
                placeholder="https://linkedin.com/in/..."
                aria-invalid={!!visibleError("linkedInUrl")}
              />
            </FormField>
            <FormField label="GitHub URL" htmlFor="profile-github" error={visibleError("githubUrl")}>
              <input
                id="profile-github"
                type="url"
                value={githubUrl}
                onChange={(e) => setGithubUrl(e.target.value)}
                onBlur={handleBlur("githubUrl")}
                placeholder="https://github.com/..."
                aria-invalid={!!visibleError("githubUrl")}
              />
            </FormField>
            <FormField label="Portfolio / website URL" htmlFor="profile-portfolio" error={visibleError("portfolioUrl")}>
              <input
                id="profile-portfolio"
                type="url"
                value={portfolioUrl}
                onChange={(e) => setPortfolioUrl(e.target.value)}
                onBlur={handleBlur("portfolioUrl")}
                placeholder="https://..."
                aria-invalid={!!visibleError("portfolioUrl")}
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
            {fieldErrors.location && (
              <p className="field-error" role="alert" style={{ marginTop: "-0.5rem", marginBottom: "0.75rem" }}>
                {fieldErrors.location}
              </p>
            )}
            <FormField
              label="Skills"
              htmlFor="profile-skills"
              required
              hint="Comma separated — matched against job requirements."
              error={visibleError("skills")}
            >
              <input
                id="profile-skills"
                value={skillsCsv}
                onChange={(e) => setSkillsCsv(e.target.value)}
                onBlur={handleBlur("skills")}
                placeholder="C#, SQL Server, React"
                aria-required="true"
                aria-invalid={!!visibleError("skills")}
              />
            </FormField>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Availability & preferences</h3>
            <FormField label="Availability" htmlFor="profile-availability">
              <select
                id="profile-availability"
                value={availabilityStatus}
                onChange={(e) => setAvailabilityStatus(e.target.value)}
              >
                {AVAILABILITY_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            </FormField>
            <FormField label="Preferred job types" htmlFor="profile-pref-job-types" hint="Comma separated, e.g. FullTime, Contract">
              <input
                id="profile-pref-job-types"
                value={preferredJobTypesCsv}
                onChange={(e) => setPreferredJobTypesCsv(e.target.value)}
                placeholder="FullTime, Contract"
              />
            </FormField>
            <FormField label="Preferred locations" htmlFor="profile-pref-locations" hint="Comma separated Indian cities/states.">
              <input
                id="profile-pref-locations"
                value={preferredLocationsCsv}
                onChange={(e) => setPreferredLocationsCsv(e.target.value)}
                placeholder="Bengaluru, Karnataka"
              />
            </FormField>
            <FormField label="Preferred roles / skills focus" htmlFor="profile-pref-roles" hint="Comma separated, e.g. Backend Engineer, Team Lead">
              <input
                id="profile-pref-roles"
                value={preferredRolesCsv}
                onChange={(e) => setPreferredRolesCsv(e.target.value)}
                placeholder="Backend Engineer, Team Lead"
              />
            </FormField>
            <FormField label="Open to remote work?" htmlFor="profile-remote">
              <select
                id="profile-remote"
                value={remotePreference}
                onChange={(e) => setRemotePreference(e.target.value)}
              >
                <option value="">Not specified</option>
                <option value="yes">Yes</option>
                <option value="no">No</option>
              </select>
            </FormField>
            <div className="form-row">
              <FormField label="Expected salary — min (₹/yr)" htmlFor="profile-salary-min" error={visibleError("expectedSalaryMin")}>
                <input
                  id="profile-salary-min"
                  type="number"
                  min={0}
                  value={expectedSalaryMin}
                  onChange={(e) => setExpectedSalaryMin(e.target.value)}
                  onBlur={handleBlur("expectedSalaryMin")}
                  aria-invalid={!!visibleError("expectedSalaryMin")}
                />
              </FormField>
              <FormField label="Expected salary — max (₹/yr)" htmlFor="profile-salary-max" error={visibleError("expectedSalaryMax")}>
                <input
                  id="profile-salary-max"
                  type="number"
                  min={0}
                  value={expectedSalaryMax}
                  onChange={(e) => setExpectedSalaryMax(e.target.value)}
                  onBlur={handleBlur("expectedSalaryMax")}
                  aria-invalid={!!visibleError("expectedSalaryMax")}
                />
              </FormField>
            </div>
            <FormField label="Notice period (days)" htmlFor="profile-notice-period" error={visibleError("noticePeriodDays")}>
              <input
                id="profile-notice-period"
                type="number"
                min={0}
                max={365}
                value={noticePeriodDays}
                onChange={(e) => setNoticePeriodDays(e.target.value)}
                onBlur={handleBlur("noticePeriodDays")}
                aria-invalid={!!visibleError("noticePeriodDays")}
              />
            </FormField>
            <FormField
              label="Profile visibility"
              htmlFor="profile-visibility"
              hint="Controls when recruiters can see this profile."
            >
              <select
                id="profile-visibility"
                value={profileVisibility}
                onChange={(e) => setProfileVisibility(e.target.value)}
              >
                {VISIBILITY_OPTIONS.map((o) => (
                  <option key={o.value} value={o.value}>{o.label}</option>
                ))}
              </select>
            </FormField>
          </div>

          {profileVisibility === "PublicShareable" && <PublicProfileShareCard />}

          {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

          <div className="form-actions">
            <Button type="submit" loading={saving} disabled={saving}>Save profile</Button>
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

      <Card className="ui-card-padded">
        <h2><FileText size={18} /> Build your resume</h2>
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Create a structured, downloadable resume from your profile — separate from the uploaded file above.
        </p>
        <Link to="/resume-builder" className="btn btn-secondary btn-sm">Open Resume Builder</Link>
      </Card>

      <Card className="ui-card-padded">
        <h2><FileText size={18} /> Cover letter templates</h2>
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Build reusable cover-letter templates to personalize when you apply to jobs.
        </p>
        <Link to="/cover-letter-templates" className="btn btn-secondary btn-sm">Open Cover Letter Templates</Link>
      </Card>

      <Card className="ui-card-padded">
        <h2><FileText size={18} /> Skill assessments</h2>
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Take short, timed assessments to showcase your skills — optionally visible to recruiters.
        </p>
        <Link to="/assessments" className="btn btn-secondary btn-sm">Open Skill Assessments</Link>
      </Card>

      <Card className="ui-card-padded">
        <h2><FileText size={18} /> Career goals</h2>
        <p className="hint" style={{ marginBottom: "0.75rem" }}>
          Set career goals and track your progress toward them.
        </p>
        <Link to="/career-goals" className="btn btn-secondary btn-sm">Open Career Goals</Link>
      </Card>
    </div>
  );
}
