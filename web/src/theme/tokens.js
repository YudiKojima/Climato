import { theme } from "antd";

const baseToken = {
  colorPrimary: "#0ea5e9",
  colorInfo: "#0ea5e9",
  borderRadius: 12,
  fontFamily:
    "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif",
  boxShadow:
    "0 6px 16px 0 rgba(15, 23, 42, 0.08), 0 3px 6px -4px rgba(15, 23, 42, 0.12)",
};

const palettes = {
  light: {
    algorithm: theme.defaultAlgorithm,
    token: { colorBgLayout: "#f0f7fc" },
    headerBg: "#ffffff",
  },
  dark: {
    algorithm: theme.darkAlgorithm,
    token: {
      colorBgLayout: "#0b1220",
      colorBgContainer: "#111a2e",
      colorBgElevated: "#16213a",
    },
    headerBg: "#111a2e",
  },
};

export const getThemeConfig = (mode) => {
  const palette = palettes[mode];

  return {
    algorithm: palette.algorithm,
    token: { ...baseToken, ...palette.token },
    components: {
      Layout: {
        headerBg: palette.headerBg,
        headerHeight: 72,
        headerPadding: "0 24px",
        footerBg: "transparent",
      },
    },
    cssVar: true,
  };
};
