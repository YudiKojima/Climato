import client from "./client";

export const getAllCities = async () => {
  const response = await client.get("/cities");
  return response.data;
};

export const getAllFavorites = async () => {
  const response = await client.get("/cities/favorites");
  return response.data;
};

export const toggleFavorite = async (id) => {
  const response = await client.patch(`/cities/${id}/favorite`);
  return response.data;
};

export const deleteCity = async (id) => {
  await client.delete(`/cities/${id}`);
};

export const addCity = async ({ name, country }) => {
  const response = await client.post("/cities", { name, country });
  return response.data;
};
