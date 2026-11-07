import client from "./client";

export const searchLocations = async (query) => {
  const response = await client.get("/locations", { params: { query } });
  return response.data;
};
