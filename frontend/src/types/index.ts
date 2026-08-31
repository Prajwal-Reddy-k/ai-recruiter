export type UserRole = "Candidate" | "Recruiter" | "Admin";

export interface AuthResponse {
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
  token: string;
  expiresAt: string;
}

export interface IndianState {
  name: string;
  cities: string[];
}

export interface IndiaLocationCatalog {
  states: IndianState[];
}

export interface JobPosting {
  id: number;
  title: string;
  description: string;
  requiredSkillsCsv: string | null;
  minExperienceYears: number | null;
  maxExperienceYears: number | null;
  minSalary: number | null;
  maxSalary: number | null;
  city: string | null;
  state: string | null;
  locality: string | null;
  isRemote: boolean;
  displayLocation: string;
  jobType: string;
  status: string;
  companyId: number;
  companyName: string;
  companyLogoUrl: string | null;
  createdAt: string;
  viewCount: number;
  publishedAt: string | null;
}

export interface CreateJobPostingRequest {
  title: string;
  description: string;
  requiredSkillsCsv?: string;
  minExperienceYears?: number;
  maxExperienceYears?: number;
  minSalary?: number;
  maxSalary?: number;
  city?: string;
  state?: string;
  locality?: string;
  isRemote: boolean;
  jobType: string;
  saveAsDraft?: boolean;
}

export type UpdateJobPostingRequest = Omit<CreateJobPostingRequest, "saveAsDraft">;

export interface RecruiterJobSummary {
  job: JobPosting;
  applicationCount: number;
}

export interface OnboardingStatus {
  isOnboarded: boolean;
  companyId: number | null;
  companyName: string | null;
  designation: string | null;
  website: string | null;
  industry: string | null;
  description: string | null;
  logoUrl: string | null;
  city: string | null;
  state: string | null;
  size: string | null;
  benefits: string | null;
  cultureHighlights: string | null;
  linkedInUrl: string | null;
  twitterUrl: string | null;
}

export interface UpsertOnboardingRequest {
  companyName: string;
  website?: string;
  industry?: string;
  description?: string;
  designation?: string;
  logoUrl?: string;
  city?: string;
  state?: string;
  size?: string;
  benefits?: string;
  cultureHighlights?: string;
  linkedInUrl?: string;
  twitterUrl?: string;
}

export interface CompanyProfile {
  id: number;
  name: string;
  website: string | null;
  industry: string | null;
  description: string | null;
  logoUrl: string | null;
  city: string | null;
  state: string | null;
  size: string | null;
  benefits: string | null;
  cultureHighlights: string | null;
  linkedInUrl: string | null;
  twitterUrl: string | null;
  openJobs: JobPosting[];
}

export interface CandidateProfile {
  id: number;
  fullName: string;
  headline: string | null;
  summary: string | null;
  education: string | null;
  experienceSummary: string | null;
  totalExperienceYears: number | null;
  city: string | null;
  state: string | null;
  locality: string | null;
  displayLocation: string;
  currentSalary: number | null;
  expectedSalary: number | null;
  skillsCsv: string | null;
  resumeOriginalFileName: string | null;
  resumeSizeBytes: number | null;
  resumeUploadedAt: string | null;
}

export interface UpsertCandidateProfileRequest {
  headline?: string;
  summary?: string;
  education?: string;
  experienceSummary?: string;
  totalExperienceYears?: number;
  city?: string;
  state?: string;
  locality?: string;
  currentSalary?: number;
  expectedSalary?: number;
  skillsCsv?: string;
}

export interface JobApplication {
  id: number;
  jobPostingId: number;
  jobTitle: string;
  companyName: string;
  candidateFullName: string | null;
  candidateHeadline: string | null;
  candidateSkillsCsv: string | null;
  status: string;
  createdAt: string;
  updatedAt: string | null;
  matchScore: number | null;
}

export interface StatusHistoryEntry {
  fromStatus: string | null;
  toStatus: string;
  changedByName: string;
  changedAt: string;
  note: string | null;
}

export interface JobApplicationDetail {
  id: number;
  jobPostingId: number;
  jobTitle: string;
  companyName: string;
  candidateProfileId: number;
  candidateFullName: string;
  status: string;
  coverNote: string | null;
  createdAt: string;
  updatedAt: string | null;
  matchScore: number | null;
  matchedSkills: string[];
  missingSkills: string[];
  suggestedImprovements: string[];
  scoringExplanation: string | null;
  statusHistory: StatusHistoryEntry[];
}

export interface LocationSuggestion {
  displayName: string;
  lat: number;
  lng: number;
}

export interface ExternalJobListing {
  title: string;
  companyName: string;
  location: string | null;
  description: string;
  url: string;
  salaryRange: string | null;
  postedAt: string | null;
  source: string;
}

export interface ExternalJobSearchResult {
  items: ExternalJobListing[];
  page: number;
  pageSize: number;
  totalCount: number | null;
}

export interface ApiProblem {
  title?: string;
  detail?: string;
  errorCode?: string;
}

export interface ApplicationStatusSummary {
  applied: number;
  underReview: number;
  shortlisted: number;
  rejected: number;
}

export interface DemoJobsSection {
  items: JobPosting[];
  isSampleData: boolean;
}

export interface UpcomingInterview {
  interviewId: number;
  jobApplicationId: number;
  jobTitle: string;
  companyName: string;
  candidateFullName: string;
  startUtc: string;
  endUtc: string;
}

export interface CandidateDashboard {
  profileCompletionPercent: number;
  applicationSummary: ApplicationStatusSummary;
  recentApplications: JobApplication[];
  recommendedJobs: JobPosting[];
  skillSuggestions: string[];
  recentlyViewedJobs: DemoJobsSection;
  savedJobs: JobPosting[];
  activeAlertCount: number;
  alertMatches: JobPosting[];
  upcomingInterviews: UpcomingInterview[];
}

export interface JobPerformance {
  jobId: number;
  title: string;
  status: string;
  applicantCount: number;
  viewCount: number;
  createdAt: string;
}

export interface RecruiterDashboard {
  onboardingStatus: OnboardingStatus;
  activeJobPostingCount: number;
  totalApplicantCount: number;
  recentApplications: JobApplication[];
  jobPerformance: JobPerformance[];
  upcomingInterviews: UpcomingInterview[];
}

export interface AppNotification {
  id: number;
  type: string;
  message: string;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
  isRead: boolean;
  createdAt: string;
}

export type InterviewTypeValue = "Online" | "Phone" | "InPerson";

export interface Interview {
  id: number;
  jobApplicationId: number;
  jobId: number;
  jobTitle: string;
  companyId: number;
  companyName: string;
  candidateProfileId: number;
  candidateFullName: string;
  scheduledStartUtc: string;
  scheduledEndUtc: string;
  type: InterviewTypeValue;
  location: string | null;
  recruiterNote: string | null;
  candidateResponseNote: string | null;
  status: string;
  canManage: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface ScheduleInterviewRequest {
  startUtc: string;
  endUtc: string;
  type: InterviewTypeValue;
  location?: string;
  recruiterNote?: string;
}

export interface RescheduleInterviewRequest {
  startUtc: string;
  endUtc: string;
  type?: InterviewTypeValue;
  location?: string;
  recruiterNote?: string;
}

export interface RespondInterviewRequest {
  responseNote?: string;
}

export interface JobAlert {
  id: number;
  skillsCsv: string | null;
  state: string | null;
  city: string | null;
  isRemote: boolean | null;
  jobType: string | null;
  minExperienceYears: number | null;
  isActive: boolean;
  matchingJobCount: number;
  matchingJobs: JobPosting[];
  createdAt: string;
}

export interface UpsertJobAlertRequest {
  skillsCsv?: string;
  state?: string;
  city?: string;
  isRemote?: boolean;
  jobType?: string;
  minExperienceYears?: number;
  isActive?: boolean;
}

export interface SavedJobEntry {
  job: JobPosting;
  savedAt: string;
}

export interface AdminUser {
  id: number;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

export interface AdminCompany {
  id: number;
  name: string;
  industry: string | null;
  jobCount: number;
  recruiterCount: number;
  createdAt: string;
}

export interface AdminJob {
  id: number;
  title: string;
  companyName: string;
  status: string;
  moderationStatus: string;
  applicationCount: number;
  createdAt: string;
}

export interface JobReport {
  id: number;
  jobPostingId: number;
  jobTitle: string;
  reportedByName: string;
  reason: string;
  status: string;
  resolutionNote: string | null;
  createdAt: string;
}

export interface AuditLogEntry {
  id: number;
  actorUserId: number | null;
  actorName: string | null;
  actorRole: string | null;
  actionType: string;
  entityType: string;
  entityId: number | null;
  timestampUtc: string;
  metadataJson: string | null;
}

export interface NamedCount {
  name: string;
  count: number;
}

export interface JobViewsVsApplications {
  jobId: number;
  title: string;
  viewCount: number;
  applicationCount: number;
}

export interface RecruiterAnalytics {
  activeJobs: number;
  totalApplications: number;
  shortlistedCandidates: number;
  interviewsScheduled: number;
  offersMade: number;
  applicationsPerJob: NamedCount[];
  hiringFunnel: NamedCount[];
  applicationsByCity: NamedCount[];
  topCandidateSkills: NamedCount[];
  viewsVsApplications: JobViewsVsApplications[];
}

export type CandidateSortOption = "NewestApplication" | "HighestMatchScore" | "ExperienceDesc" | "NameAlphabetical";

export interface CandidateSearchResult {
  applicationId: number;
  candidateProfileId: number;
  fullName: string;
  headline: string | null;
  skillsCsv: string | null;
  city: string | null;
  state: string | null;
  totalExperienceYears: number | null;
  education: string | null;
  applicationStatus: string;
  jobId: number;
  jobTitle: string;
  appliedAt: string;
  matchScore: number | null;
  canManage: boolean;
}

export interface InterviewSummary {
  interviewId: number;
  status: string;
  nextSlotUtc: string | null;
}

export interface CandidateApplicationSummary {
  applicationId: number;
  jobId: number;
  jobTitle: string;
  status: string;
  appliedAt: string;
  matchScore: number | null;
  hasResumeOnFile: boolean;
  canManage: boolean;
  interviews: InterviewSummary[];
}

export interface CandidateSearchDetail {
  candidateProfileId: number;
  fullName: string;
  headline: string | null;
  summary: string | null;
  education: string | null;
  experienceSummary: string | null;
  totalExperienceYears: number | null;
  displayLocation: string;
  skillsCsv: string | null;
  applications: CandidateApplicationSummary[];
}
