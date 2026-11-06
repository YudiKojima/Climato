import { useCallback, useEffect, useMemo, useState } from "react";
import { ConfigProvider } from "antd";
import ptBR from "antd/locale/pt_BR";
import { ThemeContext } from "./ThemeContext";
import { getInitialMode, saveMode } from "./themeStorage";
import { getThemeConfig } from "./tokens";

export default function ThemeProvider({ children }) {
  const [mode, setMode] = useState(getInitialMode);

  const toggleMode = useCallback(() => {
    const next = mode === "dark" ? "light" : "dark";
    saveMode(next);
    setMode(next);
  }, [mode]);

  useEffect(() => {
    document.documentElement.style.colorScheme = mode;
  }, [mode]);

  const value = useMemo(() => ({ mode, toggleMode }), [mode, toggleMode]);
  const themeConfig = useMemo(() => getThemeConfig(mode), [mode]);

  return (
    <ThemeContext.Provider value={value}>
      <ConfigProvider theme={themeConfig} locale={ptBR}>
        {children}
      </ConfigProvider>
    </ThemeContext.Provider>
  );
}
