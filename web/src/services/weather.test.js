import { describe, expect, it, vi } from "vitest";
import client from "./client";
import { getWeatherByCity } from "./weather";

vi.mock("./client", () => ({ default: { get: vi.fn() } }));

describe("getWeatherByCity", () => {
  it("pede o clima completo por padrão", async () => {
    client.get.mockResolvedValue({ data: { city: "São Paulo" } });

    const result = await getWeatherByCity("São Paulo");

    expect(client.get).toHaveBeenLastCalledWith("/weathers/S%C3%A3o%20Paulo", { params: {} });
    expect(result).toEqual({ city: "São Paulo" });
  });

  it("pede sem imagem quando image é false", async () => {
    client.get.mockResolvedValue({ data: { city: "Lima" } });

    await getWeatherByCity("Lima", { image: false });

    expect(client.get).toHaveBeenLastCalledWith("/weathers/Lima", { params: { image: false } });
  });
});
