import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { CheckCircle2, XCircle } from "lucide-react";
import { getActiveAssessmentAttempt, answerAssessmentQuestion, submitAssessmentAttempt, setAssessmentAttemptVisibility } from "../api/assessments";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { AssessmentAttemptInProgress, AssessmentAttemptResult } from "../types";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";

function formatRemaining(ms: number): string {
  const totalSeconds = Math.max(0, Math.floor(ms / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

export default function AssessmentAttemptPage() {
  const { attemptId } = useParams();
  const navigate = useNavigate();
  const toast = useToast();
  const [attempt, setAttempt] = useState<AssessmentAttemptInProgress | null>(null);
  const [selections, setSelections] = useState<Record<number, number>>({});
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState<AssessmentAttemptResult | null>(null);
  const [remainingMs, setRemainingMs] = useState(0);
  const submittedRef = useRef(false);

  useEffect(() => {
    if (!attemptId) return;
    getActiveAssessmentAttempt(Number(attemptId))
      .then((a) => {
        setAttempt(a);
        setSelections(a.selectedOptionsByAnswerId ?? {});
      })
      .catch((err) => toast.error(getErrorMessage(err, "Couldn't load this attempt")))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [attemptId]);

  const handleSubmit = useCallback(async () => {
    if (!attemptId || submittedRef.current) return;
    submittedRef.current = true;
    setSubmitting(true);
    try {
      const res = await submitAssessmentAttempt(Number(attemptId));
      setResult(res);
      toast.success("Assessment submitted.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to submit assessment"));
      submittedRef.current = false;
    } finally {
      setSubmitting(false);
    }
  }, [attemptId, toast]);

  useEffect(() => {
    if (!attempt || result) return;
    const tick = () => {
      const remaining = new Date(attempt.expiresAtUtc).getTime() - Date.now();
      setRemainingMs(remaining);
      if (remaining <= 0) {
        void handleSubmit();
      }
    };
    tick();
    const interval = setInterval(tick, 1000);
    return () => clearInterval(interval);
  }, [attempt, result, handleSubmit]);

  async function handleSelect(answerId: number, optionIndex: number) {
    if (!attemptId) return;
    setSelections((prev) => ({ ...prev, [answerId]: optionIndex }));
    try {
      await answerAssessmentQuestion(Number(attemptId), answerId, optionIndex);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to save your answer"));
    }
  }

  async function handleToggleVisibility(visible: boolean) {
    if (!result) return;
    try {
      await setAssessmentAttemptVisibility(result.attemptId, visible);
      setResult({ ...result, isVisibleToRecruiters: visible });
      toast.success(visible ? "Recruiters can now see this result." : "This result is now private.");
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to update visibility"));
    }
  }

  if (loading) return <p>Loading...</p>;

  if (result) {
    const options = ["A", "B", "C", "D"];
    return (
      <div>
        <div className="page-header">
          <h1>{result.category} — {result.percentageScore}%</h1>
          <p>{result.scoreCorrectCount} of {result.totalQuestionCount} correct.</p>
        </div>

        <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
          <label className="filter-option">
            <input
              type="checkbox"
              checked={result.isVisibleToRecruiters}
              onChange={(e) => handleToggleVisibility(e.target.checked)}
            />
            Let recruiters see this result as a badge on my profile
          </label>
        </Card>

        <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
          {result.review.map((q, i) => (
            <Card key={i} className="ui-card-padded">
              <p style={{ fontWeight: 600 }}>{i + 1}. {q.questionText}</p>
              <div className="chip-list" style={{ marginTop: "0.5rem" }}>
                {[q.optionA, q.optionB, q.optionC, q.optionD].map((opt, idx) => (
                  <span
                    key={idx}
                    className={`chip ${idx === q.correctOptionIndex ? "chip-matched" : idx === q.selectedOptionIndex ? "chip-missing" : ""}`}
                  >
                    {idx === q.correctOptionIndex && <CheckCircle2 size={13} style={{ verticalAlign: "-2px", marginRight: "0.2rem" }} />}
                    {idx === q.selectedOptionIndex && idx !== q.correctOptionIndex && <XCircle size={13} style={{ verticalAlign: "-2px", marginRight: "0.2rem" }} />}
                    {options[idx]}. {opt}
                  </span>
                ))}
              </div>
              {q.explanation && <p className="hint" style={{ marginTop: "0.5rem" }}>{q.explanation}</p>}
            </Card>
          ))}
        </div>

        <Button style={{ marginTop: "1.5rem" }} onClick={() => navigate("/assessments")}>Back to assessments</Button>
      </div>
    );
  }

  if (!attempt) return <p className="error">This assessment attempt could not be found.</p>;

  const answeredCount = Object.keys(selections).length;

  return (
    <div>
      <div className="page-header">
        <h1>{attempt.category}</h1>
        <p>{answeredCount} of {attempt.questions.length} answered</p>
      </div>

      <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <div className="progress-bar">
          <div className="progress-bar-fill" style={{ width: `${(answeredCount / attempt.questions.length) * 100}%` }} />
        </div>
        <p className="hint" style={{ marginTop: "0.5rem" }}>Time remaining: <strong>{formatRemaining(remainingMs)}</strong></p>
      </Card>

      <div style={{ display: "flex", flexDirection: "column", gap: "1rem" }}>
        {attempt.questions.map((q, i) => (
          <Card key={q.answerId} className="ui-card-padded">
            <p style={{ fontWeight: 600 }}>{i + 1}. {q.questionText}</p>
            <div style={{ display: "flex", flexDirection: "column", gap: "0.4rem", marginTop: "0.5rem" }}>
              {[q.optionA, q.optionB, q.optionC, q.optionD].map((opt, idx) => (
                <label key={idx} className="filter-option">
                  <input
                    type="radio"
                    name={`q-${q.answerId}`}
                    checked={selections[q.answerId] === idx}
                    onChange={() => handleSelect(q.answerId, idx)}
                  />
                  {opt}
                </label>
              ))}
            </div>
          </Card>
        ))}
      </div>

      <Button style={{ marginTop: "1.5rem" }} onClick={handleSubmit} loading={submitting} fullWidth>
        Submit assessment
      </Button>
    </div>
  );
}
