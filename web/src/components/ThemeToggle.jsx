import { Button, Tooltip } from "antd";
import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { useThemeMode } from "../theme/ThemeContext";

export default function ThemeToggle() {
  const { mode, toggleMode } = useThemeMode();
  const isDark = mode === "dark";
  const label = isDark ? "Usar tema claro" : "Usar tema escuro";

  return (
    <Tooltip title={label}>
      <Button
        type="text"
        shape="circle"
        aria-label={label}
        icon={isDark ? <SunOutlined /> : <MoonOutlined />}
        onClick={toggleMode}
      />
    </Tooltip>
  );
}
