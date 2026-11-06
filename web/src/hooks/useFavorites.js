import { getAllFavorites } from "../services/cities";
import useAsync from "./useAsync";

export default function useFavorites() {
  return useAsync(getAllFavorites);
}
