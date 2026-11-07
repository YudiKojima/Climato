import { act, renderHook, screen } from "@testing-library/react";
import { App } from "antd";
import { describe, expect, it } from "vitest";
import useApiFeedback from "./useApiFeedback";

const wrapper = ({ children }) => <App>{children}</App>;

const apiError = (status, data) => ({
  response: { status, data, headers: { "x-correlation-id": "4bf92f3577b34da6" } },
});

describe("useApiFeedback", () => {
  it("erro de negócio mostra a mensagem sem o código", async () => {
    const { result } = renderHook(() => useApiFeedback(), { wrapper });

    act(() => result.current(apiError(409, { detail: "Cidade favorita." })));

    expect(await screen.findByText("Cidade favorita.")).toBeInTheDocument();
    expect(screen.queryByText("4bf92f3577b34da6")).not.toBeInTheDocument();
  });

  it("erro 5xx mostra a mensagem e o código do erro", async () => {
    const { result } = renderHook(() => useApiFeedback(), { wrapper });

    act(() => result.current(apiError(503, { title: "Serviço indisponível." })));

    expect(await screen.findByText("Serviço indisponível.")).toBeInTheDocument();
    expect(await screen.findByText("4bf92f3577b34da6")).toBeInTheDocument();
  });

  it("erro de rede usa o fallback", async () => {
    const { result } = renderHook(() => useApiFeedback(), { wrapper });

    act(() => result.current(new Error("Network Error"), "Sem conexão com o servidor."));

    expect(await screen.findByText("Sem conexão com o servidor.")).toBeInTheDocument();
  });
});
