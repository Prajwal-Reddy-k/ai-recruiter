import { useEffect, useState } from "react";
import { BarChart3, Briefcase, CalendarClock, ClipboardList, Trophy, Users } from "lucide-react";
import { getRecruiterAnalytics } from "../api/dashboard";
import type { RecruiterAnalytics } from "../types";
import { getErrorMessage } from "../utils/errors";
import Card from "../components/ui/Card";
import StatCard from "../components/ui/StatCard";
import SectionHeader from "../components/ui/SectionHeader";
import EmptyState from "../components/ui/EmptyState";
import BarList from "../components/ui/BarList";

export default function RecruiterAnalyticsPage() {
  const [data, setData] = useState<RecruiterAnalytics | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getRecruiterAnalytics()
      .then(setData)
      .catch((err) => setError(getErrorMessage(err, "Failed to load analytics")))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Loading...</p>;
  if (error || !data) return <p className="error">{error ?? "Analytics unavailable."}</p>;

  if (data.activeJobs === 0 && data.totalApplications === 0) {
    return (
      <div className="page-header">
        <h1><BarChart3 size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Analytics</h1>
        <EmptyState
          icon={<BarChart3 size={32} />}
          title="Nothing to analyze yet"
          description="Post a job and start collecting applications to see analytics here."
        />
      </div>
    );
  }

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1><BarChart3 size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Analytics</h1>
        <p>Real data across all of your job postings.</p>
      </div>

      <div className="dashboard-grid">
        <StatCard icon={<Briefcase size={20} />} value={data.activeJobs} label="Active Jobs" tone="primary" />
        <StatCard icon={<Users size={20} />} value={data.totalApplications} label="Total Applications" tone="primary" />
        <StatCard icon={<ClipboardList size={20} />} value={data.shortlistedCandidates} label="Shortlisted" tone="accent" />
        <StatCard icon={<CalendarClock size={20} />} value={data.interviewsScheduled} label="Interviews Scheduled" tone="accent" />
        <StatCard icon={<Trophy size={20} />} value={data.offersMade} label="Offers Made" tone="neutral" />
      </div>

      <Card>
        <SectionHeader title="Hiring funnel" as="h3" />
        {data.hiringFunnel.length === 0 ? <p className="hint">No data yet.</p> : <BarList items={data.hiringFunnel} />}
      </Card>

      <Card>
        <SectionHeader title="Applications per job" as="h3" />
        {data.applicationsPerJob.length === 0 ? <p className="hint">No data yet.</p> : <BarList items={data.applicationsPerJob} />}
      </Card>

      <Card>
        <SectionHeader title="Applications by city" as="h3" />
        {data.applicationsByCity.length === 0 ? <p className="hint">No data yet.</p> : <BarList items={data.applicationsByCity} />}
      </Card>

      <Card>
        <SectionHeader title="Top candidate skills" as="h3" />
        {data.topCandidateSkills.length === 0 ? (
          <p className="hint">No data yet.</p>
        ) : (
          <div className="chip-list">
            {data.topCandidateSkills.map((s) => (
              <span key={s.name} className="chip">{s.name} ({s.count})</span>
            ))}
          </div>
        )}
      </Card>

      <Card>
        <SectionHeader title="Views vs. applications" as="h3" />
        {data.viewsVsApplications.length === 0 ? (
          <p className="hint">No data yet.</p>
        ) : (
          <div className="table-scroll">
            <table className="dashboard-table">
              <thead>
                <tr>
                  <th>Job</th>
                  <th>Views</th>
                  <th>Applications</th>
                  <th>Conversion</th>
                </tr>
              </thead>
              <tbody>
                {data.viewsVsApplications.map((j) => (
                  <tr key={j.jobId}>
                    <td>{j.title}</td>
                    <td>{j.viewCount}</td>
                    <td>{j.applicationCount}</td>
                    <td>{j.viewCount > 0 ? `${Math.round((j.applicationCount / j.viewCount) * 100)}%` : "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
