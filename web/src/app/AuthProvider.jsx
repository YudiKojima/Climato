import { useCallback, useMemo, useState } from "react";
import { clearSession, getUserName, saveSession } from "../services/session";
import { AuthContext } from "./AuthContext";

const FALLBACK_NAME = "Usuário";

export default function AuthProvider({ children }) {
  const [userName, setUserName] = useState(() => getUserName() || FALLBACK_NAME);

  const signIn = useCallback((auth) => {
    saveSession(auth);
    setUserName(auth.user.name);
  }, []);

  const signOut = useCallback(() => {
    clearSession();
    setUserName(FALLBACK_NAME);
  }, []);

  const value = useMemo(
    () => ({ userName, signIn, signOut }),
    [userName, signIn, signOut]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
