import { useEffect, useState } from "react";
import { BarChart3, Download } from "lucide-react";
import { exportApplicantsCsv, exportFunnelCsv, exportInterviewsCsv, exportJobsCsv, getRecruiterReport, type ReportDateRange } from "../api/reports";
import type { RecruiterReport } from "../types";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import { saveBlobAsFile } from "../utils/download";
import Card from "../components/ui/Card";
import StatCard from "../components/ui/StatCard";
import SectionHeader from "../components/ui/SectionHeader";
import EmptyState from "../components/ui/EmptyState";
import Button from "../components/ui/Button";
import BarList from "../components/ui/BarList";

function toIsoOrUndefined(value: string): string | undefined {
  return value ? new Date(value).toISOString() : undefined;
}

export default function RecruiterReportsPage() {
  const toast = useToast();
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [data, setData] = useState<RecruiterReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [exporting, setExporting] = useState<string | null>(null);

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function currentRange(): ReportDateRange {
    return { fromUtc: toIsoOrUndefined(fromDate), toUtc: toIsoOrUndefined(toDate) };
  }

  async function load() {
    setLoading(true);
    setError(null);
    try {
      setData(await getRecruiterReport(currentRange()));
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load report"));
    } finally {
      setLoading(false);
    }
  }

  async function handleExport(name: string, fn: (range: ReportDateRange) => Promise<Blob>, filename: string) {
    setExporting(name);
    try {
      const blob = await fn(currentRange());
      saveBlobAsFile(blob, filename);
    } catch (err) {
      toast.error(getErrorMessage(err, "Failed to export CSV"));
    } finally {
      setExporting(null);
    }
  }

  if (loading) return <p>Loading...</p>;
  if (error || !data) return <p className="error">{error ?? "Report unavailable."}</p>;

  const hasAnyData = data.jobsCreated > 0 || data.totalApplications > 0;

  return (
    <div className="dashboard-page">
      <div className="page-header">
        <h1><BarChart3 size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Reports</h1>
        <p>Company-wide hiring metrics, filterable by date range, with CSV export.</p>
      </div>

      <Card className="ui-card-padded">
        <div className="form-row">
          <label className="form-field">
            <span className="form-field-label">From</span>
            <input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} />
          </label>
          <label className="form-field">
            <span className="form-field-label">To</span>
            <input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} />
          </label>
          <Button onClick={load} style={{ alignSelf: "flex-end" }}>Apply filter</Button>
        </div>
      </Card>

      {!hasAnyData ? (
        <EmptyState
          icon={<BarChart3 size={32} />}
          title="Nothing to report yet"
          description="Post a job and start collecting applications to see reports here."
        />
      ) : (
        <>
          <div className="dashboard-grid">
            <StatCard value={data.jobsCreated} label="Jobs Created" tone="primary" />
            <StatCard value={data.jobsPublished} label="Jobs Published" tone="primary" />
            <StatCard value={data.jobsClosed} label="Jobs Closed" tone="neutral" />
            <StatCard value={data.totalApplications} label="Total Applications" tone="accent" />
            <StatCard value={data.offersMade} label="Offers Made" tone="accent" />
            <StatCard value={data.hires} label="Hires" tone="primary" />
          </div>

          <Card>
            <SectionHeader
              title="Interview funnel"
              subtitle={`${data.interviewConversionRatePercent ?? 0}% of applications reached an interview${data.averageDaysToInterview !== null ? ` · avg ${data.averageDaysToInterview} days to interview` : ""}`}
              as="h3"
            />
            <div className="dashboard-grid">
              <StatCard value={data.interviewsScheduledCount} label="Reached Interview" tone="primary" />
              <StatCard value={data.interviewsCompletedCount} label="Completed / Beyond" tone="accent" />
            </div>
          </Card>

          <Card>
            <SectionHeader title="Hiring funnel" as="h3" />
            {data.statusFunnel.length === 0 ? <p className="hint">No data yet.</p> : <BarList items={data.statusFunnel} />}
          </Card>

          <Card>
            <SectionHeader title="Applications by job" as="h3" />
            {data.applicationsByJob.length === 0 ? <p className="hint">No data yet.</p> : <BarList items={data.applicationsByJob} />}
          </Card>

          <Card>
            <SectionHeader title="Applications by state" as="h3" />
            {data.applicationsByState.length === 0 ? <p className="hint">No data yet.</p> : <BarList items={data.applicationsByState} />}
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
                {data.topCandidateSkills.map((s) => <span key={s.name} className="chip">{s.name} ({s.count})</span>)}
              </div>
            )}
          </Card>
        </>
      )}

      <Card className="ui-card-padded">
        <SectionHeader title="Export CSV" subtitle="Data authorized for your company only." as="h3" />
        <div style={{ display: "flex", gap: "0.75rem", flexWrap: "wrap", marginTop: "0.75rem" }}>
          <Button variant="secondary" icon={<Download size={16} />} loading={exporting === "jobs"} onClick={() => handleExport("jobs", exportJobsCsv, "jobs.csv")}>
            Jobs
          </Button>
          <Button variant="secondary" icon={<Download size={16} />} loading={exporting === "applicants"} onClick={() => handleExport("applicants", exportApplicantsCsv, "applicants.csv")}>
            Applicants
          </Button>
          <Button variant="secondary" icon={<Download size={16} />} loading={exporting === "interviews"} onClick={() => handleExport("interviews", exportInterviewsCsv, "interview-schedule.csv")}>
            Interview Schedule
          </Button>
          <Button variant="secondary" icon={<Download size={16} />} loading={exporting === "funnel"} onClick={() => handleExport("funnel", exportFunnelCsv, "hiring-funnel.csv")}>
            Funnel Summary
          </Button>
        </div>
      </Card>
    </div>
  );
}
