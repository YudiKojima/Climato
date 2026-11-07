import client from "./client";

export const getWeatherByCity = async (cityName) => {
  const response = await client.get(`/weathers/${encodeURIComponent(cityName)}`);
  return response.data;
};
