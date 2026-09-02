import { useState, type FormEvent } from "react";
import type { Offer, SalaryTypeName, UpsertOfferRequest } from "../types";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import Modal from "./ui/Modal";
import Button from "./ui/Button";
import FormField from "./ui/FormField";
import IndiaLocationSelector from "./IndiaLocationSelector";

const EMPLOYMENT_TYPES = ["FullTime", "PartTime", "Contract", "Internship", "Freelance"];

function dateInput(value: string | undefined): string {
  return value ? value.slice(0, 10) : "";
}

interface OfferFormModalProps {
  initial: Offer | null;
  onClose: () => void;
  onSave: (data: UpsertOfferRequest) => Promise<void>;
}

export default function OfferFormModal({ initial, onClose, onSave }: OfferFormModalProps) {
  const [offeredSalary, setOfferedSalary] = useState(initial ? String(initial.offeredSalary) : "");
  const [salaryType, setSalaryType] = useState<SalaryTypeName>(initial?.salaryType ?? "Annual");
  const [joiningDate, setJoiningDate] = useState(dateInput(initial?.joiningDate));
  const [state, setState] = useState(initial?.workState ?? "");
  const [city, setCity] = useState(initial?.workCity ?? "");
  const [locality, setLocality] = useState("");
  const [isRemote, setIsRemote] = useState(initial?.isRemote ?? false);
  const [employmentType, setEmploymentType] = useState(initial?.employmentType ?? "FullTime");
  const [probationDetails, setProbationDetails] = useState(initial?.probationDetails ?? "");
  const [benefits, setBenefits] = useState(initial?.benefits ?? "");
  const [expiryDateUtc, setExpiryDateUtc] = useState(dateInput(initial?.expiryDateUtc));
  const [recruiterMessage, setRecruiterMessage] = useState(initial?.recruiterMessage ?? "");
  const [saving, setSaving] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setErrors({});
    try {
      await onSave({
        offeredSalary: Number(offeredSalary),
        salaryType,
        joiningDate,
        workCity: city || undefined,
        workState: state || undefined,
        isRemote,
        employmentType,
        probationDetails: probationDetails || undefined,
        benefits: benefits || undefined,
        expiryDateUtc,
        recruiterMessage: recruiterMessage || undefined,
      });
    } catch (err) {
      const fe = getFieldErrors(err);
      if (fe) setErrors(fe);
      else setErrors({ general: getErrorMessage(err, "Failed to save offer") });
    } finally {
      setSaving(false);
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      title={initial ? "Edit offer" : "Create offer"}
      footer={<><Button variant="secondary" onClick={onClose}>Cancel</Button><Button onClick={handleSubmit} loading={saving}>Save draft</Button></>}
    >
      <form onSubmit={handleSubmit} noValidate>
        <div className="form-row">
          <FormField label="Offered salary" htmlFor="offer-salary" required error={errors.offeredSalary}>
            <input id="offer-salary" type="number" min={0} value={offeredSalary} onChange={(e) => setOfferedSalary(e.target.value)} />
          </FormField>
          <FormField label="Salary type" htmlFor="offer-salary-type" error={errors.salaryType}>
            <select id="offer-salary-type" value={salaryType} onChange={(e) => setSalaryType(e.target.value as SalaryTypeName)}>
              <option value="Annual">Annual (CTC)</option>
              <option value="Monthly">Monthly</option>
            </select>
          </FormField>
        </div>

        <div className="form-row">
          <FormField label="Joining date" htmlFor="offer-joining" required error={errors.joiningDate}>
            <input id="offer-joining" type="date" value={joiningDate} onChange={(e) => setJoiningDate(e.target.value)} />
          </FormField>
          <FormField label="Offer expiry date" htmlFor="offer-expiry" required error={errors.expiryDateUtc}>
            <input id="offer-expiry" type="date" value={expiryDateUtc} onChange={(e) => setExpiryDateUtc(e.target.value)} />
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
          stateError={errors.workState}
          cityError={errors.workCity}
          showRemoteOption
        />

        <FormField label="Employment type" htmlFor="offer-employment-type" error={errors.employmentType}>
          <select id="offer-employment-type" value={employmentType} onChange={(e) => setEmploymentType(e.target.value)}>
            {EMPLOYMENT_TYPES.map((t) => <option key={t} value={t}>{t.replace(/([a-z])([A-Z])/g, "$1 $2")}</option>)}
          </select>
        </FormField>

        <FormField label="Probation details" htmlFor="offer-probation" error={errors.probationDetails}>
          <textarea id="offer-probation" rows={2} value={probationDetails} onChange={(e) => setProbationDetails(e.target.value)} placeholder="e.g. 3 months, confirmable on satisfactory performance" />
        </FormField>

        <FormField label="Benefits" htmlFor="offer-benefits" error={errors.benefits}>
          <textarea id="offer-benefits" rows={3} value={benefits} onChange={(e) => setBenefits(e.target.value)} />
        </FormField>

        <FormField label="Message to candidate" htmlFor="offer-message" error={errors.recruiterMessage}>
          <textarea id="offer-message" rows={3} value={recruiterMessage} onChange={(e) => setRecruiterMessage(e.target.value)} />
        </FormField>

        {errors.general && <p className="error">{errors.general}</p>}
      </form>
    </Modal>
  );
}
