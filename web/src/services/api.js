import axios from "axios";

const API = "https://localhost:44394";

export const getAllCities = async () => {
  const response = await axios.get(`${API}/cities`);
  return response.data;
};

export const getWeatherByCity = async (cityName) => {
  const response = await axios.get(`${API}/weathers/${cityName}`);
  return response.data;
};

export const toggleFavorite = async (id) => {
  const response = await axios.patch(`${API}/cities/${id}/favorite`);
  return response.data;
};

export const getAllFavorites = async () => {
  const response = await axios.get(`${API}/cities/favorites`);
  return response.data;
};

export const DeleteCity = async (id) => {
  const response = await axios.delete(`${API}/cities/${id}`);
  return response.data;
};
