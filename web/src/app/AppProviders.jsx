import { App } from "antd";
import ThemeProvider from "../theme/ThemeProvider";
import AuthProvider from "./AuthProvider";

export default function AppProviders({ children }) {
  return (
    <ThemeProvider>
      <App>
        <AuthProvider>{children}</AuthProvider>
      </App>
    </ThemeProvider>
  );
}
