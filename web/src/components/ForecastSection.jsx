import { Card, Skeleton, Typography } from "antd";
import useForecast from "../hooks/useForecast";
import ErrorState from "./ErrorState";
import ForecastChart from "./ForecastChart";
import ForecastDays from "./ForecastDays";
import styles from "./ForecastSection.module.css";

function ForecastContent({ data, isLoading, error, reload }) {
  if (isLoading) {
    return (
      <Card>
        <Skeleton active paragraph={{ rows: 6 }} />
      </Card>
    );
  }

  if (error) {
    return (
      <ErrorState
        error={error}
        fallback="Não foi possível carregar a previsão agora."
        onRetry={reload}
      />
    );
  }

  return (
    <Card>
      <div className={styles.content}>
        <ForecastChart days={data.days} />
        <ForecastDays days={data.days} />
      </div>
    </Card>
  );
}

export default function ForecastSection({ cityName }) {
  const forecast = useForecast(cityName);

  return (
    <section>
      <Typography.Title level={4}>Próximos 5 dias</Typography.Title>
      <ForecastContent {...forecast} />
    </section>
  );
}
