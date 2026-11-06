import { createContext, useContext } from "react";

export const ThemeContext = createContext({
  mode: "light",
  toggleMode: () => {},
});

export const useThemeMode = () => useContext(ThemeContext);
