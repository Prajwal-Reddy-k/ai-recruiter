import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { Share2 } from "lucide-react";
import { getMyReferrals } from "../api/referrals";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { Referral } from "../types";
import PageHeader from "../components/ui/PageHeader";
import Card from "../components/ui/Card";
import EmptyState from "../components/ui/EmptyState";
import { StatusBadge } from "../components/ui/Badge";

export default function ReferralsPage() {
  const toast = useToast();
  const [referrals, setReferrals] = useState<Referral[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getMyReferrals()
      .then(setReferrals)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load your referrals")))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <PageHeader
        title={<><Share2 size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />My Referrals</>}
        subtitle="Referrals you've created — click 'Refer a friend' on any open job to create one."
      />

      {referrals.length === 0 ? (
        <EmptyState
          icon={<Share2 size={32} />}
          title="No referrals yet"
          description="Open any job listing and click 'Refer a friend' to create a shareable referral link."
          action={<Link to="/jobs" className="btn btn-primary">Browse open jobs</Link>}
        />
      ) : (
        <ul className="job-list-compact">
          {referrals.map((r) => (
            <li key={r.id} className="job-card job-card-compact">
              <Card className="ui-card-padded">
                <h4>{r.referredName}</h4>
                <p className="hint">{r.referredEmail}</p>
                <p style={{ marginTop: "0.5rem" }}>{r.jobTitle} at {r.companyName}</p>
                <p style={{ marginTop: "0.5rem" }}><StatusBadge status={r.status} /></p>
                <p className="hint" style={{ marginTop: "0.5rem" }}>Created {new Date(r.createdAt).toLocaleDateString()}</p>
              </Card>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
