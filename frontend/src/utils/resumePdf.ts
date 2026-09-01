import { jsPDF } from "jspdf";
import type { Resume } from "../types";

function formatDate(value: string | null): string {
  if (!value) return "";
  return new Date(value).toLocaleDateString("en-IN", { month: "short", year: "numeric" });
}

/** Builds a clean, single-column resume PDF entirely client-side from the resume-builder's
 * structured data — no backend PDF dependency. */
export function generateResumePdf(resume: Resume): Blob {
  const doc = new jsPDF({ unit: "pt", format: "a4" });
  const marginX = 48;
  const pageWidth = doc.internal.pageSize.getWidth();
  const contentWidth = pageWidth - marginX * 2;
  let y = 56;

  function ensureSpace(lines: number) {
    const needed = lines * 14 + 10;
    if (y + needed > doc.internal.pageSize.getHeight() - 48) {
      doc.addPage();
      y = 56;
    }
  }

  function heading(text: string) {
    ensureSpace(2);
    doc.setFont("helvetica", "bold");
    doc.setFontSize(12);
    doc.text(text.toUpperCase(), marginX, y);
    y += 4;
    doc.setDrawColor(200);
    doc.line(marginX, y, pageWidth - marginX, y);
    y += 16;
  }

  function paragraph(text: string, opts: { bold?: boolean; size?: number } = {}) {
    doc.setFont("helvetica", opts.bold ? "bold" : "normal");
    doc.setFontSize(opts.size ?? 10);
    const lines = doc.splitTextToSize(text, contentWidth) as string[];
    ensureSpace(lines.length);
    doc.text(lines, marginX, y);
    y += lines.length * 13 + 4;
  }

  doc.setFont("helvetica", "bold");
  doc.setFontSize(20);
  doc.text(resume.fullName, marginX, y);
  y += 22;

  if (resume.headline) {
    doc.setFont("helvetica", "normal");
    doc.setFontSize(12);
    doc.text(resume.headline, marginX, y);
    y += 20;
  }

  const links = [resume.linkedInUrl, resume.githubUrl, resume.portfolioUrl].filter(Boolean).join("  •  ");
  if (links) {
    doc.setFontSize(9);
    doc.setTextColor(90);
    doc.text(links, marginX, y);
    doc.setTextColor(0);
    y += 20;
  } else {
    y += 6;
  }

  if (resume.summary) {
    heading("Summary");
    paragraph(resume.summary);
  }

  if (resume.skillsCsv) {
    heading("Skills");
    paragraph(resume.skillsCsv.split(",").map((s) => s.trim()).filter(Boolean).join("  •  "));
  }

  if (resume.workExperiences.length > 0) {
    heading("Work Experience");
    for (const exp of resume.workExperiences) {
      const dateRange = `${formatDate(exp.startDate)} – ${exp.endDate ? formatDate(exp.endDate) : "Present"}`;
      paragraph(`${exp.title} — ${exp.company}${exp.location ? `, ${exp.location}` : ""}`, { bold: true });
      paragraph(dateRange, { size: 9 });
      if (exp.description) paragraph(exp.description);
      y += 4;
    }
  }

  if (resume.educations.length > 0) {
    heading("Education");
    for (const edu of resume.educations) {
      paragraph(`${edu.degree}${edu.fieldOfStudy ? `, ${edu.fieldOfStudy}` : ""} — ${edu.institution}`, { bold: true });
      const dateRange = [formatDate(edu.startDate), formatDate(edu.endDate)].filter(Boolean).join(" – ");
      if (dateRange || edu.gradeOrGpa) paragraph([dateRange, edu.gradeOrGpa].filter(Boolean).join("  •  "), { size: 9 });
      if (edu.description) paragraph(edu.description);
      y += 4;
    }
  }

  if (resume.projects.length > 0) {
    heading("Projects");
    for (const proj of resume.projects) {
      paragraph(proj.title, { bold: true });
      if (proj.technologiesCsv) paragraph(proj.technologiesCsv, { size: 9 });
      if (proj.description) paragraph(proj.description);
      y += 4;
    }
  }

  if (resume.certifications.length > 0) {
    heading("Certifications");
    for (const cert of resume.certifications) {
      paragraph(`${cert.name}${cert.issuingOrganization ? ` — ${cert.issuingOrganization}` : ""}`, { bold: true });
      if (cert.issueDate) paragraph(formatDate(cert.issueDate), { size: 9 });
    }
  }

  if (resume.achievementsText) {
    heading("Achievements");
    for (const line of resume.achievementsText.split("\n").filter((l) => l.trim())) {
      paragraph(`•  ${line.trim()}`);
    }
  }

  return doc.output("blob");
}
