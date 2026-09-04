import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Building2, ExternalLink, Globe, Heart, MapPin } from "lucide-react";
import { getCompanyProfile } from "../api/companies";
import { followCompany, getFollowedCompanies, unfollowCompany } from "../api/follows";
import type { CompanyProfile } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import Card from "../components/ui/Card";
import Avatar from "../components/ui/Avatar";
import EmptyState from "../components/ui/EmptyState";
import JobCard from "../components/JobCard";
import Button from "../components/ui/Button";
import VerifiedBadge from "../components/VerifiedBadge";
import CompanyReviewsSection from "../components/CompanyReviewsSection";

export default function CompanyProfilePage() {
  const { id } = useParams();
  const { isAuthenticated, user } = useAuth();
  const toast = useToast();
  const [company, setCompany] = useState<CompanyProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [following, setFollowing] = useState(false);
  const [followPending, setFollowPending] = useState(false);

  useEffect(() => {
    if (!id) return;
    getCompanyProfile(Number(id))
      .then(setCompany)
      .catch((err) => setError(getErrorMessage(err, "Failed to load company profile")))
      .finally(() => setLoading(false));
  }, [id]);

  useEffect(() => {
    if (!id || !isAuthenticated || user?.role !== "Candidate") return;
    getFollowedCompanies()
      .then((followed) => setFollowing(followed.some((f) => f.companyId === Number(id))))
      .catch(() => setFollowing(false));
  }, [id, isAuthenticated, user]);

  async function handleToggleFollow() {
    if (!id || followPending) return;
    setFollowPending(true);
    try {
      if (following) {
        await unfollowCompany(Number(id));
        setFollowing(false);
      } else {
        await followCompany(Number(id));
        setFollowing(true);
      }
    } catch (err) {
      toast.error(getErrorMessage(err, "Couldn't update followed companies"));
    } finally {
      setFollowPending(false);
    }
  }

  if (loading) return <p>Loading...</p>;
  if (error || !company) return <p className="error">{error ?? "Company not found."}</p>;

  const location = company.city ? `${company.city}${company.state ? `, ${company.state}` : ""}, India` : null;
  const benefits = (company.benefits ?? "").split(",").map((b) => b.trim()).filter(Boolean);

  return (
    <div className="job-detail-layout">
      <div className="job-detail-main">
        <div className="job-detail-header" style={{ display: "flex", alignItems: "center", gap: "1rem" }}>
          {company.logoUrl ? (
            <img src={company.logoUrl} alt="" style={{ width: 56, height: 56, borderRadius: "12px", objectFit: "cover" }} />
          ) : (
            <Avatar name={company.name} size={56} />
          )}
          <div>
            <h1>
              {company.name}
              {company.isVerified && <VerifiedBadge className="job-card-verified-badge" />}
            </h1>
            {company.industry && <p className="job-detail-company">{company.industry}</p>}
            {company.reviewCount > 0 && (
              <p className="hint" style={{ marginTop: "0.25rem" }}>
                ★ {company.averageRating?.toFixed(1)} ({company.reviewCount} review{company.reviewCount === 1 ? "" : "s"})
              </p>
            )}
          </div>
        </div>

        <div className="job-detail-facts">
          {location && <span className="job-detail-fact"><MapPin size={16} /> {location}</span>}
          {company.size && <span className="job-detail-fact"><Building2 size={16} /> {company.size}</span>}
          {company.website && (
            <span className="job-detail-fact">
              <Globe size={16} /> <a href={company.website} target="_blank" rel="noreferrer">{company.website}</a>
            </span>
          )}
          {company.linkedInUrl && (
            <span className="job-detail-fact">
              <ExternalLink size={16} /> <a href={company.linkedInUrl} target="_blank" rel="noreferrer">LinkedIn</a>
            </span>
          )}
          {company.twitterUrl && (
            <span className="job-detail-fact">
              <ExternalLink size={16} /> <a href={company.twitterUrl} target="_blank" rel="noreferrer">Twitter/X</a>
            </span>
          )}
        </div>

        {company.description && (
          <>
            <h3 style={{ marginBottom: "0.75rem" }}>About</h3>
            <p className="job-detail-body">{company.description}</p>
          </>
        )}

        {company.cultureHighlights && (
          <>
            <h3 style={{ margin: "1.5rem 0 0.75rem" }}>Culture</h3>
            <p className="job-detail-body">{company.cultureHighlights}</p>
          </>
        )}

        {benefits.length > 0 && (
          <>
            <h3 style={{ margin: "1.5rem 0 0.75rem" }}>Benefits</h3>
            <div className="chip-list">
              {benefits.map((b) => <span key={b} className="chip">{b}</span>)}
            </div>
          </>
        )}

        <CompanyReviewsSection companyId={company.id} />
      </div>

      <div className="job-detail-sidebar">
        {isAuthenticated && user?.role === "Candidate" && (
          <Card>
            <Button
              variant="secondary"
              icon={<Heart size={16} fill={following ? "currentColor" : "none"} />}
              onClick={handleToggleFollow}
              loading={followPending}
              fullWidth
            >
              {following ? "Following company" : "Follow company"}
            </Button>
          </Card>
        )}

        <Card>
          <h3 style={{ marginBottom: "1rem" }}>Open roles ({company.openJobs.length})</h3>
          {company.openJobs.length === 0 ? (
            <EmptyState title="No open roles" description="Check back later for new openings." />
          ) : (
            <div className="similar-jobs-list">
              {company.openJobs.map((job) => <JobCard key={job.id} job={job} compact />)}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}
