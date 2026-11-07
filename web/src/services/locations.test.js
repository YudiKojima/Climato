import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { searchLocations } from "./locations";

vi.mock("./client", () => ({ default: { get: vi.fn() } }));

describe("searchLocations", () => {
  it("consulta /locations com o termo em query", async () => {
    client.get.mockResolvedValue({ data: [{ name: "Tóquio", state: null, country: "JP" }] });

    const result = await searchLocations("Tóquio");

    expect(client.get).toHaveBeenCalledWith("/locations", { params: { query: "Tóquio" } });
    expect(result).toEqual([{ name: "Tóquio", state: null, country: "JP" }]);
  });
});
