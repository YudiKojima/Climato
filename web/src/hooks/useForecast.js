import { useCallback } from "react";
import { getForecastByCity } from "../services/weather";
import useAsync from "./useAsync";

export default function useForecast(cityName) {
  const load = useCallback(() => getForecastByCity(cityName), [cityName]);
  return useAsync(load);
}
