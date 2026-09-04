import { useEffect, useState, type FormEvent } from "react";
import { isAxiosError } from "axios";
import { ShieldCheck } from "lucide-react";
import { getMyCompanyVerificationStatus, submitCompanyVerification } from "../api/companyVerification";
import type { CompanyVerificationStatusDto } from "../types";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import Card from "./ui/Card";
import Button from "./ui/Button";
import FormField from "./ui/FormField";
import { StatusBadge } from "./ui/Badge";
import IndiaLocationSelector from "./IndiaLocationSelector";

/** Company verification submission + status, shown on the recruiter's Company Setup page.
 * Only the company Owner can submit (enforced server-side) — a non-owner sees a read-only
 * message instead of the form. */
export default function CompanyVerificationCard() {
  const toast = useToast();
  const [status, setStatus] = useState<CompanyVerificationStatusDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [forbidden, setForbidden] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [formOpen, setFormOpen] = useState(false);

  const [website, setWebsite] = useState("");
  const [businessEmail, setBusinessEmail] = useState("");
  const [state, setState] = useState("");
  const [city, setCity] = useState("");
  const [description, setDescription] = useState("");
  const [documentReference, setDocumentReference] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  function loadStatus() {
    setLoading(true);
    getMyCompanyVerificationStatus()
      .then((data) => {
        setStatus(data);
        setForbidden(false);
      })
      .catch((err) => {
        if (isAxiosError(err) && err.response?.status === 403) setForbidden(true);
        else toast.error(getErrorMessage(err, "Failed to load verification status"));
      })
      .finally(() => setLoading(false));
  }

  useEffect(loadStatus, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setSubmitting(true);
    try {
      const result = await submitCompanyVerification({
        website: website || undefined,
        businessEmail,
        state: state || undefined,
        city: city || undefined,
        description: description || undefined,
        verificationDocumentReference: documentReference || undefined,
      });
      setStatus(result);
      setFormOpen(false);
      toast.success("Verification submitted — an admin will review it shortly.");
    } catch (err) {
      const errors = getFieldErrors(err);
      if (errors) {
        setFieldErrors(errors);
      } else {
        toast.error(getErrorMessage(err, "Failed to submit for verification"));
      }
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) return null;
  if (forbidden) {
    return (
      <Card className="ui-card-padded" style={{ marginTop: "1rem" }}>
        <h3><ShieldCheck size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />Company verification</h3>
        <p className="hint" style={{ marginTop: "0.5rem" }}>Only your company's owner can submit for platform verification.</p>
      </Card>
    );
  }
  if (!status) return null;

  const canSubmit = status.status === "NotSubmitted" || status.status === "Rejected" || status.status === "NeedsMoreInfo";

  return (
    <Card className="ui-card-padded" style={{ marginTop: "1rem" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
        <h3><ShieldCheck size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />Company verification</h3>
        <StatusBadge status={status.status} />
      </div>

      <p className="hint" style={{ marginTop: "0.5rem" }}>
        Verified companies get a "Platform Verified" badge on their public page and job listings. This is a platform review, not a government or legal verification.
      </p>

      {status.note && (
        <p className={status.status === "Rejected" ? "error" : "hint"} style={{ marginTop: "0.5rem" }}>
          Admin note: {status.note}
        </p>
      )}

      {status.status === "Pending" && (
        <p className="hint" style={{ marginTop: "0.5rem" }}>
          You can keep using the platform as usual while your submission is under review.
        </p>
      )}

      {canSubmit && !formOpen && (
        <Button variant="secondary" style={{ marginTop: "0.75rem" }} onClick={() => setFormOpen(true)}>
          {status.status === "NotSubmitted" ? "Submit for verification" : "Resubmit for verification"}
        </Button>
      )}

      {canSubmit && formOpen && (
        <form onSubmit={handleSubmit} noValidate style={{ marginTop: "1rem" }}>
          <FormField label="Business email" htmlFor="verify-business-email" required error={fieldErrors.businessEmail}>
            <input
              id="verify-business-email"
              type="email"
              value={businessEmail}
              onChange={(e) => setBusinessEmail(e.target.value)}
              placeholder="hr@yourcompany.com"
            />
          </FormField>
          <FormField label="Website" htmlFor="verify-website" error={fieldErrors.website}>
            <input id="verify-website" value={website} onChange={(e) => setWebsite(e.target.value)} placeholder="https://" />
          </FormField>
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
            stateError={fieldErrors.city}
            cityError={fieldErrors.city}
          />
          <FormField label="Company description" htmlFor="verify-description" required error={fieldErrors.description} hint="At least 20 characters.">
            <textarea id="verify-description" value={description} onChange={(e) => setDescription(e.target.value)} rows={3} />
          </FormField>
          <FormField label="Supporting document reference" htmlFor="verify-document" hint="Optional — e.g. a CIN/registration number or a link to a public filing.">
            <input id="verify-document" value={documentReference} onChange={(e) => setDocumentReference(e.target.value)} />
          </FormField>
          <div className="form-actions">
            <Button type="button" variant="secondary" onClick={() => setFormOpen(false)}>Cancel</Button>
            <Button type="submit" loading={submitting}>Submit</Button>
          </div>
        </form>
      )}
    </Card>
  );
}
