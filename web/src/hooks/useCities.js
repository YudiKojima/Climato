import { getAllCities } from "../services/cities";
import useAsync from "./useAsync";

export default function useCities() {
  return useAsync(getAllCities);
}
