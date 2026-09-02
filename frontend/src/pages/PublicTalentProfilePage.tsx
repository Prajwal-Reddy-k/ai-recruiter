import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { Award, ExternalLink } from "lucide-react";
import { getPublicProfileBySlug } from "../api/publicProfile";
import { getErrorMessage } from "../utils/errors";
import { resolveAvatarUrl } from "../utils/format";
import type { PublicCandidateProfile } from "../types";
import Card from "../components/ui/Card";
import Avatar from "../components/ui/Avatar";
import EmptyState from "../components/ui/EmptyState";

export default function PublicTalentProfilePage() {
  const { slug } = useParams();
  const [profile, setProfile] = useState<PublicCandidateProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!slug) return;
    getPublicProfileBySlug(slug)
      .then(setProfile)
      .catch((err) => setError(getErrorMessage(err, "This profile isn't available.")))
      .finally(() => setLoading(false));
  }, [slug]);

  if (loading) return <p>Loading...</p>;

  if (error || !profile) {
    return (
      <EmptyState
        title="Profile not found"
        description={error ?? "This link may have expired, or the candidate has turned off public sharing."}
      />
    );
  }

  const skills = (profile.skillsCsv ?? "").split(",").map((s) => s.trim()).filter(Boolean);

  return (
    <div style={{ maxWidth: "720px", margin: "0 auto" }}>
      <Card className="ui-card-padded">
        <div style={{ display: "flex", alignItems: "center", gap: "1.25rem" }}>
          <Avatar name={profile.fullName} size={72} src={resolveAvatarUrl(profile.avatarUrl)} />
          <div>
            <h1>{profile.fullName}</h1>
            {profile.headline && <p className="hint">{profile.headline}</p>}
          </div>
        </div>

        <div style={{ display: "flex", gap: "1rem", marginTop: "0.75rem" }}>
          {profile.linkedInUrl && <a href={profile.linkedInUrl} target="_blank" rel="noreferrer"><ExternalLink size={14} style={{ verticalAlign: "-2px" }} /> LinkedIn</a>}
          {profile.githubUrl && <a href={profile.githubUrl} target="_blank" rel="noreferrer"><ExternalLink size={14} style={{ verticalAlign: "-2px" }} /> GitHub</a>}
          {profile.portfolioUrl && <a href={profile.portfolioUrl} target="_blank" rel="noreferrer"><ExternalLink size={14} style={{ verticalAlign: "-2px" }} /> Portfolio</a>}
        </div>

        {profile.summary && <p style={{ marginTop: "1.25rem" }}>{profile.summary}</p>}

        {skills.length > 0 && (
          <>
            <h3 style={{ marginTop: "1.5rem", marginBottom: "0.5rem" }}>Skills</h3>
            <div className="chip-list">
              {skills.map((s) => <span key={s} className="chip">{s}</span>)}
            </div>
          </>
        )}

        {(profile.experienceSummary || profile.totalExperienceYears !== null) && (
          <>
            <h3 style={{ marginTop: "1.5rem", marginBottom: "0.5rem" }}>Experience</h3>
            {profile.totalExperienceYears !== null && <p className="hint">{profile.totalExperienceYears} years of experience</p>}
            {profile.experienceSummary && <p style={{ marginTop: "0.5rem" }}>{profile.experienceSummary}</p>}
          </>
        )}

        {profile.education && (
          <>
            <h3 style={{ marginTop: "1.5rem", marginBottom: "0.5rem" }}>Education</h3>
            <p>{profile.education}</p>
          </>
        )}

        {profile.projects.length > 0 && (
          <>
            <h3 style={{ marginTop: "1.5rem", marginBottom: "0.5rem" }}>Projects</h3>
            <div style={{ display: "flex", flexDirection: "column", gap: "0.75rem" }}>
              {profile.projects.map((p) => (
                <div key={p.id}>
                  <strong>{p.title}</strong>
                  {p.projectUrl && <> — <a href={p.projectUrl} target="_blank" rel="noreferrer">{p.projectUrl}</a></>}
                  {p.description && <p className="hint">{p.description}</p>}
                </div>
              ))}
            </div>
          </>
        )}

        {profile.assessmentBadges.length > 0 && (
          <>
            <h3 style={{ marginTop: "1.5rem", marginBottom: "0.5rem" }}><Award size={16} style={{ verticalAlign: "-3px", marginRight: "0.3rem" }} />Verified skill badges</h3>
            <div className="chip-list">
              {profile.assessmentBadges.map((b) => (
                <span key={b.category} className="chip chip-matched">{b.category}: {b.percentageScore}%</span>
              ))}
            </div>
          </>
        )}
      </Card>
    </div>
  );
}
