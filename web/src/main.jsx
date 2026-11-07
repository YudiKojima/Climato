import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "antd/dist/reset.css";
import AppProviders from "./app/AppProviders";
import AppRouter from "./app/router";

createRoot(document.getElementById("root")).render(
  <StrictMode>
    <AppProviders>
      <AppRouter />
    </AppProviders>
  </StrictMode>
);
