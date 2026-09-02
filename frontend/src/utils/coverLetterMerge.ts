import type { CandidateProfile, CoverLetterTemplate } from "../types";

/** Merges a cover-letter template with safe, already-known profile fields and the job being
 * applied to — entirely client-side string concatenation, no AI involved. The result is
 * always editable before the candidate submits it. */
export function buildCoverLetterDraft(
  template: CoverLetterTemplate | null,
  profile: CandidateProfile,
  jobTitle: string,
  companyName: string,
): string {
  const lines: string[] = [];

  lines.push(`Dear ${companyName} Hiring Team,`);
  lines.push("");

  if (template?.introduction) {
    lines.push(template.introduction);
  } else {
    const skillsPart = profile.skillsCsv ? ` with experience in ${profile.skillsCsv}` : "";
    lines.push(`My name is ${profile.fullName}${profile.headline ? `, ${profile.headline}` : ""}${skillsPart}. I'm excited to apply for the ${jobTitle} role at ${companyName}.`);
  }

  if (template?.skillsHighlights) {
    lines.push("");
    lines.push(template.skillsHighlights);
  }

  if (template?.projectAchievements) {
    lines.push("");
    lines.push(template.projectAchievements);
  }

  lines.push("");
  lines.push(template?.closingMessage || "Thank you for considering my application — I look forward to hearing from you.");
  lines.push("");
  lines.push(profile.fullName);

  return lines.join("\n");
}
