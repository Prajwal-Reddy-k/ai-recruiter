import { useEffect, useState, type FormEvent } from "react";
import { User } from "lucide-react";
import { getMyDetails, updateMyDetails } from "../api/account";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { useAuth } from "../context/AuthContext";
import Card from "./ui/Card";
import Button from "./ui/Button";
import FormField from "./ui/FormField";

interface DetailsFieldErrors {
  fullName?: string;
  phoneNumber?: string;
  general?: string;
}

/** Shared "Your details" (full name / phone / read-only email) form, used on both the
 * recruiter's Company Setup page and the general Account Settings page — the backend
 * endpoint (api/account/details) is already role-agnostic. */
export default function YourDetailsCard() {
  const { updateFullName } = useAuth();
  const toast = useToast();
  const [fullName, setFullName] = useState("");
  const [phoneNumber, setPhoneNumber] = useState("");
  const [email, setEmail] = useState("");
  const [initializing, setInitializing] = useState(true);
  const [saving, setSaving] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<DetailsFieldErrors>({});

  useEffect(() => {
    getMyDetails()
      .then((details) => {
        setFullName(details.fullName);
        setPhoneNumber(details.phoneNumber ?? "");
        setEmail(details.email);
      })
      .finally(() => setInitializing(false));
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});

    if (!fullName.trim()) {
      setFieldErrors({ fullName: "Full name is required." });
      return;
    }

    setSaving(true);
    try {
      const updated = await updateMyDetails({ fullName: fullName.trim(), phoneNumber: phoneNumber.trim() || undefined });
      setFullName(updated.fullName);
      setPhoneNumber(updated.phoneNumber ?? "");
      updateFullName(updated.fullName);
      toast.success("Your details were updated.");
    } catch (err) {
      const serverFieldErrors = getFieldErrors(err);
      if (serverFieldErrors) {
        setFieldErrors(serverFieldErrors as DetailsFieldErrors);
      } else {
        setFieldErrors({ general: getErrorMessage(err, "Failed to save your details") });
      }
    } finally {
      setSaving(false);
    }
  }

  if (initializing) return null;

  return (
    <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
      <h2><User size={18} style={{ verticalAlign: "-3px", marginRight: "0.4rem" }} />Your details</h2>
      <p className="hint" style={{ marginBottom: "1rem" }}>Your personal name and phone number.</p>
      <form onSubmit={handleSubmit} noValidate>
        <FormField label="Full name" htmlFor="details-fullname" required error={fieldErrors.fullName}>
          <input id="details-fullname" value={fullName} onChange={(e) => setFullName(e.target.value)} aria-required="true" />
        </FormField>
        <FormField label="Phone" htmlFor="details-phone" hint="10-digit Indian mobile number." error={fieldErrors.phoneNumber}>
          <input id="details-phone" type="tel" value={phoneNumber} onChange={(e) => setPhoneNumber(e.target.value)} placeholder="9876543210" />
        </FormField>
        <FormField label="Email" htmlFor="details-email" hint="Contact support to change your email.">
          <input id="details-email" value={email} disabled />
        </FormField>

        {fieldErrors.general && <p className="error" style={{ marginBottom: "1rem" }}>{fieldErrors.general}</p>}

        <div className="form-actions">
          <Button type="submit" loading={saving}>Save details</Button>
        </div>
      </form>
    </Card>
  );
}
