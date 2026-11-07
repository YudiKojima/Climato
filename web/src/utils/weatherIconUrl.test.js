import { describe, expect, it } from "vitest";
import { weatherIconUrl } from "./weatherIconUrl";

describe("weatherIconUrl", () => {
  it("monta a URL do ícone da OpenWeather em 2x", () => {
    expect(weatherIconUrl("01d")).toBe("https://openweathermap.org/img/wn/01d@2x.png");
  });
});
