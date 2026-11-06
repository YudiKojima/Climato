import { useCallback, useEffect, useState } from "react";

export default function useAsync(fn) {
  const [state, setState] = useState({
    data: undefined,
    isLoading: true,
    error: null,
  });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let isCurrent = true;
    setState((previous) => ({ ...previous, isLoading: true, error: null }));

    fn().then(
      (data) => {
        if (isCurrent) {
          setState({ data, isLoading: false, error: null });
        }
      },
      (error) => {
        if (isCurrent) {
          setState({ data: undefined, isLoading: false, error });
        }
      }
    );

    return () => {
      isCurrent = false;
    };
  }, [fn, attempt]);

  const reload = useCallback(() => setAttempt((count) => count + 1), []);

  return { ...state, reload };
}
