import { useEffect, useState } from "react";
import { Activity } from "lucide-react";
import { getMyActivityTimeline } from "../api/activity";
import type { ActivityTimelineEntry } from "../types";
import { getErrorMessage } from "../utils/errors";
import Card from "../components/ui/Card";
import PageHeader from "../components/ui/PageHeader";
import FormField from "../components/ui/FormField";
import EmptyState from "../components/ui/EmptyState";
import ActivityTimeline from "../components/ActivityTimeline";

export default function ActivityTimelinePage() {
  const [entries, setEntries] = useState<ActivityTimelineEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [typeFilter, setTypeFilter] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");

  const availableTypes = Array.from(new Set(entries.map((e) => e.type))).sort();

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function load(filters?: { type?: string; from?: string; to?: string }) {
    setLoading(true);
    setError(null);
    try {
      const data = await getMyActivityTimeline({
        type: filters?.type || undefined,
        from: filters?.from ? new Date(`${filters.from}T00:00:00`).toISOString() : undefined,
        to: filters?.to ? new Date(`${filters.to}T23:59:59`).toISOString() : undefined,
      });
      setEntries(data);
    } catch (err) {
      setError(getErrorMessage(err, "Failed to load your activity timeline"));
    } finally {
      setLoading(false);
    }
  }

  function handleApplyFilters() {
    void load({ type: typeFilter, from, to });
  }

  function handleClearFilters() {
    setTypeFilter("");
    setFrom("");
    setTo("");
    void load();
  }

  return (
    <div>
      <PageHeader
        title={<><Activity size={26} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Activity Timeline</>}
        subtitle="Everything that's happened on your account, in one place."
      />

      <Card className="ui-card-padded" style={{ marginBottom: "1.5rem" }}>
        <div className="form-row">
          <FormField label="Activity type" htmlFor="activity-type-filter">
            <select id="activity-type-filter" value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}>
              <option value="">All types</option>
              {availableTypes.map((t) => (
                <option key={t} value={t}>{t.replace(/([a-z])([A-Z])/g, "$1 $2")}</option>
              ))}
            </select>
          </FormField>
          <FormField label="From" htmlFor="activity-from">
            <input id="activity-from" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </FormField>
          <FormField label="To" htmlFor="activity-to">
            <input id="activity-to" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </FormField>
        </div>
        <div className="form-actions">
          <button type="button" className="btn btn-primary" onClick={handleApplyFilters}>Apply filters</button>
          <button type="button" className="btn btn-secondary" onClick={handleClearFilters}>Clear</button>
        </div>
      </Card>

      {error && <p className="error" style={{ marginBottom: "1rem" }}>{error}</p>}

      <Card>
        {loading ? (
          <p>Loading...</p>
        ) : entries.length === 0 ? (
          <EmptyState
            icon={<Activity size={28} />}
            title="No activity yet"
            description="Actions you take and updates you receive will show up here."
          />
        ) : (
          <ActivityTimeline entries={entries} />
        )}
      </Card>
    </div>
  );
}
