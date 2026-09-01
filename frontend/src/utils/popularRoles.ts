/** Shared with Footer.tsx's "Popular Job Roles" grid and the Landing page's quick-links —
 * one list so both stay in sync. */
export const POPULAR_ROLES = [
  "Software Developer",
  "Java Developer",
  ".NET Developer",
  "React Developer",
  "Full Stack Developer",
  "Data Analyst",
  "Data Scientist",
  "UI/UX Designer",
  "Digital Marketing Executive",
  "HR Executive",
];

export function roleSearchPath(role: string): string {
  return `/jobs?q=${encodeURIComponent(role)}`;
}
