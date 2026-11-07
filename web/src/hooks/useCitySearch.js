import { useCallback, useRef, useState } from "react";
import { searchLocations } from "../services/locations";
import useApiFeedback from "./useApiFeedback";

const MIN_QUERY_LENGTH = 2;
const INITIAL_STATE = { query: "", results: [], isSearching: false };

export default function useCitySearch() {
  const notifyError = useApiFeedback();
  const [state, setState] = useState(INITIAL_STATE);
  const latestSearch = useRef(0);

  const search = useCallback(
    async (value) => {
      const query = value.trim();

      if (query.length < MIN_QUERY_LENGTH) {
        return;
      }

      const searchId = ++latestSearch.current;
      setState({ query, results: [], isSearching: true });

      try {
        const results = await searchLocations(query);

        if (searchId === latestSearch.current) {
          setState({ query, results, isSearching: false });
        }
      } catch (err) {
        if (searchId === latestSearch.current) {
          setState({ query, results: [], isSearching: false });
          notifyError(err, "Não foi possível buscar cidades agora.");
        }
      }
    },
    [notifyError]
  );

  const clear = useCallback(() => {
    latestSearch.current += 1;
    setState(INITIAL_STATE);
  }, []);

  return { ...state, search, clear };
}
