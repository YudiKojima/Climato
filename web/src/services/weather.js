import client from "./client";

export const getWeatherByCity = async (cityName, { image = true } = {}) => {
  const response = await client.get(`/weathers/${encodeURIComponent(cityName)}`, {
    params: image ? {} : { image: false },
  });
  return response.data;
};

export const getForecastByCity = async (cityName) => {
  const response = await client.get(`/forecasts/${encodeURIComponent(cityName)}`);
  return response.data;
};
