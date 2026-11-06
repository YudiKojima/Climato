import { describe, expect, it } from "vitest";
import {
  getApiErrorMessage,
  getCorrelationId,
  isUnexpectedError,
} from "./errors";

const apiError = (status, data = {}, headers = {}) => ({
  response: { status, data, headers },
});

describe("getApiErrorMessage", () => {
  it("prefere o primeiro erro de validação", () => {
    const error = apiError(400, {
      title: "Dados inválidos.",
      errors: { Email: ["E-mail inválido."], Password: ["Senha curta."] },
    });

    expect(getApiErrorMessage(error)).toBe("E-mail inválido.");
  });

  it("usa o detail quando não há erro de validação", () => {
    const error = apiError(409, { title: "Conflito", detail: "Cidade favorita." });

    expect(getApiErrorMessage(error)).toBe("Cidade favorita.");
  });

  it("usa o title quando não há detail", () => {
    expect(getApiErrorMessage(apiError(503, { title: "Serviço indisponível." }))).toBe(
      "Serviço indisponível."
    );
  });

  it("usa o fallback quando não há resposta", () => {
    expect(getApiErrorMessage(new Error("Network Error"), "Sem conexão.")).toBe(
      "Sem conexão."
    );
  });

  it("usa o fallback padrão sem fallback informado", () => {
    expect(getApiErrorMessage(apiError(500))).toBe(
      "Não foi possível concluir a operação."
    );
  });
});

describe("getCorrelationId", () => {
  it("lê o header X-Correlation-ID", () => {
    const error = apiError(500, {}, { "x-correlation-id": "4bf92f3577b34da6" });

    expect(getCorrelationId(error)).toBe("4bf92f3577b34da6");
  });

  it("é undefined sem o header", () => {
    expect(getCorrelationId(apiError(500))).toBeUndefined();
  });

  it("é undefined sem resposta", () => {
    expect(getCorrelationId(new Error("Network Error"))).toBeUndefined();
  });
});

describe("isUnexpectedError", () => {
  it.each([
    ["erro de rede", new Error("Network Error"), true],
    ["500", apiError(500), true],
    ["503", apiError(503), true],
    ["404", apiError(404), false],
    ["400", apiError(400), false],
  ])("%s → %s", (_, error, expected) => {
    expect(isUnexpectedError(error)).toBe(expected);
  });
});
