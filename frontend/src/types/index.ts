export type UserRole = "Candidate" | "Recruiter" | "Admin";

export interface AuthResponse {
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
  token: string;
  expiresAt: string;
  avatarUrl: string | null;
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
  applicationDeadlineUtc: string | null;
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

export interface UserDetails {
  userId: number;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  role: string;
}

export interface UpdateUserDetailsRequest {
  fullName: string;
  phoneNumber?: string;
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
  graduationYear: number | null;
  experienceSummary: string | null;
  totalExperienceYears: number | null;
  city: string | null;
  state: string | null;
  locality: string | null;
  displayLocation: string;
  currentSalary: number | null;
  expectedSalary: number | null;
  skillsCsv: string | null;
  phone: string | null;
  linkedInUrl: string | null;
  githubUrl: string | null;
  portfolioUrl: string | null;
  resumeOriginalFileName: string | null;
  resumeSizeBytes: number | null;
  resumeUploadedAt: string | null;
  avatarUrl: string | null;
  availabilityStatus: string;
  preferredJobTypesCsv: string | null;
  preferredLocationsCsv: string | null;
  remotePreference: boolean | null;
  expectedSalaryMin: number | null;
  expectedSalaryMax: number | null;
  noticePeriodDays: number | null;
  preferredRolesCsv: string | null;
  profileVisibility: string;
}

export interface UpsertCandidateProfileRequest {
  headline?: string;
  summary?: string;
  education?: string;
  graduationYear?: number;
  experienceSummary?: string;
  totalExperienceYears?: number;
  city?: string;
  state?: string;
  locality?: string;
  currentSalary?: number;
  expectedSalary?: number;
  skillsCsv?: string;
  phone?: string;
  linkedInUrl?: string;
  githubUrl?: string;
  portfolioUrl?: string;
  availabilityStatus: string;
  preferredJobTypesCsv?: string;
  preferredLocationsCsv?: string;
  remotePreference?: boolean;
  expectedSalaryMin?: number;
  expectedSalaryMax?: number;
  noticePeriodDays?: number;
  preferredRolesCsv?: string;
  profileVisibility: string;
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
  fieldErrors?: Record<string, string>;
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

export interface Report {
  id: number;
  entityType: string;
  entityId: number;
  entityLabel: string | null;
  reportedByName: string;
  reason: string;
  details: string | null;
  status: string;
  moderationNote: string | null;
  reviewedByName: string | null;
  reviewedAt: string | null;
  createdAt: string;
}

// --- Candidate availability & preferences ---

export interface CandidateAvailabilityPreferences {
  availabilityStatus: string;
  preferredJobTypesCsv: string | null;
  preferredLocationsCsv: string | null;
  remotePreference: boolean | null;
  expectedSalaryMin: number | null;
  expectedSalaryMax: number | null;
  noticePeriodDays: number | null;
  preferredRolesCsv: string | null;
  profileVisibility: string;
}

// --- Recruiter candidate invitations ---

export interface Invitation {
  id: number;
  jobPostingId: number;
  jobTitle: string;
  companyName: string;
  candidateProfileId: number;
  candidateFullName: string;
  invitedByName: string;
  message: string | null;
  status: string;
  sentAtUtc: string;
  viewedAtUtc: string | null;
  respondedAtUtc: string | null;
  expiresAtUtc: string;
}

export interface DiscoverableCandidate {
  candidateProfileId: number;
  fullName: string;
  headline: string | null;
  skillsCsv: string | null;
  displayLocation: string;
  totalExperienceYears: number | null;
  availabilityStatus: string;
  remotePreference: boolean | null;
  preferredJobTypesCsv: string | null;
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

// --- Job templates ---------------------------------------------------------

export interface JobTemplate {
  id: number;
  title: string;
  department: string | null;
  description: string;
  responsibilities: string | null;
  requiredSkillsCsv: string | null;
  preferredSkillsCsv: string | null;
  minExperienceYears: number | null;
  maxExperienceYears: number | null;
  employmentType: string;
  salaryVisible: boolean;
  minSalary: number | null;
  maxSalary: number | null;
  defaultCity: string | null;
  defaultState: string | null;
  defaultLocality: string | null;
  defaultIsRemote: boolean;
  createdByName: string;
  canManage: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertJobTemplateRequest {
  title: string;
  department?: string;
  description: string;
  responsibilities?: string;
  requiredSkillsCsv?: string;
  preferredSkillsCsv?: string;
  minExperienceYears?: number;
  maxExperienceYears?: number;
  employmentType: number;
  salaryVisible: boolean;
  minSalary?: number;
  maxSalary?: number;
  defaultCity?: string;
  defaultState?: string;
  defaultLocality?: string;
  defaultIsRemote: boolean;
}

// --- Messaging ---------------------------------------------------------

export interface Message {
  id: number;
  jobApplicationId: number;
  senderUserId: number;
  senderRole: string;
  senderName: string;
  body: string;
  createdAt: string;
  isRead: boolean;
}

export interface ConversationSummary {
  jobApplicationId: number;
  jobId: number;
  jobTitle: string;
  companyName: string;
  counterpartName: string;
  lastMessageBody: string | null;
  lastMessageAt: string | null;
  unreadCount: number;
}

// --- Interview feedback ---------------------------------------------------------

export type InterviewRecommendationValue = "StrongNo" | "No" | "Neutral" | "Yes" | "StrongYes";

export interface InterviewFeedback {
  id: number;
  interviewId: number;
  recruiterProfileId: number;
  authorName: string;
  technicalScore: number;
  communicationScore: number;
  problemSolvingScore: number;
  cultureFitScore: number;
  recommendation: string;
  strengths: string | null;
  concerns: string | null;
  privateNotes: string | null;
  isDraft: boolean;
  submittedAtUtc: string | null;
  createdAt: string;
  updatedAt: string | null;
  canEdit: boolean;
  isMine: boolean;
}

export interface UpsertInterviewFeedbackRequest {
  technicalScore: number;
  communicationScore: number;
  problemSolvingScore: number;
  cultureFitScore: number;
  recommendation: number;
  strengths?: string;
  concerns?: string;
  privateNotes?: string;
}

export interface InterviewFeedbackSummary {
  interviewId: number;
  scorecards: InterviewFeedback[];
  averageTechnicalScore: number | null;
  averageCommunicationScore: number | null;
  averageProblemSolvingScore: number | null;
  averageCultureFitScore: number | null;
}

// --- Hiring team ---------------------------------------------------------

export type CompanyRoleValue = "Owner" | "Recruiter" | "HiringManager" | "Interviewer";

export interface TeamMember {
  recruiterProfileId: number;
  userId: number;
  fullName: string;
  email: string;
  designation: string | null;
  companyRole: string;
  joinedAt: string;
}

export interface JobAssignment {
  jobPostingId: number;
  recruiterProfileId: number;
  recruiterName: string;
  companyRole: string;
  assignedAt: string;
}

export interface InterviewAssignment {
  interviewId: number;
  recruiterProfileId: number;
  recruiterName: string;
  companyRole: string;
  assignedAt: string;
}

// --- Reports ---------------------------------------------------------

export interface RecruiterReport {
  jobsCreated: number;
  jobsPublished: number;
  jobsClosed: number;
  totalApplications: number;
  applicationsByJob: NamedCount[];
  applicationsByCity: NamedCount[];
  applicationsByState: NamedCount[];
  statusFunnel: NamedCount[];
  interviewsScheduledCount: number;
  interviewsCompletedCount: number;
  interviewConversionRatePercent: number | null;
  averageDaysToInterview: number | null;
  topCandidateSkills: NamedCount[];
  offersMade: number;
  hires: number;
}

// --- Feedback / Help & Support ---

export interface FeedbackSubmission {
  id: number;
  name: string;
  email: string;
  category: string;
  message: string;
  status: string;
  submittedByName: string | null;
  createdAt: string;
}

export interface SubmitFeedbackRequest {
  name: string;
  email: string;
  category: string;
  message: string;
}

// --- Account settings ---

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface RequestAccountDeletionRequest {
  password: string;
}

export interface NotificationPreferences {
  messagesEnabled: boolean;
  applicationsEnabled: boolean;
  interviewsEnabled: boolean;
  invitationsEnabled: boolean;
}

// --- Platform stats (public landing page) ---

export interface PlatformStats {
  openJobCount: number;
  candidateCount: number;
  companyCount: number;
}
