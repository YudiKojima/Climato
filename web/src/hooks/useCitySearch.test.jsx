import { act, renderHook, screen, waitFor } from "@testing-library/react";
import { App } from "antd";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { searchLocations } from "../services/locations";
import useCitySearch from "./useCitySearch";

vi.mock("../services/locations", () => ({ searchLocations: vi.fn() }));

const wrapper = ({ children }) => <App>{children}</App>;

describe("useCitySearch", () => {
  beforeEach(() => {
    searchLocations.mockReset();
  });

  it("ignora termo com menos de 2 caracteres", async () => {
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    await act(() => result.current.search(" a "));

    expect(searchLocations).not.toHaveBeenCalled();
    expect(result.current.query).toBe("");
  });

  it("busca o termo sem espaços e devolve os resultados", async () => {
    searchLocations.mockResolvedValue([{ name: "Tóquio", state: null, country: "JP" }]);
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    await act(() => result.current.search("  Tóquio "));

    expect(searchLocations).toHaveBeenCalledWith("Tóquio");
    expect(result.current.query).toBe("Tóquio");
    expect(result.current.results).toHaveLength(1);
    expect(result.current.isSearching).toBe(false);
  });

  it("erro limpa os resultados e mostra a mensagem", async () => {
    searchLocations.mockRejectedValue({
      response: { status: 400, data: { title: "Parâmetro 'query' inválido." }, headers: {} },
    });
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    await act(() => result.current.search("Tóquio"));

    expect(result.current.results).toEqual([]);
    expect(await screen.findByText("Parâmetro 'query' inválido.")).toBeInTheDocument();
  });

  it("descarta a resposta de uma busca anterior", async () => {
    let resolveOld;
    searchLocations
      .mockImplementationOnce(() => new Promise((resolve) => { resolveOld = resolve; }))
      .mockResolvedValueOnce([{ name: "Recife", state: "Pernambuco", country: "BR" }]);
    const { result } = renderHook(() => useCitySearch(), { wrapper });

    act(() => { result.current.search("Lisboa"); });
    await act(() => result.current.search("Recife"));
    await act(async () => resolveOld([{ name: "Lisboa", state: null, country: "PT" }]));

    await waitFor(() => expect(result.current.query).toBe("Recife"));
    expect(result.current.results[0].name).toBe("Recife");
  });

  it("clear volta ao estado inicial", async () => {
    searchLocations.mockResolvedValue([{ name: "Tóquio", state: null, country: "JP" }]);
    const { result } = renderHook(() => useCitySearch(), { wrapper });
    await act(() => result.current.search("Tóquio"));

    act(() => result.current.clear());

    expect(result.current.query).toBe("");
    expect(result.current.results).toEqual([]);
  });
});
