import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { loginAsDemo } from "./auth";

vi.mock("./client", () => ({ default: { post: vi.fn() } }));

describe("loginAsDemo", () => {
  it("chama /auth/demo e devolve a sessão", async () => {
    const auth = { token: "t", expiresAt: "2026-11-08T12:00:00Z", user: { name: "Visitante" } };
    client.post.mockResolvedValue({ data: auth });

    const result = await loginAsDemo();

    expect(client.post).toHaveBeenCalledWith("/auth/demo");
    expect(result).toEqual(auth);
  });
});
