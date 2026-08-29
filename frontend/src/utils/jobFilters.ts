import type { JobPosting } from "../types";
import { daysAgo } from "./format";

export type WorkMode = "all" | "remote" | "onsite";
export type ExperienceBucket = "0-2" | "3-5" | "6-10" | "10+";
export type DatePostedFilter = "any" | "24h" | "week" | "month";
export type SortKey = "newest" | "salary-high";

export interface JobFilters {
  workMode: WorkMode;
  experience: ExperienceBucket[];
  location: string;
  state: string;
  city: string;
  jobTypes: string[];
  skills: string[];
  minSalary: number | null;
  datePosted: DatePostedFilter;
}

export const DEFAULT_FILTERS: JobFilters = {
  workMode: "all",
  experience: [],
  location: "",
  state: "",
  city: "",
  jobTypes: [],
  skills: [],
  minSalary: null,
  datePosted: "any",
};

function jobSkills(job: JobPosting): string[] {
  return (job.requiredSkillsCsv ?? "")
    .split(",")
    .map((s) => s.trim())
    .filter(Boolean);
}

function inExperienceBucket(minYears: number | null, bucket: ExperienceBucket): boolean {
  const years = minYears ?? 0;
  switch (bucket) {
    case "0-2":
      return years <= 2;
    case "3-5":
      return years >= 3 && years <= 5;
    case "6-10":
      return years >= 6 && years <= 10;
    case "10+":
      return years > 10;
  }
}

function withinDatePosted(createdAt: string, filter: DatePostedFilter): boolean {
  if (filter === "any") return true;
  const age = daysAgo(createdAt);
  if (filter === "24h") return age < 1;
  if (filter === "week") return age < 7;
  return age < 30;
}

export function filterJobs(jobs: JobPosting[], filters: JobFilters): JobPosting[] {
  return jobs.filter((job) => {
    if (filters.workMode === "remote" && !job.isRemote) return false;
    if (filters.workMode === "onsite" && job.isRemote) return false;

    if (filters.experience.length > 0 && !filters.experience.some((b) => inExperienceBucket(job.minExperienceYears, b))) {
      return false;
    }

    if (filters.location.trim()) {
      const haystack = `${job.city ?? ""} ${job.state ?? ""} ${job.locality ?? ""} ${job.displayLocation}`.toLowerCase();
      if (!haystack.includes(filters.location.trim().toLowerCase())) return false;
    }

    if (filters.state && job.state !== filters.state) return false;
    if (filters.city && job.city !== filters.city) return false;

    if (filters.jobTypes.length > 0 && !filters.jobTypes.includes(job.jobType)) return false;

    if (filters.skills.length > 0) {
      const skills = jobSkills(job).map((s) => s.toLowerCase());
      const matches = filters.skills.some((s) => skills.some((js) => js.includes(s.toLowerCase())));
      if (!matches) return false;
    }

    if (filters.minSalary !== null) {
      if (job.maxSalary === null || job.maxSalary < filters.minSalary) return false;
    }

    if (!withinDatePosted(job.createdAt, filters.datePosted)) return false;

    return true;
  });
}

export function sortJobs(jobs: JobPosting[], sortKey: SortKey): JobPosting[] {
  const copy = [...jobs];
  if (sortKey === "salary-high") {
    return copy.sort((a, b) => (b.maxSalary ?? b.minSalary ?? 0) - (a.maxSalary ?? a.minSalary ?? 0));
  }
  return copy.sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime());
}

/** Builds the skill filter checklist from whatever jobs are actually loaded, so the
 * facet list always reflects real data instead of a hardcoded list. */
export function extractSkillFacets(jobs: JobPosting[], limit = 12): string[] {
  const counts = new Map<string, number>();

  for (const job of jobs) {
    for (const skill of jobSkills(job)) {
      counts.set(skill, (counts.get(skill) ?? 0) + 1);
    }
  }

  return [...counts.entries()]
    .sort((a, b) => b[1] - a[1])
    .slice(0, limit)
    .map(([skill]) => skill);
}

/** Distinct India states present in the loaded jobs, for the state filter facet. */
export function extractStateFacets(jobs: JobPosting[]): string[] {
  return [...new Set(jobs.map((j) => j.state).filter((s): s is string => Boolean(s)))].sort();
}

/** Distinct cities within a given state (or all loaded jobs if no state is selected). */
export function extractCityFacets(jobs: JobPosting[], state: string): string[] {
  const source = state ? jobs.filter((j) => j.state === state) : jobs;
  return [...new Set(source.map((j) => j.city).filter((c): c is string => Boolean(c)))].sort();
}

/** Client-side "similar jobs" heuristic: shares at least one skill or the same city,
 * excludes the job itself. */
export function findSimilarJobs(currentJob: JobPosting, allJobs: JobPosting[], limit = 4): JobPosting[] {
  const currentSkills = new Set(jobSkills(currentJob).map((s) => s.toLowerCase()));

  return allJobs
    .filter((j) => j.id !== currentJob.id)
    .map((j) => {
      const shared = jobSkills(j).filter((s) => currentSkills.has(s.toLowerCase())).length;
      const sameCity = j.city && currentJob.city && j.city === currentJob.city ? 1 : 0;
      return { job: j, score: shared * 2 + sameCity };
    })
    .filter((x) => x.score > 0)
    .sort((a, b) => b.score - a.score)
    .slice(0, limit)
    .map((x) => x.job);
}
