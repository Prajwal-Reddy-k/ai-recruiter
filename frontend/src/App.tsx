import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider, useAuth } from "./context/AuthContext";
import { ToastProvider } from "./context/ToastContext";
import { ThemeProvider } from "./context/ThemeContext";
import NavBar from "./components/NavBar";
import Footer from "./components/Footer";
import ToastContainer from "./components/ToastContainer";
import ProtectedRoute, { dashboardPathForRole } from "./components/ProtectedRoute";
import LandingPage from "./pages/LandingPage";
import HelpSupportPage from "./pages/HelpSupportPage";
import SettingsPage from "./pages/SettingsPage";
import ResumeBuilderPage from "./pages/ResumeBuilderPage";
import CoverLetterTemplatesPage from "./pages/CoverLetterTemplatesPage";
import SkillAssessmentsPage from "./pages/SkillAssessmentsPage";
import AssessmentAttemptPage from "./pages/AssessmentAttemptPage";
import CareerGoalsPage from "./pages/CareerGoalsPage";
import PublicTalentProfilePage from "./pages/PublicTalentProfilePage";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import ForgotPasswordPage from "./pages/ForgotPasswordPage";
import VerifyResetCodePage from "./pages/VerifyResetCodePage";
import ResetPasswordPage from "./pages/ResetPasswordPage";
import JobsPage from "./pages/JobsPage";
import JobDetailPage from "./pages/JobDetailPage";
import PostJobPage from "./pages/PostJobPage";
import OnboardingPage from "./pages/OnboardingPage";
import ManageJobsPage from "./pages/ManageJobsPage";
import CandidateProfilePage from "./pages/CandidateProfilePage";
import SavedJobsPage from "./pages/SavedJobsPage";
import CandidateSearchPage from "./pages/CandidateSearchPage";
import ApplicationsPage from "./pages/ApplicationsPage";
import ApplicationDetailPage from "./pages/ApplicationDetailPage";
import RecruiterApplicantsPage from "./pages/RecruiterApplicantsPage";
import KanbanBoardPage from "./pages/KanbanBoardPage";
import ExternalJobsPage from "./pages/ExternalJobsPage";
import CandidateDashboardPage from "./pages/CandidateDashboardPage";
import RecruiterDashboardPage from "./pages/RecruiterDashboardPage";
import RecruiterAnalyticsPage from "./pages/RecruiterAnalyticsPage";
import DashboardRedirectPage from "./pages/DashboardRedirectPage";
import JobAlertsPage from "./pages/JobAlertsPage";
import CompanyProfilePage from "./pages/CompanyProfilePage";
import AdminPage from "./pages/AdminPage";
import CandidateInterviewsPage from "./pages/CandidateInterviewsPage";
import RecruiterInterviewsPage from "./pages/RecruiterInterviewsPage";
import PlaceholderInfoPage from "./pages/PlaceholderInfoPage";
import RecruiterJobTemplatesPage from "./pages/RecruiterJobTemplatesPage";
import RecruiterMessagesPage from "./pages/RecruiterMessagesPage";
import CandidateMessagesPage from "./pages/CandidateMessagesPage";
import RecruiterTeamPage from "./pages/RecruiterTeamPage";
import RecruiterReportsPage from "./pages/RecruiterReportsPage";
import RecruiterTalentPoolsPage from "./pages/RecruiterTalentPoolsPage";
import TalentPoolDetailPage from "./pages/TalentPoolDetailPage";
import ReferralsPage from "./pages/ReferralsPage";
import RecruiterReferralsPage from "./pages/RecruiterReferralsPage";
import "./App.css";

function HomeRoute() {
  const { isAuthenticated, user } = useAuth();
  if (isAuthenticated && user) {
    return <Navigate to={dashboardPathForRole(user.role)} replace />;
  }
  return <LandingPage />;
}

export default function App() {
  return (
    <BrowserRouter>
      <ThemeProvider>
      <AuthProvider>
        <ToastProvider>
        <NavBar />
        <ToastContainer />
        <main className="app-content">
          <Routes>
            <Route path="/" element={<HomeRoute />} />
            <Route path="/help" element={<HelpSupportPage />} />
            <Route
              path="/settings"
              element={
                <ProtectedRoute>
                  <SettingsPage />
                </ProtectedRoute>
              }
            />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/forgot-password" element={<ForgotPasswordPage />} />
            <Route path="/verify-reset-code" element={<VerifyResetCodePage />} />
            <Route path="/reset-password" element={<ResetPasswordPage />} />
            <Route path="/jobs" element={<JobsPage />} />
            <Route path="/privacy" element={<PlaceholderInfoPage title="Privacy Policy" />} />
            <Route path="/terms" element={<PlaceholderInfoPage title="Terms of Service" />} />
            <Route path="/jobs/:id" element={<JobDetailPage />} />
            <Route path="/companies/:id" element={<CompanyProfilePage />} />
            <Route path="/talent/:slug" element={<PublicTalentProfilePage />} />
            <Route
              path="/post-job"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <PostJobPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/jobs/:id/edit"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <PostJobPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/onboarding"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <OnboardingPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/jobs/mine"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <ManageJobsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/candidates"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <CandidateSearchPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/saved-jobs"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <SavedJobsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/jobs/:id/applicants"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterApplicantsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/jobs/:id/board"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <KanbanBoardPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/profile"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <CandidateProfilePage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/resume-builder"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <ResumeBuilderPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/cover-letter-templates"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <CoverLetterTemplatesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/assessments"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <SkillAssessmentsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/assessments/attempt/:attemptId"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <AssessmentAttemptPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/career-goals"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <CareerGoalsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/applications"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <ApplicationsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/applications/:id"
              element={
                <ProtectedRoute allowedRoles={["Candidate", "Recruiter"]}>
                  <ApplicationDetailPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/alerts"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <JobAlertsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/interviews"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <CandidateInterviewsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/interviews"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterInterviewsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/templates"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterJobTemplatesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/messages"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterMessagesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/messages"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <CandidateMessagesPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/team"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterTeamPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/reports"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterReportsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/external-jobs"
              element={
                <ProtectedRoute>
                  <ExternalJobsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/talent-pools"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterTalentPoolsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/talent-pools/:id"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <TalentPoolDetailPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/referrals"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterReferralsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/referrals"
              element={
                <ProtectedRoute>
                  <ReferralsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/dashboard"
              element={
                <ProtectedRoute>
                  <DashboardRedirectPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/candidate/dashboard"
              element={
                <ProtectedRoute allowedRoles={["Candidate"]}>
                  <CandidateDashboardPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/dashboard"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterDashboardPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/recruiter/analytics"
              element={
                <ProtectedRoute allowedRoles={["Recruiter"]}>
                  <RecruiterAnalyticsPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin"
              element={
                <ProtectedRoute allowedRoles={["Admin"]}>
                  <AdminPage />
                </ProtectedRoute>
              }
            />
          </Routes>
        </main>
        <Footer />
        </ToastProvider>
      </AuthProvider>
      </ThemeProvider>
    </BrowserRouter>
  );
}
