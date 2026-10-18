import axios from "axios";
import { clearSession, getToken } from "./session";

const api = axios.create({ baseURL: import.meta.env.VITE_API_URL });

api.interceptors.request.use((config) => {
  const token = getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
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

export const login = async (data) => {
  const response = await api.post("/auth/login", data);
  return response.data;
};

export const register = async (data) => {
  const response = await api.post("/auth/register", data);
  return response.data;
};

export const getAllCities = async () => {
  const response = await api.get("/cities");
  return response.data;
};

export const getAllFavorites = async () => {
  const response = await api.get("/cities/favorites");
  return response.data;
};

export const getWeatherByCity = async (cityName) => {
  const response = await api.get(`/weathers/${encodeURIComponent(cityName)}`);
  return response.data;
};

export const toggleFavorite = async (id) => {
  const response = await api.patch(`/cities/${id}/favorite`);
  return response.data;
};

export const deleteCity = async (id) => {
  await api.delete(`/cities/${id}`);
};

export const getApiErrorMessage = (
  error,
  fallback = "Não foi possível concluir a operação."
) => {
  const data = error.response?.data;
  const firstValidationError = data?.errors
    ? Object.values(data.errors).flat()[0]
    : undefined;

  return firstValidationError || data?.detail || data?.title || fallback;
};
