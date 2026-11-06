import { Typography } from "antd";

export default function CorrelationId({ id }) {
  return (
    <Typography.Text type="secondary">
      Código do erro:{" "}
      <Typography.Text code copyable>
        {id}
      </Typography.Text>
    </Typography.Text>
  );
}
