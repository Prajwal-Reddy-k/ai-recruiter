import type { ScreeningQuestion, SubmitScreeningAnswerRequest } from "../types";
import FormField from "./ui/FormField";

export interface ScreeningAnswerValue {
  textValue?: string;
  numberValue?: number;
  selectedOptionIds?: number[];
}

interface ScreeningQuestionsFormProps {
  questions: ScreeningQuestion[];
  /** Keyed by question id. */
  values: Record<number, ScreeningAnswerValue>;
  onChange?: (questionId: number, value: ScreeningAnswerValue) => void;
  /** Keyed by "question_<id>" — matches the backend's field-error convention. */
  errors?: Record<string, string>;
  /** Read-only mode for the recruiter's "preview as candidate" — inputs are disabled and
   * nothing is ever submitted, so preferredAnswer is simply never rendered here either way. */
  readOnly?: boolean;
}

/** Renders a job's screening questions either as the candidate's interactive apply-time
 * answer form, or as the recruiter's read-only "preview as candidate" — same markup either
 * way, so what a recruiter previews always matches what a candidate actually sees. */
export default function ScreeningQuestionsForm({ questions, values, onChange, errors, readOnly = false }: ScreeningQuestionsFormProps) {
  if (questions.length === 0) return null;

  function update(questionId: number, value: ScreeningAnswerValue) {
    onChange?.(questionId, value);
  }

  return (
    <div className="screening-questions-form">
      <h3 className="form-section-title">Application Questions</h3>
      {[...questions].sort((a, b) => a.displayOrder - b.displayOrder).map((q) => {
        const value = values[q.id] ?? {};
        const error = errors?.[`question_${q.id}`];
        const fieldId = `screening-question-${q.id}`;

        return (
          <FormField key={q.id} label={q.questionText} htmlFor={fieldId} required={q.isRequired} hint={q.helpText ?? undefined} error={error}>
            {q.questionType === "ShortText" && (
              <input id={fieldId} value={value.textValue ?? ""} disabled={readOnly} maxLength={500}
                onChange={(e) => update(q.id, { textValue: e.target.value })} />
            )}
            {q.questionType === "LongText" && (
              <textarea id={fieldId} value={value.textValue ?? ""} disabled={readOnly} rows={4} maxLength={3000}
                onChange={(e) => update(q.id, { textValue: e.target.value })} />
            )}
            {q.questionType === "Number" && (
              <input id={fieldId} type="number" value={value.numberValue ?? ""} disabled={readOnly}
                onChange={(e) => update(q.id, { numberValue: e.target.value === "" ? undefined : Number(e.target.value) })} />
            )}
            {q.questionType === "Url" && (
              <input id={fieldId} type="url" value={value.textValue ?? ""} disabled={readOnly} placeholder="https://"
                onChange={(e) => update(q.id, { textValue: e.target.value })} />
            )}
            {q.questionType === "YesNo" && (
              <div className="filter-option-group" role="radiogroup" aria-labelledby={fieldId}>
                {["Yes", "No"].map((option) => (
                  <label key={option} className="filter-option">
                    <input
                      type="radio"
                      name={fieldId}
                      checked={value.textValue === option}
                      disabled={readOnly}
                      onChange={() => update(q.id, { textValue: option })}
                    />
                    {option}
                  </label>
                ))}
              </div>
            )}
            {q.questionType === "SingleChoice" && (
              <div className="filter-option-group" role="radiogroup" aria-labelledby={fieldId}>
                {q.options.map((opt) => (
                  <label key={opt.id} className="filter-option">
                    <input
                      type="radio"
                      name={fieldId}
                      checked={value.selectedOptionIds?.[0] === opt.id}
                      disabled={readOnly}
                      onChange={() => update(q.id, { selectedOptionIds: [opt.id] })}
                    />
                    {opt.optionText}
                  </label>
                ))}
              </div>
            )}
            {q.questionType === "MultipleChoice" && (
              <div className="filter-option-group">
                {q.options.map((opt) => {
                  const selected = value.selectedOptionIds ?? [];
                  const checked = selected.includes(opt.id);
                  return (
                    <label key={opt.id} className="filter-option">
                      <input
                        type="checkbox"
                        checked={checked}
                        disabled={readOnly}
                        onChange={(e) =>
                          update(q.id, {
                            selectedOptionIds: e.target.checked ? [...selected, opt.id] : selected.filter((id) => id !== opt.id),
                          })
                        }
                      />
                      {opt.optionText}
                    </label>
                  );
                })}
              </div>
            )}
          </FormField>
        );
      })}
    </div>
  );
}

/** Builds the API request payload from the form's local value map — omits questions with no
 * answer at all so an untouched optional question isn't sent as an empty answer. */
export function buildScreeningAnswerRequests(values: Record<number, ScreeningAnswerValue>): SubmitScreeningAnswerRequest[] {
  return Object.entries(values)
    .filter(([, v]) => v.textValue !== undefined || v.numberValue !== undefined || (v.selectedOptionIds && v.selectedOptionIds.length > 0))
    .map(([questionId, v]) => ({
      questionId: Number(questionId),
      textValue: v.textValue,
      numberValue: v.numberValue,
      selectedOptionIds: v.selectedOptionIds,
    }));
}
