import { useState } from "react";
import { ArrowDown, ArrowUp, Copy, Eye, Plus, Trash2 } from "lucide-react";
import type { ScreeningQuestion, ScreeningQuestionType, UpsertScreeningQuestionRequest } from "../types";
import FormField from "./ui/FormField";
import Button from "./ui/Button";
import Switch from "./ui/Switch";
import Modal from "./ui/Modal";
import ScreeningQuestionsForm from "./ScreeningQuestionsForm";

const QUESTION_TYPE_OPTIONS: { value: ScreeningQuestionType; label: string }[] = [
  { value: "ShortText", label: "Short Text" },
  { value: "LongText", label: "Long Text" },
  { value: "YesNo", label: "Yes / No" },
  { value: "SingleChoice", label: "Single Choice" },
  { value: "MultipleChoice", label: "Multiple Choice" },
  { value: "Number", label: "Number" },
  { value: "Url", label: "URL" },
];

const CHOICE_TYPES: ScreeningQuestionType[] = ["SingleChoice", "MultipleChoice"];
const MAX_QUESTIONS = 10;

function emptyQuestion(displayOrder: number): UpsertScreeningQuestionRequest {
  return { questionText: "", questionType: "ShortText", isRequired: true, options: [], displayOrder };
}

interface ScreeningQuestionsEditorProps {
  questions: UpsertScreeningQuestionRequest[];
  onChange: (questions: UpsertScreeningQuestionRequest[]) => void;
  /** Question ids that already have at least one submitted answer — their type/options can't
   * be changed and they can't be deleted, mirroring the backend's historical-integrity rule. */
  answeredQuestionIds?: number[];
}

export default function ScreeningQuestionsEditor({ questions, onChange, answeredQuestionIds = [] }: ScreeningQuestionsEditorProps) {
  const [previewOpen, setPreviewOpen] = useState(false);

  function renumber(list: UpsertScreeningQuestionRequest[]): UpsertScreeningQuestionRequest[] {
    return list.map((q, i) => ({ ...q, displayOrder: i }));
  }

  function updateAt(index: number, patch: Partial<UpsertScreeningQuestionRequest>) {
    const next = questions.map((q, i) => (i === index ? { ...q, ...patch } : q));
    onChange(next);
  }

  function addQuestion() {
    onChange(renumber([...questions, emptyQuestion(questions.length)]));
  }

  function duplicateAt(index: number) {
    const source = questions[index];
    const copy: UpsertScreeningQuestionRequest = { ...source, id: undefined, questionText: `${source.questionText} (copy)` };
    const next = [...questions.slice(0, index + 1), copy, ...questions.slice(index + 1)];
    onChange(renumber(next));
  }

  function deleteAt(index: number) {
    onChange(renumber(questions.filter((_, i) => i !== index)));
  }

  function moveAt(index: number, direction: -1 | 1) {
    const target = index + direction;
    if (target < 0 || target >= questions.length) return;
    const next = [...questions];
    [next[index], next[target]] = [next[target], next[index]];
    onChange(renumber(next));
  }

  function updateOption(index: number, optionIndex: number, text: string) {
    const options = [...(questions[index].options ?? [])];
    options[optionIndex] = text;
    updateAt(index, { options });
  }

  function addOption(index: number) {
    updateAt(index, { options: [...(questions[index].options ?? []), ""] });
  }

  function removeOption(index: number, optionIndex: number) {
    updateAt(index, { options: (questions[index].options ?? []).filter((_, i) => i !== optionIndex) });
  }

  const previewQuestions: ScreeningQuestion[] = questions.map((q, i) => ({
    id: q.id ?? -(i + 1),
    questionText: q.questionText || "(untitled question)",
    questionType: q.questionType,
    isRequired: q.isRequired,
    helpText: q.helpText ?? null,
    displayOrder: q.displayOrder,
    options: (q.options ?? []).map((text, oi) => ({ id: -(oi + 1), optionText: text, displayOrder: oi })),
  }));

  return (
    <div>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "0.75rem" }}>
        <p className="form-section-desc" style={{ margin: 0 }}>
          Add up to {MAX_QUESTIONS} questions candidates must answer to apply. {questions.length}/{MAX_QUESTIONS} used.
        </p>
        {questions.length > 0 && (
          <Button type="button" variant="secondary" size="sm" icon={<Eye size={14} />} onClick={() => setPreviewOpen(true)}>
            Preview as candidate
          </Button>
        )}
      </div>

      {questions.map((q, i) => {
        const isAnswered = q.id !== undefined && answeredQuestionIds.includes(q.id);
        return (
          <div key={q.id ?? `new-${i}`} className="job-card" style={{ marginBottom: "1rem" }}>
            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", gap: "0.5rem" }}>
              <FormField label="Question text" htmlFor={`sq-text-${i}`} required>
                <input id={`sq-text-${i}`} value={q.questionText} maxLength={300} onChange={(e) => updateAt(i, { questionText: e.target.value })} />
              </FormField>
              <div style={{ display: "flex", gap: "0.25rem", marginTop: "1.6rem" }}>
                <button type="button" className="link-button" onClick={() => moveAt(i, -1)} disabled={i === 0} aria-label="Move up"><ArrowUp size={14} /></button>
                <button type="button" className="link-button" onClick={() => moveAt(i, 1)} disabled={i === questions.length - 1} aria-label="Move down"><ArrowDown size={14} /></button>
                <button type="button" className="link-button" onClick={() => duplicateAt(i)} aria-label="Duplicate question"><Copy size={14} /></button>
                <button
                  type="button"
                  className="link-button"
                  onClick={() => deleteAt(i)}
                  disabled={isAnswered}
                  aria-label="Delete question"
                  title={isAnswered ? "This question already has candidate answers and can't be deleted." : undefined}
                >
                  <Trash2 size={14} />
                </button>
              </div>
            </div>

            <div className="form-row">
              <FormField label="Question type" htmlFor={`sq-type-${i}`}>
                <select
                  id={`sq-type-${i}`}
                  value={q.questionType}
                  disabled={isAnswered}
                  title={isAnswered ? "This question already has candidate answers and its type can't be changed." : undefined}
                  onChange={(e) => updateAt(i, { questionType: e.target.value as ScreeningQuestionType })}
                >
                  {QUESTION_TYPE_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
                </select>
              </FormField>
              <FormField label="Required" htmlFor={`sq-required-${i}`}>
                <Switch checked={q.isRequired} onChange={(v) => updateAt(i, { isRequired: v })} label="Required" />
              </FormField>
            </div>

            <FormField label="Help text" htmlFor={`sq-help-${i}`} hint="Optional — shown to candidates under the question.">
              <input id={`sq-help-${i}`} value={q.helpText ?? ""} maxLength={500} onChange={(e) => updateAt(i, { helpText: e.target.value })} />
            </FormField>

            {CHOICE_TYPES.includes(q.questionType) && (
              <FormField label="Options" htmlFor={`sq-options-${i}`} hint="At least two, no duplicates.">
                <div style={{ display: "flex", flexDirection: "column", gap: "0.4rem" }}>
                  {(q.options ?? []).map((option, oi) => (
                    <div key={oi} style={{ display: "flex", gap: "0.5rem" }}>
                      <input value={option} disabled={isAnswered} onChange={(e) => updateOption(i, oi, e.target.value)} />
                      <button type="button" className="link-button" disabled={isAnswered} onClick={() => removeOption(i, oi)}>Remove</button>
                    </div>
                  ))}
                  <Button type="button" variant="secondary" size="sm" icon={<Plus size={14} />} disabled={isAnswered} onClick={() => addOption(i)}>
                    Add option
                  </Button>
                </div>
              </FormField>
            )}

            <FormField label="Preferred answer" htmlFor={`sq-preferred-${i}`} hint="Optional — for your own reference only. Candidates never see this.">
              <input id={`sq-preferred-${i}`} value={q.preferredAnswer ?? ""} onChange={(e) => updateAt(i, { preferredAnswer: e.target.value })} />
            </FormField>
          </div>
        );
      })}

      <Button type="button" variant="secondary" icon={<Plus size={16} />} disabled={questions.length >= MAX_QUESTIONS} onClick={addQuestion}>
        Add question
      </Button>

      <Modal open={previewOpen} onClose={() => setPreviewOpen(false)} title="Preview: how candidates will see this">
        <ScreeningQuestionsForm questions={previewQuestions} values={{}} readOnly />
      </Modal>
    </div>
  );
}
