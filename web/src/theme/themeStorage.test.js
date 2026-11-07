import { describe, expect, it, vi } from "vitest";
import { getInitialMode, saveMode } from "./themeStorage";

const systemPrefersDark = (isDark) =>
  vi.stubGlobal("matchMedia", vi.fn().mockReturnValue({ matches: isDark }));

describe("getInitialMode", () => {
  it("sem preferência salva, segue o sistema escuro", () => {
    systemPrefersDark(true);

    expect(getInitialMode()).toBe("dark");
  });

  it("sem preferência salva, segue o sistema claro", () => {
    systemPrefersDark(false);

    expect(getInitialMode()).toBe("light");
  });

  it("preferência salva vence o sistema", () => {
    systemPrefersDark(true);
    saveMode("light");

    expect(getInitialMode()).toBe("light");
  });

  it("ignora valor salvo inválido", () => {
    systemPrefersDark(true);
    localStorage.setItem("climato.theme", "roxo");

    expect(getInitialMode()).toBe("dark");
  });

  it("localStorage bloqueado não quebra e segue o sistema", () => {
    systemPrefersDark(false);
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("bloqueado");
    });

    expect(() => saveMode("dark")).not.toThrow();
    expect(getInitialMode()).toBe("light");
  });
});
