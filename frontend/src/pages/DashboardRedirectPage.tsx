import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { dashboardPathForRole } from "../components/ProtectedRoute";

export default function DashboardRedirectPage() {
  const { user } = useAuth();

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={dashboardPathForRole(user.role)} replace />;
}
