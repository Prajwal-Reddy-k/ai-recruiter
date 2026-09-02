import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Award, Clock } from "lucide-react";
import { getAssessmentCategories, startAssessmentAttempt } from "../api/assessments";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { AssessmentCategorySummary } from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import Button from "../components/ui/Button";

const CATEGORY_LABELS: Record<string, string> = {
  Java: "Java",
  CSharpDotNet: "C# / .NET",
  React: "React",
  JavaScript: "JavaScript",
  Sql: "SQL",
  Python: "Python",
  Communication: "Communication",
  Aptitude: "Aptitude",
};

export default function SkillAssessmentsPage() {
  const navigate = useNavigate();
  const toast = useToast();
  const [categories, setCategories] = useState<AssessmentCategorySummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [startingCategory, setStartingCategory] = useState<string | null>(null);

  useEffect(() => {
    getAssessmentCategories()
      .then(setCategories)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load assessment categories")))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function handleStart(category: string) {
    setStartingCategory(category);
    try {
      const attempt = await startAssessmentAttempt(category);
      navigate(`/assessments/attempt/${attempt.attemptId}`);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to start assessment"));
      setStartingCategory(null);
    }
  }

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <PageHeader
        title={<><Award size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Skill Assessments</>}
        subtitle="15 multiple-choice questions, 20 minutes, locally seeded — not an official certification and not externally proctored."
      />

      <div className="dashboard-grid">
        {categories.map((cat) => (
          <Card key={cat.category} className="ui-card-padded">
            <h3>{CATEGORY_LABELS[cat.category] ?? cat.category}</h3>
            <p className="hint">{cat.questionBankSize} questions in the bank</p>
            {cat.bestPercentageScore !== null && (
              <p style={{ marginTop: "0.5rem" }}>Best score: <strong>{cat.bestPercentageScore}%</strong></p>
            )}
            {cat.cooldownEndsAtUtc && (
              <p className="hint" style={{ marginTop: "0.5rem" }}>
                <Clock size={13} style={{ verticalAlign: "-2px" }} /> Retake available {new Date(cat.cooldownEndsAtUtc).toLocaleString()}
              </p>
            )}
            {cat.hasActiveAttempt && !cat.canAttemptNow && (
              <p className="hint" style={{ marginTop: "0.5rem" }}>You have an assessment already in progress.</p>
            )}
            <Button
              size="sm"
              style={{ marginTop: "0.75rem" }}
              disabled={!cat.canAttemptNow}
              loading={startingCategory === cat.category}
              onClick={() => handleStart(cat.category)}
            >
              {cat.bestPercentageScore !== null ? "Retake" : "Start"}
            </Button>
          </Card>
        ))}
      </div>
    </div>
  );
}
