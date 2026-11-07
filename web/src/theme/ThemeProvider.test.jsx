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

  it("aplica o color-scheme do modo no documento", () => {
    vi.stubGlobal("matchMedia", vi.fn().mockReturnValue({ matches: true }));
    const { result } = renderHook(() => useThemeMode(), { wrapper });

    expect(document.documentElement.style.colorScheme).toBe("dark");

    act(() => result.current.toggleMode());

    expect(document.documentElement.style.colorScheme).toBe("light");
  });

  it("não persiste nada antes de o usuário escolher", () => {
    renderHook(() => useThemeMode(), { wrapper });

    expect(localStorage.getItem("climato.theme")).toBeNull();
  });
});
