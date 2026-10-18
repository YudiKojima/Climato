import { Navigate } from "react-router-dom";
import { hasValidSession } from "../services/session";

const ProtectedRoute = ({ children }) => {
  if (!hasValidSession()) {
    return <Navigate to="/signin" replace />;
  }

  return children;
};

export default ProtectedRoute;
