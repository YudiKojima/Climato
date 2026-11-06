import { Navigate, Outlet } from "react-router-dom";
import { hasValidSession } from "../services/session";

export default function GuestRoute() {
  return hasValidSession() ? <Navigate to="/p" replace /> : <Outlet />;
}
