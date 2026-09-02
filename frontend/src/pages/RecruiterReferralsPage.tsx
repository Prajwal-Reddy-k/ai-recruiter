import { useEffect, useState } from "react";
import { Share2 } from "lucide-react";
import { getCompanyReferrals } from "../api/referrals";
import { getErrorMessage } from "../utils/errors";
import { useToast } from "../context/ToastContext";
import type { Referral } from "../types";
import PageHeader from "../components/ui/PageHeader";
import EmptyState from "../components/ui/EmptyState";
import { StatusBadge } from "../components/ui/Badge";

export default function RecruiterReferralsPage() {
  const toast = useToast();
  const [referrals, setReferrals] = useState<Referral[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    getCompanyReferrals()
      .then(setReferrals)
      .catch((err) => toast.error(getErrorMessage(err, "Failed to load referrals")))
      .finally(() => setLoading(false));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  if (loading) return <p>Loading...</p>;

  return (
    <div>
      <PageHeader
        title={<><Share2 size={24} style={{ verticalAlign: "-4px", marginRight: "0.5rem" }} />Referrals</>}
        subtitle="Employee referrals for your company's job postings."
      />

      {referrals.length === 0 ? (
        <EmptyState icon={<Share2 size={32} />} title="No referrals yet" description="Referrals will appear here once your employees start referring candidates to your open jobs." />
      ) : (
        <div className="table-scroll">
          <table className="dashboard-table">
            <thead>
              <tr>
                <th>Referred Person</th>
                <th>Job</th>
                <th>Status</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {referrals.map((r) => (
                <tr key={r.id}>
                  <td>{r.referredName} <span className="hint">({r.referredEmail})</span></td>
                  <td>{r.jobTitle}</td>
                  <td><StatusBadge status={r.status} /></td>
                  <td>{new Date(r.createdAt).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
