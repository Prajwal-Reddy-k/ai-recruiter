import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { MapPin, Search, SlidersHorizontal, X } from "lucide-react";
import { getOpenJobs } from "../api/jobs";
import { getSavedJobs } from "../api/savedJobs";
import type { JobPosting } from "../types";
import { useAuth } from "../context/AuthContext";
import JobCard from "../components/JobCard";
import EmptyState from "../components/ui/EmptyState";
import Button from "../components/ui/Button";
import { JobCardSkeleton } from "../components/ui/Skeleton";
import {
  DEFAULT_FILTERS,
  extractCityFacets,
  extractSkillFacets,
  extractStateFacets,
  filterJobs,
  sortJobs,
  type DatePostedFilter,
  type ExperienceBucket,
  type JobFilters,
  type SortKey,
  type WorkMode,
} from "../utils/jobFilters";

const PAGE_SIZE = 9;
const EXPERIENCE_OPTIONS: { value: ExperienceBucket; label: string }[] = [
  { value: "0-2", label: "0-2 years" },
  { value: "3-5", label: "3-5 years" },
  { value: "6-10", label: "6-10 years" },
  { value: "10+", label: "10+ years" },
];
const JOB_TYPE_OPTIONS = ["FullTime", "PartTime", "Contract", "Internship", "Freelance"];

export default function JobsPage() {
  const { user } = useAuth();
  const [searchParams] = useSearchParams();
  const [allJobs, setAllJobs] = useState<JobPosting[]>([]);
  const [savedIds, setSavedIds] = useState<Set<number>>(new Set());
  const [loading, setLoading] = useState(true);
  const [titleQuery, setTitleQuery] = useState(() => searchParams.get("q") ?? "");
  const [locationQuery, setLocationQuery] = useState("");
  const [filters, setFilters] = useState<JobFilters>(DEFAULT_FILTERS);
  const [sortKey, setSortKey] = useState<SortKey>("newest");
  const [visibleCount, setVisibleCount] = useState(PAGE_SIZE);
  const [filtersOpen, setFiltersOpen] = useState(false);

  useEffect(() => {
    void loadJobs();
  }, []);

  useEffect(() => {
    const q = searchParams.get("q");
    if (q !== null) {
      setTitleQuery(q);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [searchParams]);

  useEffect(() => {
    if (user?.role === "Candidate") {
      getSavedJobs()
        .then((entries) => setSavedIds(new Set(entries.map((e) => e.job.id))))
        .catch(() => setSavedIds(new Set()));
    }
  }, [user]);

  async function loadJobs() {
    setLoading(true);
    try {
      const data = await getOpenJobs();
      setAllJobs(data);
    } finally {
      setLoading(false);
    }
  }

  const skillFacets = useMemo(() => extractSkillFacets(allJobs), [allJobs]);
  const stateFacets = useMemo(() => extractStateFacets(allJobs), [allJobs]);
  const cityFacets = useMemo(() => extractCityFacets(allJobs, filters.state), [allJobs, filters.state]);

  const combinedFilters: JobFilters = { ...filters, location: locationQuery };

  const filteredJobs = useMemo(() => {
    const searched = titleQuery.trim()
      ? allJobs.filter((job) => {
          const haystack = `${job.title} ${job.requiredSkillsCsv ?? ""} ${job.companyName}`.toLowerCase();
          return haystack.includes(titleQuery.trim().toLowerCase());
        })
      : allJobs;
    return sortJobs(filterJobs(searched, combinedFilters), sortKey);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [allJobs, titleQuery, filters, locationQuery, sortKey]);

  const visibleJobs = filteredJobs.slice(0, visibleCount);

  function toggleExperience(bucket: ExperienceBucket) {
    setVisibleCount(PAGE_SIZE);
    setFilters((prev) => ({
      ...prev,
      experience: prev.experience.includes(bucket)
        ? prev.experience.filter((b) => b !== bucket)
        : [...prev.experience, bucket],
    }));
  }

  function toggleSkill(skill: string) {
    setVisibleCount(PAGE_SIZE);
    setFilters((prev) => ({
      ...prev,
      skills: prev.skills.includes(skill) ? prev.skills.filter((s) => s !== skill) : [...prev.skills, skill],
    }));
  }

  function toggleJobType(jobType: string) {
    setVisibleCount(PAGE_SIZE);
    setFilters((prev) => ({
      ...prev,
      jobTypes: prev.jobTypes.includes(jobType) ? prev.jobTypes.filter((t) => t !== jobType) : [...prev.jobTypes, jobType],
    }));
  }

  function setStateFilter(state: string) {
    setVisibleCount(PAGE_SIZE);
    setFilters((prev) => ({ ...prev, state, city: "" }));
  }

  function setCityFilter(city: string) {
    setVisibleCount(PAGE_SIZE);
    setFilters((prev) => ({ ...prev, city }));
  }

  function setWorkMode(mode: WorkMode) {
    setVisibleCount(PAGE_SIZE);
    setFilters((prev) => ({ ...prev, workMode: mode }));
  }

  function setDatePosted(value: DatePostedFilter) {
    setVisibleCount(PAGE_SIZE);
    setFilters((prev) => ({ ...prev, datePosted: value }));
  }

  function clearFilters() {
    setFilters(DEFAULT_FILTERS);
    setLocationQuery("");
    setVisibleCount(PAGE_SIZE);
  }

  const hasActiveFilters =
    filters.workMode !== "all" ||
    filters.experience.length > 0 ||
    filters.skills.length > 0 ||
    filters.jobTypes.length > 0 ||
    filters.state !== "" ||
    filters.city !== "" ||
    filters.datePosted !== "any" ||
    locationQuery.trim() !== "";

  const filterSidebar = (
    <aside className={`filter-sidebar ui-card ui-card-padded ${filtersOpen ? "filter-sidebar-open" : ""}`}>
      <div className="section-header" style={{ marginBottom: "1rem" }}>
        <h3>Filters</h3>
        {hasActiveFilters && (
          <button type="button" className="link-button" onClick={clearFilters}>
            Clear all
          </button>
        )}
      </div>

      <div className="filter-group">
        <h4>Work mode</h4>
        {(["all", "remote", "onsite"] as WorkMode[]).map((mode) => (
          <label key={mode} className="filter-option">
            <input type="radio" name="workMode" checked={filters.workMode === mode} onChange={() => setWorkMode(mode)} />
            {mode === "all" ? "All" : mode === "remote" ? "Remote" : "On-site"}
          </label>
        ))}
      </div>

      <div className="filter-group">
        <h4>State</h4>
        <select value={filters.state} onChange={(e) => setStateFilter(e.target.value)}>
          <option value="">All states</option>
          {stateFacets.map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>

      {filters.state && (
        <div className="filter-group">
          <h4>City</h4>
          <select value={filters.city} onChange={(e) => setCityFilter(e.target.value)}>
            <option value="">All cities in {filters.state}</option>
            {cityFacets.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>
      )}

      <div className="filter-group">
        <h4>Job type</h4>
        {JOB_TYPE_OPTIONS.map((type) => (
          <label key={type} className="filter-option">
            <input type="checkbox" checked={filters.jobTypes.includes(type)} onChange={() => toggleJobType(type)} />
            {type.replace(/([A-Z])/g, " $1").trim()}
          </label>
        ))}
      </div>

      <div className="filter-group">
        <h4>Experience</h4>
        {EXPERIENCE_OPTIONS.map((opt) => (
          <label key={opt.value} className="filter-option">
            <input
              type="checkbox"
              checked={filters.experience.includes(opt.value)}
              onChange={() => toggleExperience(opt.value)}
            />
            {opt.label}
          </label>
        ))}
      </div>

      <div className="filter-group">
        <h4>Date posted</h4>
        {(
          [
            { value: "any", label: "Anytime" },
            { value: "24h", label: "Last 24 hours" },
            { value: "week", label: "Past week" },
            { value: "month", label: "Past month" },
          ] as { value: DatePostedFilter; label: string }[]
        ).map((opt) => (
          <label key={opt.value} className="filter-option">
            <input
              type="radio"
              name="datePosted"
              checked={filters.datePosted === opt.value}
              onChange={() => setDatePosted(opt.value)}
            />
            {opt.label}
          </label>
        ))}
      </div>

      {skillFacets.length > 0 && (
        <div className="filter-group">
          <h4>Skills</h4>
          <div className="filter-chip-list">
            {skillFacets.map((skill) => (
              <button
                key={skill}
                type="button"
                className={`filter-chip ${filters.skills.includes(skill) ? "filter-chip-active" : ""}`}
                onClick={() => toggleSkill(skill)}
              >
                {skill}
              </button>
            ))}
          </div>
        </div>
      )}
    </aside>
  );

  return (
    <div>
      <section className="jobs-hero">
        <h1>Find your next role</h1>
        <p>Search open positions by title, skill, or location.</p>
        <div className="jobs-search-bar">
          <div className="jobs-search-field">
            <Search size={18} />
            <input
              placeholder="Job title or skill"
              value={titleQuery}
              onChange={(e) => setTitleQuery(e.target.value)}
              aria-label="Search by job title or skill"
            />
          </div>
          <div className="jobs-search-field">
            <MapPin size={18} />
            <input
              placeholder="Location"
              value={locationQuery}
              onChange={(e) => setLocationQuery(e.target.value)}
              aria-label="Search by location"
            />
          </div>
        </div>
      </section>

      <button
        type="button"
        className="btn btn-secondary btn-sm mobile-filter-toggle"
        onClick={() => setFiltersOpen((v) => !v)}
      >
        <SlidersHorizontal size={16} /> {filtersOpen ? "Hide filters" : "Filters"}
      </button>

      <div className="jobs-layout">
        {filterSidebar}

        <div>
          <div className="results-bar">
            <span className="results-count">
              <strong>{filteredJobs.length}</strong> {filteredJobs.length === 1 ? "role" : "roles"} found
            </span>
            <select className="sort-select" value={sortKey} onChange={(e) => setSortKey(e.target.value as SortKey)}>
              <option value="newest">Newest first</option>
              <option value="salary-high">Highest salary</option>
            </select>
          </div>

          {loading ? (
            <ul className="job-list">
              {Array.from({ length: 4 }).map((_, i) => (
                <li key={i}><JobCardSkeleton /></li>
              ))}
            </ul>
          ) : filteredJobs.length === 0 ? (
            <EmptyState
              icon={<X size={32} />}
              title="No roles match your search"
              description="Try widening your filters or searching different keywords."
              action={hasActiveFilters ? <Button variant="secondary" onClick={clearFilters}>Clear filters</Button> : undefined}
            />
          ) : (
            <>
              <ul className="job-list">
                {visibleJobs.map((job) => (
                  <li key={job.id}>
                    <JobCard job={job} initiallySaved={savedIds.has(job.id)} />
                  </li>
                ))}
              </ul>
              {visibleCount < filteredJobs.length && (
                <div className="load-more-wrap">
                  <Button variant="secondary" onClick={() => setVisibleCount((c) => c + PAGE_SIZE)}>
                    Load more roles
                  </Button>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>
  );
}
