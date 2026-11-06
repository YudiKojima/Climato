import { useCallback } from "react";
import { getWeatherByCity } from "../services/weather";
import useAsync from "./useAsync";

export default function useWeather(cityName) {
  const load = useCallback(() => getWeatherByCity(cityName), [cityName]);
  return useAsync(load);
}
