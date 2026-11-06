import { describe, expect, it } from "vitest";
import { formatTime } from "./formatTime";

describe("formatTime", () => {
  it("devolve a hora local da cidade, sem converter o fuso", () => {
    expect(formatTime("2026-11-05T06:12:00+09:00")).toBe("06:12");
  });
});
