import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { clearSession } from "../services/session";

const SignOut = () => {
  const navigate = useNavigate();

  useEffect(() => {
    clearSession();
    navigate("/signin");
  }, [navigate]);

  return null;
};

export default SignOut;
