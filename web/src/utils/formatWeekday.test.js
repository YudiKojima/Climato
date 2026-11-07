import { describe, expect, it } from "vitest";
import { formatWeekday } from "./formatWeekday";

describe("formatWeekday", () => {
  it("mostra dia da semana e data em pt-BR", () => {
    expect(formatWeekday("2026-11-06")).toBe("sex., 06/11");
  });

  it("não pula de dia por causa do fuso", () => {
    expect(formatWeekday("2026-01-01")).toBe("qui., 01/01");
  });
});
