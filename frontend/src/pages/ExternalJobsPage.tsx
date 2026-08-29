import { useEffect, useState, type FormEvent } from "react";
import { ExternalLink, Search } from "lucide-react";
import { getExternalJobsAvailability, searchExternalJobs } from "../api/externalJobs";
import type { ExternalJobListing } from "../types";
import { getErrorMessage } from "../utils/errors";
import Button from "../components/ui/Button";
import EmptyState from "../components/ui/EmptyState";

export default function ExternalJobsPage() {
  const [available, setAvailable] = useState<boolean | null>(null);
  const [keywords, setKeywords] = useState("");
  const [location, setLocation] = useState("");
  const [results, setResults] = useState<ExternalJobListing[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [searched, setSearched] = useState(false);

  useEffect(() => {
    getExternalJobsAvailability().then(setAvailable);
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    setSearched(true);
    try {
      const result = await searchExternalJobs(keywords, location);
      setResults(result.items);
    } catch (err) {
      setError(getErrorMessage(err, "External job search is temporarily unavailable."));
    } finally {
      setLoading(false);
    }
  }

  if (available === null) return <p>Loading...</p>;

  if (!available) {
    return (
      <div className="page-header">
        <h1>External Job Search</h1>
        <p>External job search is not configured on this server.</p>
      </div>
    );
  }

  return (
    <div>
      <div className="page-header">
        <h1>External Job Search</h1>
        <p className="hint">Results are sourced from Adzuna. You'll apply on the original site — not in this app.</p>
      </div>

      <form className="search-bar" onSubmit={handleSubmit}>
        <input placeholder="Keywords" value={keywords} onChange={(e) => setKeywords(e.target.value)} />
        <input placeholder="Location" value={location} onChange={(e) => setLocation(e.target.value)} />
        <Button type="submit" loading={loading} icon={<Search size={16} />}>Search</Button>
      </form>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      {searched && !loading && !error && results.length === 0 && (
        <EmptyState title="No external listings found" description="Try different keywords or a broader location." />
      )}

      <ul className="job-list">
        {results.map((job, idx) => (
          <li key={`${job.url}-${idx}`} className="job-card">
            <span className="external-badge">External listing — sourced from Adzuna</span>
            <h3>{job.title}</h3>
            <p className="job-card-company">{job.companyName} {job.location ? `· ${job.location}` : ""}</p>
            {job.salaryRange && <p className="hint">Salary: {job.salaryRange}</p>}
            <a href={job.url} target="_blank" rel="noreferrer" className="btn btn-secondary btn-sm" style={{ marginTop: "0.75rem" }}>
              View on Adzuna <ExternalLink size={14} />
            </a>
          </li>
        ))}
      </ul>
    </div>
  );
}
