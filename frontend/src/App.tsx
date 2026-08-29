import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ToastProvider } from "./context/ToastContext";
import NavBar from "./components/NavBar";
import Footer from "./components/Footer";
import ToastContainer from "./components/ToastContainer";
import ProtectedRoute from "./components/ProtectedRoute";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
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
import "./App.css";

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ToastProvider>
        <NavBar />
        <ToastContainer />
        <main className="app-content">
          <Routes>
            <Route path="/" element={<Navigate to="/jobs" replace />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/jobs" element={<JobsPage />} />
            <Route path="/jobs/:id" element={<JobDetailPage />} />
            <Route path="/companies/:id" element={<CompanyProfilePage />} />
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
              path="/external-jobs"
              element={
                <ProtectedRoute>
                  <ExternalJobsPage />
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
    </BrowserRouter>
  );
}
