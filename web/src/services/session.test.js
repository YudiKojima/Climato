import { describe, expect, it } from "vitest";
import {
  clearSession,
  getToken,
  getUserName,
  hasValidSession,
  saveSession,
} from "./session";

const inMinutes = (minutes) =>
  new Date(Date.now() + minutes * 60_000).toISOString();

const auth = (expiresAt) => ({
  token: "token-de-teste",
  expiresAt,
  user: { name: "Ana" },
});

describe("session", () => {
  it("saveSession guarda token e nome do usuário", () => {
    saveSession(auth(inMinutes(5)));

    expect(getToken()).toBe("token-de-teste");
    expect(getUserName()).toBe("Ana");
  });

  it("hasValidSession é verdadeiro com expiração futura", () => {
    saveSession(auth(inMinutes(5)));

    expect(hasValidSession()).toBe(true);
  });

  it("hasValidSession é falso com expiração passada", () => {
    saveSession(auth(inMinutes(-5)));

    expect(hasValidSession()).toBe(false);
  });

  it("hasValidSession é falso sem sessão", () => {
    expect(hasValidSession()).toBe(false);
  });

  it("clearSession remove token, expiração e nome", () => {
    saveSession(auth(inMinutes(5)));

    clearSession();

    expect(getToken()).toBeNull();
    expect(getUserName()).toBeNull();
    expect(hasValidSession()).toBe(false);
  });
});
