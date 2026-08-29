import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Building2, ExternalLink, Globe, MapPin } from "lucide-react";
import { getCompanyProfile } from "../api/companies";
import type { CompanyProfile } from "../types";
import { getErrorMessage } from "../utils/errors";
import Card from "../components/ui/Card";
import Avatar from "../components/ui/Avatar";
import EmptyState from "../components/ui/EmptyState";
import JobCard from "../components/JobCard";

export default function CompanyProfilePage() {
  const { id } = useParams();
  const [company, setCompany] = useState<CompanyProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    getCompanyProfile(Number(id))
      .then(setCompany)
      .catch((err) => setError(getErrorMessage(err, "Failed to load company profile")))
      .finally(() => setLoading(false));
  }, [id]);

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
            <h1>{company.name}</h1>
            {company.industry && <p className="job-detail-company">{company.industry}</p>}
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
      </div>

      <div className="job-detail-sidebar">
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
