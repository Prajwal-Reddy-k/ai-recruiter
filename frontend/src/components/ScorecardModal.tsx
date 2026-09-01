import { useEffect, useState } from "react";
import { getMyFeedback, recommendationToNumber, saveFeedbackDraft, submitFeedback } from "../api/interviewFeedback";
import type { InterviewRecommendationValue } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import Modal from "./ui/Modal";
import Button from "./ui/Button";
import FormField from "./ui/FormField";

const RECOMMENDATION_OPTIONS: { value: InterviewRecommendationValue; label: string }[] = [
  { value: "StrongYes", label: "Strong Yes" },
  { value: "Yes", label: "Yes" },
  { value: "Neutral", label: "Neutral" },
  { value: "No", label: "No" },
  { value: "StrongNo", label: "Strong No" },
];

const SCORE_LABELS = [
  { key: "technical", label: "Technical skills" },
  { key: "communication", label: "Communication" },
  { key: "problemSolving", label: "Problem-solving" },
  { key: "cultureFit", label: "Culture / team fit" },
] as const;

interface ScorecardModalProps {
  open: boolean;
  onClose: () => void;
  interviewId: number;
  candidateName?: string;
  onSaved?: () => void;
}

export default function ScorecardModal({ open, onClose, interviewId, candidateName, onSaved }: ScorecardModalProps) {
  const toast = useToast();
  const [technical, setTechnical] = useState(3);
  const [communication, setCommunication] = useState(3);
  const [problemSolving, setProblemSolving] = useState(3);
  const [cultureFit, setCultureFit] = useState(3);
  const [recommendation, setRecommendation] = useState<InterviewRecommendationValue>("Neutral");
  const [strengths, setStrengths] = useState("");
  const [concerns, setConcerns] = useState("");
  const [privateNotes, setPrivateNotes] = useState("");
  const [alreadySubmitted, setAlreadySubmitted] = useState(false);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<"draft" | "submit" | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setLoading(true);
    setError(null);
    getMyFeedback(interviewId)
      .then((existing) => {
        if (existing) {
          setTechnical(existing.technicalScore);
          setCommunication(existing.communicationScore);
          setProblemSolving(existing.problemSolvingScore);
          setCultureFit(existing.cultureFitScore);
          setRecommendation(existing.recommendation as InterviewRecommendationValue);
          setStrengths(existing.strengths ?? "");
          setConcerns(existing.concerns ?? "");
          setPrivateNotes(existing.privateNotes ?? "");
          setAlreadySubmitted(!existing.isDraft);
        }
      })
      .catch((err) => setError(getErrorMessage(err, "Failed to load your feedback")))
      .finally(() => setLoading(false));
  }, [open, interviewId]);

  function buildPayload() {
    return {
      technicalScore: technical,
      communicationScore: communication,
      problemSolvingScore: problemSolving,
      cultureFitScore: cultureFit,
      recommendation: recommendationToNumber[recommendation],
      strengths: strengths || undefined,
      concerns: concerns || undefined,
      privateNotes: privateNotes || undefined,
    };
  }

  async function handleSaveDraft() {
    setSaving("draft");
    setError(null);
    try {
      await saveFeedbackDraft(interviewId, buildPayload());
      toast.success("Draft saved.");
      onSaved?.();
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Failed to save draft"));
    } finally {
      setSaving(null);
    }
  }

  async function handleSubmit() {
    setSaving("submit");
    setError(null);
    try {
      await submitFeedback(interviewId, buildPayload());
      toast.success(alreadySubmitted ? "Feedback updated." : "Feedback submitted.");
      onSaved?.();
      onClose();
    } catch (err) {
      setError(getErrorMessage(err, "Failed to submit feedback"));
    } finally {
      setSaving(null);
    }
  }

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={`Interview Feedback${candidateName ? ` — ${candidateName}` : ""}`}
      footer={
        <>
          <Button variant="secondary" loading={saving === "draft"} disabled={loading} onClick={handleSaveDraft}>Save draft</Button>
          <Button loading={saving === "submit"} disabled={loading} onClick={handleSubmit}>{alreadySubmitted ? "Save changes" : "Submit feedback"}</Button>
        </>
      }
    >
      {loading ? (
        <p className="hint">Loading...</p>
      ) : (
        <>
          {alreadySubmitted && (
            <p className="hint" style={{ marginBottom: "1rem" }}>
              This scorecard was already submitted — editing it now will be recorded as an edit history entry.
            </p>
          )}

          {SCORE_LABELS.map(({ key, label }) => {
            const value = key === "technical" ? technical : key === "communication" ? communication : key === "problemSolving" ? problemSolving : cultureFit;
            const setValue = key === "technical" ? setTechnical : key === "communication" ? setCommunication : key === "problemSolving" ? setProblemSolving : setCultureFit;
            return (
              <FormField key={key} label={`${label} (1-5)`} htmlFor={`score-${key}`}>
                <input
                  id={`score-${key}`}
                  type="range"
                  min={1}
                  max={5}
                  value={value}
                  onChange={(e) => setValue(Number(e.target.value))}
                  aria-valuetext={`${value} out of 5`}
                />
                <span className="hint">{value} / 5</span>
              </FormField>
            );
          })}

          <FormField label="Overall recommendation" htmlFor="recommendation">
            <select id="recommendation" value={recommendation} onChange={(e) => setRecommendation(e.target.value as InterviewRecommendationValue)}>
              {RECOMMENDATION_OPTIONS.map((opt) => <option key={opt.value} value={opt.value}>{opt.label}</option>)}
            </select>
          </FormField>

          <FormField label="Strengths" htmlFor="strengths">
            <textarea id="strengths" value={strengths} onChange={(e) => setStrengths(e.target.value)} rows={2} />
          </FormField>
          <FormField label="Concerns" htmlFor="concerns">
            <textarea id="concerns" value={concerns} onChange={(e) => setConcerns(e.target.value)} rows={2} />
          </FormField>
          <FormField label="Private interviewer notes" htmlFor="private-notes" hint="Never shown to the candidate.">
            <textarea id="private-notes" value={privateNotes} onChange={(e) => setPrivateNotes(e.target.value)} rows={2} />
          </FormField>

          {error && <p className="error">{error}</p>}
        </>
      )}
    </Modal>
  );
}
