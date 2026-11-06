import { act, renderHook, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import useAsync from "./useAsync";

describe("useAsync", () => {
  it("começa carregando e devolve os dados", async () => {
    const fn = vi.fn().mockResolvedValue(["Tóquio"]);
    const { result } = renderHook(() => useAsync(fn));

    expect(result.current.isLoading).toBe(true);

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.data).toEqual(["Tóquio"]);
    expect(result.current.error).toBeNull();
  });

  it("devolve o erro sem dados", async () => {
    const error = new Error("falhou");
    const fn = vi.fn().mockRejectedValue(error);
    const { result } = renderHook(() => useAsync(fn));

    await waitFor(() => expect(result.current.isLoading).toBe(false));
    expect(result.current.error).toBe(error);
    expect(result.current.data).toBeUndefined();
  });

  it("reload chama a função de novo e mantém os dados durante a carga", async () => {
    const fn = vi.fn().mockResolvedValueOnce("primeira").mockResolvedValueOnce("segunda");
    const { result } = renderHook(() => useAsync(fn));
    await waitFor(() => expect(result.current.data).toBe("primeira"));

    act(() => result.current.reload());

    expect(result.current.data).toBe("primeira");
    await waitFor(() => expect(result.current.data).toBe("segunda"));
    expect(fn).toHaveBeenCalledTimes(2);
  });

  it("descarta a resposta da função anterior", async () => {
    let resolveOld;
    const oldFn = vi.fn(() => new Promise((resolve) => { resolveOld = resolve; }));
    const newFn = vi.fn().mockResolvedValue("nova");
    const { result, rerender } = renderHook(({ fn }) => useAsync(fn), {
      initialProps: { fn: oldFn },
    });

    rerender({ fn: newFn });
    await waitFor(() => expect(result.current.data).toBe("nova"));
    await act(async () => resolveOld("antiga"));

    expect(result.current.data).toBe("nova");
  });
});
