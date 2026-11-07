import { Navigate, Outlet } from "react-router-dom";
import { hasValidSession } from "../services/session";

export default function ProtectedRoute() {
  return hasValidSession() ? <Outlet /> : <Navigate to="/signin" replace />;
}
