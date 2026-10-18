const TOKEN_KEY = "token";
const EXPIRES_AT_KEY = "expiresAt";

export const saveSession = (auth) => {
  localStorage.setItem(TOKEN_KEY, auth.token);
  localStorage.setItem(EXPIRES_AT_KEY, auth.expiresAt);
};

export const getToken = () => localStorage.getItem(TOKEN_KEY);

export const clearSession = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(EXPIRES_AT_KEY);
};

export const hasValidSession = () => {
  const token = getToken();
  const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);

  return Boolean(token && expiresAt && new Date(expiresAt) > new Date());
};
