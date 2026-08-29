import { useEffect, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { Building2 } from "lucide-react";
import { getOnboardingStatus, upsertOnboarding } from "../api/recruiters";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";
import FormField from "../components/ui/FormField";
import IndiaLocationSelector from "../components/IndiaLocationSelector";

interface FieldErrors {
  companyName?: string;
  website?: string;
  state?: string;
  city?: string;
  general?: string;
}

export default function OnboardingPage() {
  const [companyName, setCompanyName] = useState("");
  const [website, setWebsite] = useState("");
  const [industry, setIndustry] = useState("");
  const [description, setDescription] = useState("");
  const [designation, setDesignation] = useState("");
  const [logoUrl, setLogoUrl] = useState("");
  const [state, setState] = useState("");
  const [city, setCity] = useState("");
  const [size, setSize] = useState("");
  const [benefits, setBenefits] = useState("");
  const [cultureHighlights, setCultureHighlights] = useState("");
  const [linkedInUrl, setLinkedInUrl] = useState("");
  const [twitterUrl, setTwitterUrl] = useState("");
  const [loading, setLoading] = useState(false);
  const [initializing, setInitializing] = useState(true);
  const [wasOnboarded, setWasOnboarded] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<FieldErrors>({});
  const navigate = useNavigate();
  const toast = useToast();

  useEffect(() => {
    getOnboardingStatus()
      .then((status) => {
        if (status.isOnboarded) {
          setCompanyName(status.companyName ?? "");
          setDesignation(status.designation ?? "");
          setWebsite(status.website ?? "");
          setIndustry(status.industry ?? "");
          setDescription(status.description ?? "");
          setLogoUrl(status.logoUrl ?? "");
          setState(status.state ?? "");
          setCity(status.city ?? "");
          setSize(status.size ?? "");
          setBenefits(status.benefits ?? "");
          setCultureHighlights(status.cultureHighlights ?? "");
          setLinkedInUrl(status.linkedInUrl ?? "");
          setTwitterUrl(status.twitterUrl ?? "");
          setWasOnboarded(true);
        }
      })
      .finally(() => setInitializing(false));
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    const clientErrors: FieldErrors = {};
    if (!companyName.trim()) clientErrors.companyName = "Company name is required.";
    if (website.trim() && !/^https?:\/\/.+/i.test(website.trim())) {
      clientErrors.website = "Website must start with http:// or https://";
    }
    if (city.trim() && !state.trim()) clientErrors.state = "Select the state for this city.";
    if (Object.keys(clientErrors).length > 0) {
      setFieldErrors(clientErrors);
      return;
    }

    setLoading(true);
    try {
      await upsertOnboarding({
        companyName,
        website: website || undefined,
        industry: industry || undefined,
        description: description || undefined,
        designation: designation || undefined,
        logoUrl: logoUrl || undefined,
        state: state || undefined,
        city: city || undefined,
        size: size || undefined,
        benefits: benefits || undefined,
        cultureHighlights: cultureHighlights || undefined,
        linkedInUrl: linkedInUrl || undefined,
        twitterUrl: twitterUrl || undefined,
      });
      if (wasOnboarded) {
        toast.success("Company profile updated.");
      } else {
        navigate("/post-job");
      }
    } catch (err) {
      setFieldErrors({ general: getErrorMessage(err, "Failed to save company details") });
    } finally {
      setLoading(false);
    }
  }

  if (initializing) return <p>Loading...</p>;

  return (
    <div style={{ maxWidth: 560, margin: "0 auto" }}>
      <div className="page-header">
        <h1><Building2 size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Company Setup</h1>
        <p>{wasOnboarded ? "Update your company details anytime." : "Complete your company profile before posting jobs."}</p>
      </div>

      <Card className="ui-card-padded">
        <form onSubmit={handleSubmit} noValidate>
          <div className="form-section">
            <h3 className="form-section-title">Company</h3>
            <FormField label="Company name" htmlFor="onboarding-company" required error={fieldErrors.companyName}>
              <input id="onboarding-company" value={companyName} onChange={(e) => setCompanyName(e.target.value)} />
            </FormField>
            <FormField label="Your designation" htmlFor="onboarding-designation">
              <input id="onboarding-designation" value={designation} onChange={(e) => setDesignation(e.target.value)} placeholder="e.g. Talent Acquisition Lead" />
            </FormField>
            <FormField label="Logo URL" htmlFor="onboarding-logo" hint="Optional — a placeholder logo is used when left blank.">
              <input id="onboarding-logo" value={logoUrl} onChange={(e) => setLogoUrl(e.target.value)} placeholder="https://..." />
            </FormField>
            <FormField label="Company size" htmlFor="onboarding-size">
              <input id="onboarding-size" value={size} onChange={(e) => setSize(e.target.value)} placeholder="e.g. 51-200 employees" />
            </FormField>
          </div>

          <div className="form-section">
            <h3 className="form-section-title">Location</h3>
            <IndiaLocationSelector
              state={state}
              city={city}
              locality=""
              isRemote={false}
              onStateChange={setState}
              onCityChange={setCity}
              onLocalityChange={() => {}}
              onIsRemoteChange={() => {}}
              showRemoteOption={false}
              stateError={fieldErrors.state}
              cityError={fieldErrors.city}
            />
          </div>

          <div className="form-section">
            <h3 className="form-section-title">About</h3>
            <FormField label="Website" htmlFor="onboarding-website" error={fieldErrors.website}>
              <input id="onboarding-website" value={website} onChange={(e) => setWebsite(e.target.value)} placeholder="https://" />
            </FormField>
            <FormField label="Industry" htmlFor="onboarding-industry">
              <input id="onboarding-industry" value={industry} onChange={(e) => setIndustry(e.target.value)} />
            </FormField>
            <FormField label="About the company" htmlFor="onboarding-description">
              <textarea id="onboarding-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={4} />
            </FormField>
            <FormField label="Benefits" htmlFor="onboarding-benefits" hint="Comma separated, e.g. Health insurance, WFH stipend">
              <input id="onboarding-benefits" value={benefits} onChange={(e) => setBenefits(e.target.value)} />
            </FormField>
            <FormField label="Culture highlights" htmlFor="onboarding-culture">
              <textarea id="onboarding-culture" value={cultureHighlights} onChange={(e) => setCultureHighlights(e.target.value)} rows={2} />
            </FormField>
            <div className="form-row">
              <FormField label="LinkedIn URL" htmlFor="onboarding-linkedin">
                <input id="onboarding-linkedin" value={linkedInUrl} onChange={(e) => setLinkedInUrl(e.target.value)} placeholder="https://linkedin.com/company/..." />
              </FormField>
              <FormField label="Twitter/X URL" htmlFor="onboarding-twitter">
                <input id="onboarding-twitter" value={twitterUrl} onChange={(e) => setTwitterUrl(e.target.value)} placeholder="https://x.com/..." />
              </FormField>
            </div>
          </div>

          {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

          <div className="form-actions">
            <Button type="submit" loading={loading}>{wasOnboarded ? "Save changes" : "Save and continue"}</Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
