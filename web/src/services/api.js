import axios from "axios";

const API = "https://localhost:44394";

export const getAllCities = async () => {
  const userId = localStorage.getItem("userId");
  const response = await axios.get(`${API}/cities?userId=${userId}`);
  return response.data;
};

export const getWeatherByCity = async (cityName) => {
  const userId = localStorage.getItem("userId");
  const response = await axios.get(
    `${API}/weathers/${cityName}?userId=${userId}`
  );
  return response.data;
};

export const toggleFavorite = async (id) => {
  const response = await axios.patch(`${API}/cities/${id}/favorite`);
  return response.data;
};

export const getAllFavorites = async () => {
  const userId = localStorage.getItem("userId");
  const response = await axios.get(`${API}/cities/favorites?userId=${userId}`);
  return response.data;
};

export const DeleteCity = async (id) => {
  const response = await axios.delete(`${API}/cities/${id}`);
  return response.data;
};

export const GetUser = async (data) => {
  const response = await axios.get(
    `${API}/users?email=${data.email}&password=${data.password}`
  );
  return response.data;
};

export const CreateUser = async (data) => {
  const response = await axios.post(`${API}/users`, data);
  return response.data;
};
