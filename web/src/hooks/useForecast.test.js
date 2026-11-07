import { renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { getForecastByCity } from "../services/weather";
import useForecast from "./useForecast";

vi.mock("../services/weather", () => ({ getForecastByCity: vi.fn() }));

describe("useForecast", () => {
  it("refaz a carga quando a cidade muda", async () => {
    getForecastByCity.mockImplementation(async (cityName) => ({ city: cityName, days: [] }));
    const { result, rerender } = renderHook(({ cityName }) => useForecast(cityName), {
      initialProps: { cityName: "Lisboa" },
    });
    await waitFor(() => expect(result.current.data?.city).toBe("Lisboa"));

    rerender({ cityName: "Recife" });

    await waitFor(() => expect(result.current.data?.city).toBe("Recife"));
  });
});
