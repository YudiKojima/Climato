import { useCallback } from "react";
import { App } from "antd";
import CorrelationId from "../components/CorrelationId";
import {
  getApiErrorMessage,
  getCorrelationId,
  isUnexpectedError,
} from "../services/errors";

export default function useApiFeedback() {
  const { message, notification } = App.useApp();

  return useCallback(
    (error, fallback) => {
      const text = getApiErrorMessage(error, fallback);

      if (!isUnexpectedError(error)) {
        message.error(text);
        return;
      }

      const correlationId = getCorrelationId(error);
      notification.error({
        message: text,
        description: correlationId && <CorrelationId id={correlationId} />,
      });
    },
    [message, notification]
  );
}
