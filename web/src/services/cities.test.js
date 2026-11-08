import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { addCity } from "./cities";

vi.mock("./client", () => ({ default: { post: vi.fn() } }));

describe("addCity", () => {
  it("envia o nome para /cities", async () => {
    client.post.mockResolvedValue({ data: { id: "1", name: "Lima", country: "PE" } });

    const result = await addCity("Lima");

    expect(client.post).toHaveBeenCalledWith("/cities", { name: "Lima" });
    expect(result).toEqual({ id: "1", name: "Lima", country: "PE" });
  });
});
