import { jsPDF } from "jspdf";
import type { OfferDetail } from "../types";

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString("en-IN", { day: "numeric", month: "short", year: "numeric" });
}

function formatSalary(amount: number, salaryType: string): string {
  const formatted = amount.toLocaleString("en-IN");
  return `₹${formatted} ${salaryType === "Monthly" ? "per month" : "per annum"}`;
}

/** Builds a clean offer-summary PDF entirely client-side — no backend PDF dependency. This
 * is a portfolio/demo document, never presented as a legally binding offer letter or
 * e-signature system (see the mandatory disclaimer paragraph at the end). */
export function generateOfferPdf(detail: OfferDetail): Blob {
  const { offer } = detail;
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

  function paragraph(text: string, opts: { bold?: boolean; size?: number; italic?: boolean } = {}) {
    doc.setFont("helvetica", opts.bold ? "bold" : opts.italic ? "italic" : "normal");
    doc.setFontSize(opts.size ?? 10);
    const lines = doc.splitTextToSize(text, contentWidth) as string[];
    ensureSpace(lines.length);
    doc.text(lines, marginX, y);
    y += lines.length * 13 + 4;
  }

  doc.setFont("helvetica", "bold");
  doc.setFontSize(20);
  doc.text("Offer Summary", marginX, y);
  y += 26;

  doc.setFont("helvetica", "normal");
  doc.setFontSize(12);
  doc.text(`${offer.jobTitle} at ${offer.companyName}`, marginX, y);
  y += 18;
  doc.setFontSize(11);
  doc.text(`Candidate: ${offer.candidateName}`, marginX, y);
  y += 24;

  heading("Compensation");
  paragraph(formatSalary(offer.offeredSalary, offer.salaryType), { bold: true });

  heading("Joining & Location");
  paragraph(`Joining date: ${formatDate(offer.joiningDate)}`);
  paragraph(`Work location: ${offer.isRemote ? "Remote — India" : [offer.workCity, offer.workState].filter(Boolean).join(", ") || "Not specified"}`);
  paragraph(`Employment type: ${offer.employmentType.replace(/([a-z])([A-Z])/g, "$1 $2")}`);

  if (offer.probationDetails) {
    heading("Probation");
    paragraph(offer.probationDetails);
  }

  if (offer.benefits) {
    heading("Benefits");
    paragraph(offer.benefits);
  }

  if (offer.recruiterMessage) {
    heading("Message from the Recruiter");
    paragraph(offer.recruiterMessage);
  }

  heading("Offer Validity");
  paragraph(`This offer is valid until ${formatDate(offer.expiryDateUtc)}.`);

  y += 12;
  paragraph(
    "This is a demo/portfolio document generated for illustrative purposes only. It is not a legally binding offer of employment and does not constitute an e-signature or contract.",
    { italic: true, size: 8 },
  );

  return doc.output("blob");
}
