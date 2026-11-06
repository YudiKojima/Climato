import axios from "axios";
import { clearSession, getToken } from "./session";

const client = axios.create({ baseURL: import.meta.env.VITE_API_URL });

client.interceptors.request.use((config) => {
  const token = getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

client.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAuthRoute = error.config?.url?.startsWith("/auth/");

    if (error.response?.status === 401 && !isAuthRoute) {
      clearSession();
      window.location.assign("/signin");
    }

    return Promise.reject(error);
  }
);

export default client;
