import client from "./client";

export const getWeatherByCity = async (cityName) => {
  const response = await client.get(`/weathers/${encodeURIComponent(cityName)}`);
  return response.data;
};

export const getForecastByCity = async (cityName) => {
  const response = await client.get(`/forecasts/${encodeURIComponent(cityName)}`);
  return response.data;
};
