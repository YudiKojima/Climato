import { Button, Result } from "antd";
import {
  getApiErrorMessage,
  getCorrelationId,
  isUnexpectedError,
} from "../services/errors";
import CorrelationId from "./CorrelationId";

export default function ErrorState({ error, fallback, onRetry }) {
  const correlationId = isUnexpectedError(error) ? getCorrelationId(error) : undefined;

  return (
    <Result
      status="warning"
      title={getApiErrorMessage(error, fallback)}
      subTitle={correlationId && <CorrelationId id={correlationId} />}
      extra={
        onRetry && (
          <Button type="primary" onClick={onRetry}>
            Tentar de novo
          </Button>
        )
      }
    />
  );
}
