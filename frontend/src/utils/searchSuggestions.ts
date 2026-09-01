import { POPULAR_ROLES } from "./popularRoles";

/** Static quick-search suggestions for the job title/skill search box — kept small and
 * hand-picked rather than derived, so it's useful even before any jobs are loaded. */
export const SUGGESTED_SKILLS = [
  "React", "C#", "Python", "SQL", "AWS", "Docker", "Java", "Node.js", "TypeScript",
  "Kubernetes", "ASP.NET Core", "Angular", "Power BI", "Excel", "Figma",
];

export const SUGGESTED_SEARCHES = [...POPULAR_ROLES, ...SUGGESTED_SKILLS];
