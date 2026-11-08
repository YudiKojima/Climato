import { useCallback } from "react";
import { getWeatherByCity } from "../services/weather";
import useAsync from "./useAsync";

export default function useWeather(cityName, { image = true } = {}) {
  const load = useCallback(() => getWeatherByCity(cityName, { image }), [cityName, image]);
  return useAsync(load);
}
