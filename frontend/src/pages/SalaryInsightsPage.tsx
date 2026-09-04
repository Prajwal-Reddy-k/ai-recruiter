import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { IndianRupee } from "lucide-react";
import { getSalaryInsights } from "../api/salaryInsights";
import type { SalaryInsight } from "../types";
import { getErrorMessage } from "../utils/errors";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import FormField from "../components/ui/FormField";
import EmptyState from "../components/ui/EmptyState";
import BarList from "../components/ui/BarList";

const EXPERIENCE_BANDS = ["Junior", "Mid", "Senior", "Lead"];

function formatInr(value: number): string {
  return `₹${Math.round(value).toLocaleString("en-IN")}`;
}

export default function SalaryInsightsPage() {
  const [insights, setInsights] = useState<SalaryInsight[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [role, setRole] = useState("");
  const [city, setCity] = useState("");
  const [experienceBand, setExperienceBand] = useState("");

  function load() {
    setLoading(true);
    setError(null);
    getSalaryInsights({
      role: role || undefined,
      city: city || undefined,
      experienceBand: experienceBand || undefined,
    })
      .then(setInsights)
      .catch((err) => setError(getErrorMessage(err, "Failed to load salary insights")))
      .finally(() => setLoading(false));
  }

  useEffect(load, []);

  const withData = insights.filter((i) => i.hasEnoughData);
  const barItems = withData.map((i) => ({
    name: `${i.roleTitle} (${i.experienceBand}${i.isRemote ? ", Remote" : i.city ? `, ${i.city}` : ""})`,
    count: Math.round(i.median ?? 0),
  }));

  return (
    <div>
      <PageHeader
        title={<><IndianRupee size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Salary Insights</>}
        subtitle="Estimated salary ranges based on published job postings and accepted offers on the platform. Not a substitute for professional compensation advice."
      />

      <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <div className="form-row">
          <FormField label="Role" htmlFor="salary-role">
            <input id="salary-role" value={role} onChange={(e) => setRole(e.target.value)} placeholder="e.g. Backend Engineer" />
          </FormField>
          <FormField label="City" htmlFor="salary-city">
            <input id="salary-city" value={city} onChange={(e) => setCity(e.target.value)} placeholder="e.g. Bengaluru" />
          </FormField>
          <FormField label="Experience level" htmlFor="salary-experience">
            <select id="salary-experience" value={experienceBand} onChange={(e) => setExperienceBand(e.target.value)}>
              <option value="">Any</option>
              {EXPERIENCE_BANDS.map((b) => <option key={b} value={b}>{b}</option>)}
            </select>
          </FormField>
        </div>
        <div className="form-actions">
          <button type="button" className="btn btn-primary" onClick={load}>Search</button>
        </div>
      </Card>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      {loading ? (
        <p>Loading...</p>
      ) : insights.length === 0 ? (
        <EmptyState
          icon={<IndianRupee size={28} />}
          title="No matching data"
          description="Try a broader search — fewer filters, or a nearby role/city."
        />
      ) : (
        <>
          {barItems.length > 0 && (
            <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
              <h3 style={{ marginBottom: "0.75rem" }}>Median annual salary by role</h3>
              <BarList items={barItems} />
            </Card>
          )}

          <Card>
            <div className="table-scroll">
              <table className="dashboard-table">
                <thead>
                  <tr>
                    <th>Role</th>
                    <th>Experience</th>
                    <th>Location</th>
                    <th>Min</th>
                    <th>Median</th>
                    <th>Max</th>
                    <th>Sample size</th>
                  </tr>
                </thead>
                <tbody>
                  {insights.map((i, idx) => (
                    <tr key={idx}>
                      <td>
                        <Link to={`/jobs?search=${encodeURIComponent(i.roleTitle)}`}>{i.roleTitle}</Link>
                      </td>
                      <td>{i.experienceBand}</td>
                      <td>{i.isRemote ? "Remote" : i.city ? `${i.city}${i.state ? `, ${i.state}` : ""}` : "—"}</td>
                      {i.hasEnoughData ? (
                        <>
                          <td>{formatInr(i.min!)}</td>
                          <td>{formatInr(i.median!)}</td>
                          <td>{formatInr(i.max!)}</td>
                        </>
                      ) : (
                        <td colSpan={3} className="hint">Not enough data yet</td>
                      )}
                      <td>{i.sampleCount}{i.hasEnoughData && " (estimated)"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>
        </>
      )}
    </div>
  );
}
