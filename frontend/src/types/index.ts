export type UserRole = "Candidate" | "Recruiter" | "Admin";

export interface AuthResponse {
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
  token: string;
  expiresAt: string;
  avatarUrl: string | null;
  refreshToken: string;
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
  shareCount: number;
  companyIsVerified: boolean;
  screeningQuestions: ScreeningQuestion[];
}

export type ScreeningQuestionType = "ShortText" | "LongText" | "YesNo" | "SingleChoice" | "MultipleChoice" | "Number" | "Url";

export interface ScreeningQuestionOption {
  id: number;
  optionText: string;
  displayOrder: number;
}

export interface ScreeningQuestion {
  id: number;
  questionText: string;
  questionType: ScreeningQuestionType;
  isRequired: boolean;
  helpText: string | null;
  options: ScreeningQuestionOption[];
  displayOrder: number;
  /** Recruiter-only reference answer — never present for a candidate/public viewer. */
  preferredAnswer?: string | null;
}

export interface UpsertScreeningQuestionRequest {
  id?: number;
  questionText: string;
  questionType: ScreeningQuestionType;
  isRequired: boolean;
  helpText?: string;
  options?: string[];
  displayOrder: number;
  preferredAnswer?: string;
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
  screeningQuestions?: UpsertScreeningQuestionRequest[];
}

export type UpdateJobPostingRequest = Omit<CreateJobPostingRequest, "saveAsDraft">;

export interface JobQualitySuggestion {
  label: string;
  tip: string;
}

export interface JobQualityScore {
  score: number;
  suggestions: JobQualitySuggestion[];
}

export interface RecruiterJobSummary {
  job: JobPosting;
  applicationCount: number;
  qualityScore: JobQualityScore;
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
  isVerified: boolean;
  averageRating: number | null;
  reviewCount: number;
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
  jobLocation: string | null;
  nextInterviewAtUtc: string | null;
  candidateAvatarUrl: string | null;
  candidateProfileId: number | null;
  requiredQuestionsAnsweredCount: number;
  requiredQuestionsTotalCount: number;
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
  nextAction: string;
  screeningAnswers: ScreeningAnswer[];
}

export interface SubmitScreeningAnswerRequest {
  questionId: number;
  textValue?: string;
  numberValue?: number;
  selectedOptionIds?: number[];
}

export interface ScreeningAnswer {
  questionId: number;
  questionText: string;
  questionType: ScreeningQuestionType;
  isRequired: boolean;
  textValue: string | null;
  numberValue: number | null;
  selectedOptionTexts: string[];
  /** Only populated when the viewer is the owning recruiter. */
  preferredAnswer?: string | null;
}

export interface ApplicantScreeningFilter {
  questionId?: number;
  yesNo?: "Yes" | "No";
  optionId?: number;
  minNumber?: number;
  maxNumber?: number;
  requiredAnsweredOnly?: boolean;
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

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
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

export interface NextBestAction {
  label: string;
  description: string;
  linkPath: string;
}

export interface CandidateDashboard {
  profileCompletionPercent: number;
  applicationSummary: ApplicationStatusSummary;
  recentApplications: JobApplication[];
  recommendedJobs: JobPosting[];
  skillSuggestions: string[];
  recentlyViewedJobs: JobPosting[];
  savedJobs: JobPosting[];
  activeAlertCount: number;
  alertMatches: JobPosting[];
  upcomingInterviews: UpcomingInterview[];
  nextBestActions: NextBestAction[];
  recentAssessmentResults: AssessmentAttemptHistoryItem[];
  careerGoalsSummary: CareerGoalsSummary;
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
  name: string | null;
  keyword: string | null;
  minSalary: number | null;
  maxSalary: number | null;
  sortOption: string | null;
  isDefault: boolean;
}

export interface UpsertJobAlertRequest {
  skillsCsv?: string;
  state?: string;
  city?: string;
  isRemote?: boolean;
  jobType?: string;
  minExperienceYears?: number;
  isActive?: boolean;
  name?: string;
  keyword?: string;
  minSalary?: number;
  maxSalary?: number;
  sortOption?: string;
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
  avatarUrl: string | null;
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
  shareCount: number;
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
  avatarUrl: string | null;
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
  avatarUrl: string | null;
  workExperiences: WorkExperience[] | null;
  educations: EducationEntry[] | null;
  certifications: Certification[] | null;
  projects: ResumeProject[] | null;
  achievementsText: string | null;
  assessmentBadges: RecruiterVisibleBadge[] | null;
}

// --- Resume builder ---------------------------------------------------------

export interface WorkExperience {
  id: number;
  title: string;
  company: string;
  location: string | null;
  startDate: string;
  endDate: string | null;
  description: string | null;
  displayOrder: number;
}

export interface UpsertWorkExperienceRequest {
  title: string;
  company: string;
  location?: string;
  startDate: string;
  endDate?: string | null;
  description?: string;
}

export interface EducationEntry {
  id: number;
  institution: string;
  degree: string;
  fieldOfStudy: string | null;
  startDate: string | null;
  endDate: string | null;
  gradeOrGpa: string | null;
  description: string | null;
  displayOrder: number;
}

export interface UpsertEducationEntryRequest {
  institution: string;
  degree: string;
  fieldOfStudy?: string;
  startDate?: string | null;
  endDate?: string | null;
  gradeOrGpa?: string;
  description?: string;
}

export interface Certification {
  id: number;
  name: string;
  issuingOrganization: string | null;
  issueDate: string | null;
  expiryDate: string | null;
  credentialUrl: string | null;
  displayOrder: number;
}

export interface UpsertCertificationRequest {
  name: string;
  issuingOrganization?: string;
  issueDate?: string | null;
  expiryDate?: string | null;
  credentialUrl?: string;
}

export interface ResumeProject {
  id: number;
  title: string;
  description: string | null;
  projectUrl: string | null;
  technologiesCsv: string | null;
  displayOrder: number;
}

export interface UpsertProjectRequest {
  title: string;
  description?: string;
  projectUrl?: string;
  technologiesCsv?: string;
}

export interface ReorderRequest {
  orderedIds: number[];
}

export interface MissingProfileItem {
  label: string;
  tip: string;
  linkPath: string;
}

export interface ProfileStrength {
  score: number;
  missingItems: MissingProfileItem[];
}

export interface UpsertResumeSummaryRequest {
  summary?: string;
  skillsCsv?: string;
  linkedInUrl?: string;
  githubUrl?: string;
  portfolioUrl?: string;
  achievementsText?: string;
}

export interface Resume {
  fullName: string;
  headline: string | null;
  summary: string | null;
  skillsCsv: string | null;
  linkedInUrl: string | null;
  githubUrl: string | null;
  portfolioUrl: string | null;
  achievementsText: string | null;
  workExperiences: WorkExperience[];
  educations: EducationEntry[];
  certifications: Certification[];
  projects: ResumeProject[];
  strength: ProfileStrength;
}

// --- Cover letter templates -------------------------------------------------

export interface CoverLetterTemplate {
  id: number;
  title: string;
  introduction: string | null;
  skillsHighlights: string | null;
  projectAchievements: string | null;
  closingMessage: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface UpsertCoverLetterTemplateRequest {
  title: string;
  introduction?: string;
  skillsHighlights?: string;
  projectAchievements?: string;
  closingMessage?: string;
}

// --- Skill assessments -------------------------------------------------------

export type AssessmentCategoryName =
  | "Java" | "CSharpDotNet" | "React" | "JavaScript" | "Sql" | "Python" | "Communication" | "Aptitude";

export interface AssessmentCategorySummary {
  category: AssessmentCategoryName;
  questionBankSize: number;
  bestPercentageScore: number | null;
  lastAttemptAt: string | null;
  canAttemptNow: boolean;
  cooldownEndsAtUtc: string | null;
  hasActiveAttempt: boolean;
}

export interface AssessmentQuestionForAttempt {
  answerId: number;
  displayOrder: number;
  questionText: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
}

export interface AssessmentAttemptInProgress {
  attemptId: number;
  category: AssessmentCategoryName;
  startedAtUtc: string;
  expiresAtUtc: string;
  questions: AssessmentQuestionForAttempt[];
  selectedOptionsByAnswerId: Record<number, number>;
}

export interface AssessmentReviewQuestion {
  questionText: string;
  optionA: string;
  optionB: string;
  optionC: string;
  optionD: string;
  correctOptionIndex: number;
  selectedOptionIndex: number | null;
  explanation: string | null;
}

export interface AssessmentAttemptResult {
  attemptId: number;
  category: AssessmentCategoryName;
  scoreCorrectCount: number;
  totalQuestionCount: number;
  percentageScore: number;
  submittedAtUtc: string;
  isVisibleToRecruiters: boolean;
  review: AssessmentReviewQuestion[];
}

export interface AssessmentAttemptHistoryItem {
  attemptId: number;
  category: AssessmentCategoryName;
  scoreCorrectCount: number;
  totalQuestionCount: number;
  percentageScore: number;
  submittedAtUtc: string;
  isVisibleToRecruiters: boolean;
}

export interface RecruiterVisibleBadge {
  category: AssessmentCategoryName;
  percentageScore: number;
  submittedAtUtc: string;
}

// --- Public portfolio profile ------------------------------------------------

export interface PublicCandidateProfile {
  fullName: string;
  headline: string | null;
  avatarUrl: string | null;
  skillsCsv: string | null;
  summary: string | null;
  experienceSummary: string | null;
  totalExperienceYears: number | null;
  education: string | null;
  projects: ResumeProject[];
  linkedInUrl: string | null;
  githubUrl: string | null;
  portfolioUrl: string | null;
  assessmentBadges: RecruiterVisibleBadge[];
}

export interface PublicProfilePreview {
  isCurrentlyPublic: boolean;
  publicUrl: string | null;
  preview: PublicCandidateProfile;
}

// --- Career goals -------------------------------------------------------------

export type CareerGoalStatusName = "InProgress" | "Completed" | "Paused";

export interface CareerGoalSuggestion {
  label: string;
  tip: string;
  linkPath: string;
}

export interface CareerGoal {
  id: number;
  targetRole: string | null;
  targetSkill: string | null;
  targetCompanyType: string | null;
  preferredState: string | null;
  preferredCity: string | null;
  isLocationRemote: boolean;
  targetCompletionDate: string | null;
  progressPercent: number;
  status: CareerGoalStatusName;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
  suggestions: CareerGoalSuggestion[];
}

export interface UpsertCareerGoalRequest {
  targetRole?: string;
  targetSkill?: string;
  targetCompanyType?: string;
  preferredState?: string;
  preferredCity?: string;
  isLocationRemote: boolean;
  targetCompletionDate?: string | null;
  progressPercent: number;
  status: CareerGoalStatusName;
  notes?: string;
}

export interface CareerGoalsSummary {
  goals: CareerGoal[];
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

// --- Offers -------------------------------------------------------------------

export type OfferStatusName = "Draft" | "Sent" | "Viewed" | "Accepted" | "Declined" | "Withdrawn" | "Expired";
export type SalaryTypeName = "Monthly" | "Annual";

export interface Offer {
  id: number;
  jobApplicationId: number;
  jobTitle: string;
  companyName: string;
  candidateName: string;
  offeredSalary: number;
  salaryType: SalaryTypeName;
  joiningDate: string;
  workCity: string | null;
  workState: string | null;
  isRemote: boolean;
  employmentType: string;
  probationDetails: string | null;
  benefits: string | null;
  expiryDateUtc: string;
  recruiterMessage: string | null;
  status: OfferStatusName;
  sentAtUtc: string | null;
  respondedAtUtc: string | null;
  candidateResponseNote: string | null;
  createdAt: string;
}

export interface OfferStatusHistoryEntry {
  fromStatus: string | null;
  toStatus: string;
  changedByName: string;
  changedAt: string;
  note: string | null;
}

export interface OfferDetail {
  offer: Offer;
  statusHistory: OfferStatusHistoryEntry[];
}

export interface UpsertOfferRequest {
  offeredSalary: number;
  salaryType: SalaryTypeName;
  joiningDate: string;
  workCity?: string;
  workState?: string;
  isRemote: boolean;
  employmentType: string;
  probationDetails?: string;
  benefits?: string;
  expiryDateUtc: string;
  recruiterMessage?: string;
}

export interface RespondToOfferRequest {
  accept: boolean;
  note?: string;
}

// --- Talent pools ---------------------------------------------------------------

export interface TalentPool {
  id: number;
  name: string;
  candidateCount: number;
  createdAt: string;
}

export interface TalentPoolCandidate {
  candidateProfileId: number;
  fullName: string;
  headline: string | null;
  skillsCsv: string | null;
  totalExperienceYears: number | null;
  displayLocation: string;
  avatarUrl: string | null;
  latestApplicationJobTitle: string | null;
  latestApplicationStatus: string | null;
  matchScore: number | null;
  notes: string | null;
  tagsCsv: string | null;
  addedAt: string;
}

export interface AddCandidateToPoolRequest {
  candidateProfileId: number;
  notes?: string;
  tagsCsv?: string;
}

export interface UpdatePoolCandidateNotesRequest {
  notes?: string;
  tagsCsv?: string;
}

// --- Referrals --------------------------------------------------------------------

export type ReferralStatusName = "Invited" | "Registered" | "Applied" | "Interviewing" | "Hired" | "NotSelected";

export interface CreateReferralRequest {
  referredName: string;
  referredEmail: string;
  referredPhone?: string;
  relevantSkillsCsv?: string;
  note?: string;
  jobPostingId: number;
}

export interface CreateReferralResponse {
  id: number;
  rawToken: string;
  tokenExpiresAtUtc: string;
}

export interface Referral {
  id: number;
  referredName: string;
  referredEmail: string;
  jobTitle: string;
  companyName: string;
  status: ReferralStatusName;
  createdAt: string;
  registeredAtUtc: string | null;
  appliedAtUtc: string | null;
}

export interface ReferralTokenPreview {
  jobTitle: string;
  companyName: string;
  tokenExpiresAtUtc: string;
}

// --- Company verification ---------------------------------------------------------

export type CompanyVerificationStatusName = "NotSubmitted" | "Pending" | "Verified" | "Rejected" | "NeedsMoreInfo";

export interface SubmitCompanyVerificationRequest {
  website?: string;
  businessEmail: string;
  city?: string;
  state?: string;
  description?: string;
  verificationDocumentReference?: string;
}

export interface CompanyVerificationStatusDto {
  status: CompanyVerificationStatusName;
  note: string | null;
  submittedAtUtc: string | null;
  reviewedAtUtc: string | null;
}

export interface PendingCompanyVerification {
  companyId: number;
  companyName: string;
  website: string | null;
  businessEmail: string | null;
  city: string | null;
  state: string | null;
  description: string | null;
  verificationDocumentReference: string | null;
  status: CompanyVerificationStatusName;
  submittedAtUtc: string | null;
}

export interface SetCompanyVerificationStatusRequest {
  status: CompanyVerificationStatusName;
  note?: string;
}

// --- Company follows ---------------------------------------------------------------

export interface FollowedCompany {
  companyId: number;
  companyName: string;
  logoUrl: string | null;
  industry: string | null;
  notifyOnNewJob: boolean;
  followedAtUtc: string;
}

// --- Activity timeline ---------------------------------------------------------------

export type ActivityTimelineSource = "Action" | "Notification";

export interface ActivityTimelineEntry {
  type: string;
  message: string;
  timestampUtc: string;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
  source: ActivityTimelineSource;
}

// --- Company reviews ---------------------------------------------------------------

export type ReviewerRelationshipTypeName = "Applicant" | "Interviewed" | "ReceivedOffer" | "Hired";
export type ReviewStatusName = "Pending" | "Published" | "Rejected" | "Flagged";

export interface SubmitCompanyReviewRequest {
  overallRating: number;
  workCultureRating: number;
  interviewExperienceRating: number;
  workLifeBalanceRating: number;
  careerGrowthRating: number;
  title: string;
  pros: string;
  cons: string;
  adviceToManagement?: string;
  relationshipType: ReviewerRelationshipTypeName;
}

export interface PublicCompanyReview {
  id: number;
  overallRating: number;
  workCultureRating: number;
  interviewExperienceRating: number;
  workLifeBalanceRating: number;
  careerGrowthRating: number;
  title: string;
  pros: string;
  cons: string;
  adviceToManagement: string | null;
  relationshipType: ReviewerRelationshipTypeName;
  recruiterResponse: string | null;
  recruiterRespondedAt: string | null;
  createdAt: string;
}

export interface CompanyReviewRatingBreakdown {
  stars: number;
  count: number;
}

export interface CompanyReviewsSummary {
  averageRating: number | null;
  reviewCount: number;
  ratingBreakdown: CompanyReviewRatingBreakdown[];
  reviews: PublicCompanyReview[];
}

export interface ReviewEligibility {
  eligible: boolean;
  alreadyReviewed: boolean;
  reason: string | null;
}

export interface PendingCompanyReview {
  id: number;
  companyId: number;
  companyName: string;
  overallRating: number;
  title: string;
  pros: string;
  cons: string;
  adviceToManagement: string | null;
  relationshipType: ReviewerRelationshipTypeName;
  status: ReviewStatusName;
  reviewerName: string;
  createdAt: string;
}

// --- Salary insights ---------------------------------------------------------------

export interface SalaryInsight {
  roleTitle: string;
  experienceBand: string;
  city: string | null;
  state: string | null;
  isRemote: boolean;
  min: number | null;
  median: number | null;
  max: number | null;
  sampleCount: number;
  hasEnoughData: boolean;
}

// --- Privacy center & account export ---------------------------------------------------

export interface AccountDeletionStatus {
  isPending: boolean;
  requestedAtUtc: string | null;
  scheduledDeactivationAtUtc: string | null;
  daysRemaining: number | null;
}

export interface PrivacySummary {
  profileVisibility: string | null;
  messagesEnabled: boolean;
  applicationsEnabled: boolean;
  interviewsEnabled: boolean;
  invitationsEnabled: boolean;
  deletion: AccountDeletionStatus;
}

export interface AccountExportAccount {
  userId: number;
  fullName: string;
  email: string;
  phoneNumber: string | null;
  role: string;
  createdAt: string;
}

export interface AccountExportCandidateProfile {
  headline: string | null;
  summary: string | null;
  education: string | null;
  graduationYear: number | null;
  experienceSummary: string | null;
  totalExperienceYears: number | null;
  city: string | null;
  state: string | null;
  locality: string | null;
  skillsCsv: string | null;
  linkedInUrl: string | null;
  githubUrl: string | null;
  portfolioUrl: string | null;
  resumeOriginalFileName: string | null;
  availabilityStatus: string;
  preferredJobTypesCsv: string | null;
  preferredLocationsCsv: string | null;
  remotePreference: boolean | null;
  expectedSalaryMin: number | null;
  expectedSalaryMax: number | null;
  noticePeriodDays: number | null;
  profileVisibility: string;
}

export interface AccountExportRecruiterProfile {
  companyName: string;
  designation: string | null;
  companyRole: string;
}

export interface AccountExport {
  exportedAtUtc: string;
  account: AccountExportAccount;
  candidateProfile: AccountExportCandidateProfile | null;
  recruiterProfile: AccountExportRecruiterProfile | null;
  applications: { jobTitle: string; companyName: string; status: string; createdAt: string }[];
  savedJobs: { jobTitle: string; companyName: string; savedAtUtc: string }[];
  savedSearches: { name: string | null; keyword: string | null; city: string | null; state: string | null; isActive: boolean; createdAt: string }[];
  followedCompanies: { companyName: string; followedAtUtc: string }[];
  companyReviews: { companyName: string; overallRating: number; title: string; status: string; createdAt: string }[];
}
