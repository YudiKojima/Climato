import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { getWeatherByCity } from "../services/weather";
import useWeather from "./useWeather";

vi.mock("../services/weather", () => ({ getWeatherByCity: vi.fn() }));

describe("useWeather", () => {
  it("refaz a carga quando a cidade muda", async () => {
    getWeatherByCity.mockImplementation(async (cityName) => ({ city: cityName }));
    const { result, rerender } = renderHook(({ cityName }) => useWeather(cityName), {
      initialProps: { cityName: "Lisboa" },
    });
    await waitFor(() => expect(result.current.data).toEqual({ city: "Lisboa" }));

    rerender({ cityName: "Recife" });

    await waitFor(() => expect(result.current.data).toEqual({ city: "Recife" }));
    expect(getWeatherByCity).toHaveBeenLastCalledWith("Recife", { image: true });
  });

  it("repassa image false ao serviço", async () => {
    getWeatherByCity.mockResolvedValue({ city: "Lima" });
    const { result } = renderHook(() => useWeather("Lima", { image: false }));

    await waitFor(() => expect(result.current.data).toEqual({ city: "Lima" }));
    expect(getWeatherByCity).toHaveBeenLastCalledWith("Lima", { image: false });
  });
});
