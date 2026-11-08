import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { addCity } from "./cities";

vi.mock("./client", () => ({ default: { post: vi.fn() } }));

describe("addCity", () => {
  it("envia nome e país para /cities", async () => {
    client.post.mockResolvedValue({ data: { id: "1", name: "Lima", country: "PE" } });

    const result = await addCity({ name: "Lima", country: "PE" });

    expect(client.post).toHaveBeenCalledWith("/cities", { name: "Lima", country: "PE" });
    expect(result).toEqual({ id: "1", name: "Lima", country: "PE" });
  });
});
