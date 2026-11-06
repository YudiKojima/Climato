import { act, renderHook } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ThemeProvider from "./ThemeProvider";
import { useThemeMode } from "./ThemeContext";

const wrapper = ({ children }) => <ThemeProvider>{children}</ThemeProvider>;

describe("ThemeProvider", () => {
  it("toggleMode alterna o modo e persiste a escolha", () => {
    vi.stubGlobal("matchMedia", vi.fn().mockReturnValue({ matches: false }));
    const { result } = renderHook(() => useThemeMode(), { wrapper });

    expect(result.current.mode).toBe("light");

    act(() => result.current.toggleMode());

    expect(result.current.mode).toBe("dark");
    expect(localStorage.getItem("climato.theme")).toBe("dark");
  });

  it("não persiste nada antes de o usuário escolher", () => {
    renderHook(() => useThemeMode(), { wrapper });

    expect(localStorage.getItem("climato.theme")).toBeNull();
  });
});
