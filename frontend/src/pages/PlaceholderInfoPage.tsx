import { Link } from "react-router-dom";
import { FileClock } from "lucide-react";
import Card from "../components/ui/Card";
import EmptyState from "../components/ui/EmptyState";

interface PlaceholderInfoPageProps {
  title: string;
}

export default function PlaceholderInfoPage({ title }: PlaceholderInfoPageProps) {
  return (
    <div style={{ maxWidth: 640, margin: "0 auto" }}>
      <div className="page-header">
        <h1>{title}</h1>
      </div>
      <Card className="ui-card-padded">
        <EmptyState
          icon={<FileClock size={28} />}
          title="Coming soon"
          description={`This is a portfolio demonstration project, so a full ${title.toLowerCase()} page hasn't been written yet — check back later, or head back to browsing jobs.`}
          action={<Link to="/jobs" className="btn btn-secondary btn-sm">Browse Jobs</Link>}
        />
      </Card>
    </div>
  );
}
