import { useEffect, useState, type FormEvent } from "react";
import { getCompanyReviews, getReviewEligibility, respondToReview, submitCompanyReview } from "../api/companyReviews";
import type { CompanyReviewsSummary, ReviewerRelationshipTypeName } from "../types";
import { getErrorMessage, getFieldErrors } from "../utils/errors";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import Card from "./ui/Card";
import Button from "./ui/Button";
import Modal from "./ui/Modal";
import FormField from "./ui/FormField";
import StarRating from "./ui/StarRating";
import EmptyState from "./ui/EmptyState";

const RELATIONSHIP_OPTIONS: { value: ReviewerRelationshipTypeName; label: string }[] = [
  { value: "Applicant", label: "Applicant" },
  { value: "Interviewed", label: "Interviewed" },
  { value: "ReceivedOffer", label: "Received an offer" },
  { value: "Hired", label: "Hired" },
];

function relationshipLabel(value: string): string {
  return RELATIONSHIP_OPTIONS.find((o) => o.value === value)?.label ?? value;
}

export default function CompanyReviewsSection({ companyId }: { companyId: number }) {
  const { isAuthenticated, user } = useAuth();
  const toast = useToast();
  const [summary, setSummary] = useState<CompanyReviewsSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<string>("");
  const [formOpen, setFormOpen] = useState(false);
  const [canReview, setCanReview] = useState(false);
  const [alreadyReviewed, setAlreadyReviewed] = useState(false);

  const [overallRating, setOverallRating] = useState(0);
  const [workCultureRating, setWorkCultureRating] = useState(0);
  const [interviewExperienceRating, setInterviewExperienceRating] = useState(0);
  const [workLifeBalanceRating, setWorkLifeBalanceRating] = useState(0);
  const [careerGrowthRating, setCareerGrowthRating] = useState(0);
  const [title, setTitle] = useState("");
  const [pros, setPros] = useState("");
  const [cons, setCons] = useState("");
  const [adviceToManagement, setAdviceToManagement] = useState("");
  const [relationshipType, setRelationshipType] = useState<ReviewerRelationshipTypeName>("Applicant");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [submitting, setSubmitting] = useState(false);
  const [respondingToId, setRespondingToId] = useState<number | null>(null);
  const [responseText, setResponseText] = useState("");
  const [respondSubmitting, setRespondSubmitting] = useState(false);

  function loadSummary() {
    setLoading(true);
    getCompanyReviews(companyId, filter || undefined)
      .then(setSummary)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load reviews")))
      .finally(() => setLoading(false));
  }

  useEffect(loadSummary, [companyId, filter]);

  useEffect(() => {
    if (!isAuthenticated || user?.role !== "Candidate") return;
    getReviewEligibility(companyId)
      .then((e) => {
        setCanReview(e.eligible);
        setAlreadyReviewed(e.alreadyReviewed);
      })
      .catch(() => {});
  }, [companyId, isAuthenticated, user]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setFieldErrors({});
    setSubmitting(true);
    try {
      await submitCompanyReview(companyId, {
        overallRating, workCultureRating, interviewExperienceRating, workLifeBalanceRating, careerGrowthRating,
        title, pros, cons, adviceToManagement: adviceToManagement || undefined, relationshipType,
      });
      toast.success("Review submitted — it'll appear publicly once approved by our moderation team.");
      setFormOpen(false);
      setCanReview(false);
      setAlreadyReviewed(true);
    } catch (err) {
      const errors = getFieldErrors(err);
      if (errors) setFieldErrors(errors);
      else toast.error(getErrorMessage(err, "Failed to submit review"));
    } finally {
      setSubmitting(false);
    }
  }

  async function handleSubmitResponse(reviewId: number) {
    if (!responseText.trim()) return;
    setRespondSubmitting(true);
    try {
      await respondToReview(reviewId, responseText.trim());
      toast.success("Response posted.");
      setRespondingToId(null);
      setResponseText("");
      loadSummary();
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to post response"));
    } finally {
      setRespondSubmitting(false);
    }
  }

  return (
    <Card style={{ marginTop: "1.5rem" }}>
      <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start", flexWrap: "wrap", gap: "0.75rem" }}>
        <div>
          <h3>Reviews</h3>
          {summary && summary.reviewCount > 0 ? (
            <p className="hint" style={{ marginTop: "0.25rem" }}>
              <StarRating value={Math.round(summary.averageRating ?? 0)} /> {summary.averageRating?.toFixed(1)} average · {summary.reviewCount} review{summary.reviewCount === 1 ? "" : "s"}
            </p>
          ) : (
            <p className="hint" style={{ marginTop: "0.25rem" }}>No reviews yet.</p>
          )}
        </div>
        {canReview && !alreadyReviewed && (
          <Button variant="secondary" onClick={() => setFormOpen(true)}>Write a review</Button>
        )}
        {alreadyReviewed && <p className="hint">You've already reviewed this company.</p>}
      </div>

      {summary && summary.reviewCount > 0 && (
        <div style={{ margin: "1rem 0" }}>
          {summary.ratingBreakdown.map((b) => (
            <div key={b.stars} style={{ display: "flex", alignItems: "center", gap: "0.5rem", fontSize: "var(--font-sm)" }}>
              <span style={{ width: "3.5rem" }}>{b.stars} star{b.stars === 1 ? "" : "s"}</span>
              <div style={{ flex: 1, background: "var(--surface-muted, var(--border))", borderRadius: "4px", height: "6px", overflow: "hidden" }}>
                <div style={{ width: `${summary.reviewCount > 0 ? (b.count / summary.reviewCount) * 100 : 0}%`, background: "var(--color-accent)", height: "100%" }} />
              </div>
              <span className="hint">{b.count}</span>
            </div>
          ))}
        </div>
      )}

      <FormField label="Filter by relationship" htmlFor="review-filter">
        <select id="review-filter" value={filter} onChange={(e) => setFilter(e.target.value)}>
          <option value="">All reviewers</option>
          {RELATIONSHIP_OPTIONS.map((o) => (
            <option key={o.value} value={o.value}>{o.label}</option>
          ))}
        </select>
      </FormField>

      {loading ? (
        <p>Loading...</p>
      ) : !summary || summary.reviews.length === 0 ? (
        <EmptyState title="No reviews to show" description="Be the first to share your experience with this company." />
      ) : (
        <ul className="job-list-compact">
          {summary.reviews.map((r) => (
            <li key={r.id} className="job-card job-card-compact">
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <h4>{r.title}</h4>
                <StarRating value={r.overallRating} size={14} />
              </div>
              <p className="hint">{relationshipLabel(r.relationshipType)} · {new Date(r.createdAt).toLocaleDateString()}</p>
              <p><strong>Pros:</strong> {r.pros}</p>
              <p><strong>Cons:</strong> {r.cons}</p>
              {r.adviceToManagement && <p><strong>Advice to management:</strong> {r.adviceToManagement}</p>}
              {r.recruiterResponse ? (
                <div style={{ marginTop: "0.5rem", padding: "0.5rem 0.75rem", background: "var(--surface-muted, var(--border))", borderRadius: "var(--radius-sm)" }}>
                  <p className="hint"><strong>Company response:</strong> {r.recruiterResponse}</p>
                </div>
              ) : (
                isAuthenticated && user?.role === "Recruiter" && (
                  respondingToId === r.id ? (
                    <div style={{ marginTop: "0.5rem" }}>
                      <textarea
                        rows={2}
                        value={responseText}
                        onChange={(e) => setResponseText(e.target.value)}
                        placeholder="Write a public response..."
                        maxLength={2000}
                      />
                      <div style={{ display: "flex", gap: "0.5rem", marginTop: "0.4rem" }}>
                        <Button size="sm" onClick={() => handleSubmitResponse(r.id)} loading={respondSubmitting}>Post response</Button>
                        <Button size="sm" variant="secondary" onClick={() => { setRespondingToId(null); setResponseText(""); }}>Cancel</Button>
                      </div>
                    </div>
                  ) : (
                    <button type="button" className="link-button" style={{ marginTop: "0.4rem" }} onClick={() => setRespondingToId(r.id)}>
                      Respond as company
                    </button>
                  )
                )
              )}
            </li>
          ))}
        </ul>
      )}

      <Modal
        open={formOpen}
        onClose={() => setFormOpen(false)}
        title="Write a review"
        footer={
          <>
            <Button variant="secondary" onClick={() => setFormOpen(false)}>Cancel</Button>
            <Button onClick={handleSubmit} loading={submitting}>Submit review</Button>
          </>
        }
      >
        <form onSubmit={handleSubmit} noValidate>
          <FormField label="Your relationship with this company" htmlFor="review-relationship" required>
            <select id="review-relationship" value={relationshipType} onChange={(e) => setRelationshipType(e.target.value as ReviewerRelationshipTypeName)}>
              {RELATIONSHIP_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </FormField>
          {fieldErrors.relationshipType && <p className="error">{fieldErrors.relationshipType}</p>}

          <FormField label="Overall rating" htmlFor="review-overall" required error={fieldErrors.overallRating}>
            <StarRating value={overallRating} onChange={setOverallRating} />
          </FormField>
          <FormField label="Work culture" htmlFor="review-culture" required error={fieldErrors.workCultureRating}>
            <StarRating value={workCultureRating} onChange={setWorkCultureRating} />
          </FormField>
          <FormField label="Interview experience" htmlFor="review-interview" required error={fieldErrors.interviewExperienceRating}>
            <StarRating value={interviewExperienceRating} onChange={setInterviewExperienceRating} />
          </FormField>
          <FormField label="Work-life balance" htmlFor="review-balance" required error={fieldErrors.workLifeBalanceRating}>
            <StarRating value={workLifeBalanceRating} onChange={setWorkLifeBalanceRating} />
          </FormField>
          <FormField label="Career growth" htmlFor="review-growth" required error={fieldErrors.careerGrowthRating}>
            <StarRating value={careerGrowthRating} onChange={setCareerGrowthRating} />
          </FormField>

          <FormField label="Review title" htmlFor="review-title" required error={fieldErrors.title}>
            <input id="review-title" value={title} onChange={(e) => setTitle(e.target.value)} maxLength={150} />
          </FormField>
          <FormField label="Pros" htmlFor="review-pros" required error={fieldErrors.pros}>
            <textarea id="review-pros" value={pros} onChange={(e) => setPros(e.target.value)} rows={3} maxLength={2000} />
          </FormField>
          <FormField label="Cons" htmlFor="review-cons" required error={fieldErrors.cons}>
            <textarea id="review-cons" value={cons} onChange={(e) => setCons(e.target.value)} rows={3} maxLength={2000} />
          </FormField>
          <FormField label="Advice to management" htmlFor="review-advice" hint="Optional">
            <textarea id="review-advice" value={adviceToManagement} onChange={(e) => setAdviceToManagement(e.target.value)} rows={2} maxLength={2000} />
          </FormField>

          <p className="hint">Reviews are anonymous — the company will never see your identity. Your review is reviewed by our moderation team before it appears publicly.</p>
        </form>
      </Modal>
    </Card>
  );
}
