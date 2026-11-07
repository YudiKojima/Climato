const DEFAULT_MESSAGE = "Não foi possível concluir a operação.";

export const getApiErrorMessage = (error, fallback = DEFAULT_MESSAGE) => {
  const data = error?.response?.data;
  const firstValidationError = data?.errors
    ? Object.values(data.errors).flat()[0]
    : undefined;

  return firstValidationError || data?.detail || data?.title || fallback;
};

export const getCorrelationId = (error) =>
  error?.response?.headers?.["x-correlation-id"];

export const isUnexpectedError = (error) =>
  !error?.response || error.response.status >= 500;
