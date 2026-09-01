import type { JobPosting } from "../types";
import { formatExperienceRange, formatRelativeTime, formatSalaryRange } from "../utils/format";
import Modal from "./ui/Modal";

interface JobCompareModalProps {
  open: boolean;
  jobs: JobPosting[];
  onClose: () => void;
}

const ROWS: { label: string; render: (job: JobPosting) => string }[] = [
  { label: "Location", render: (j) => j.displayLocation },
  { label: "Experience", render: (j) => formatExperienceRange(j.minExperienceYears, j.maxExperienceYears) },
  { label: "Skills", render: (j) => j.requiredSkillsCsv || "—" },
  { label: "Job type", render: (j) => j.jobType },
  { label: "Salary", render: (j) => formatSalaryRange(j.minSalary, j.maxSalary) },
  { label: "Posted", render: (j) => formatRelativeTime(j.createdAt) },
];

export default function JobCompareModal({ open, jobs, onClose }: JobCompareModalProps) {
  return (
    <Modal open={open} onClose={onClose} title={`Compare ${jobs.length} job${jobs.length === 1 ? "" : "s"}`}>
      <div className="compare-table-wrap">
        <table className="compare-table">
          <thead>
            <tr>
              <th></th>
              {jobs.map((j) => <th key={j.id}>{j.title}<br /><span className="hint">{j.companyName}</span></th>)}
            </tr>
          </thead>
          <tbody>
            {ROWS.map((row) => (
              <tr key={row.label}>
                <th>{row.label}</th>
                {jobs.map((j) => <td key={j.id}>{row.render(j)}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Modal>
  );
}
