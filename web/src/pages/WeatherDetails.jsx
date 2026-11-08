import { lazy, Suspense, useEffect } from "react";
import { Button, Card, Col, Row, Skeleton, Space, Statistic, Typography } from "antd";
import {
  ArrowDownOutlined,
  ArrowLeftOutlined,
  ArrowUpOutlined,
  CloudOutlined,
  CompassOutlined,
  DashboardOutlined,
  FireOutlined,
  MoonOutlined,
  SendOutlined,
  SunOutlined,
} from "@ant-design/icons";
import { useNavigate, useParams } from "react-router-dom";
import EmptyState from "../components/EmptyState";
import ErrorBoundary from "../components/ErrorBoundary";
import ErrorState from "../components/ErrorState";
import useApiFeedback from "../hooks/useApiFeedback";
import useWeather from "../hooks/useWeather";
import { addCity } from "../services/cities";
import { formatTime } from "../utils/formatTime";
import { weatherIconUrl } from "../utils/weatherIconUrl";
import styles from "./WeatherDetails.module.css";

const ForecastSection = lazy(() => import("../components/ForecastSection"));

const METRIC_COLS = { xs: 12, md: 8 };

const toMetrics = (weather) => [
  { label: "Mínima", value: `${weather.tempMin}°C`, icon: <ArrowDownOutlined /> },
  { label: "Máxima", value: `${weather.tempMax}°C`, icon: <ArrowUpOutlined /> },
  { label: "Sensação", value: `${weather.feelsLike}°C`, icon: <FireOutlined /> },
  { label: "Umidade", value: `${weather.humidity}%`, icon: <CloudOutlined /> },
  { label: "Pressão", value: `${weather.pressure} hPa`, icon: <DashboardOutlined /> },
  { label: "Vento", value: `${weather.windSpeedKmh} km/h`, icon: <SendOutlined /> },
  { label: "Direção do vento", value: `${weather.windDirection}°`, icon: <CompassOutlined /> },
  { label: "Nascer do sol", value: formatTime(weather.sunrise), icon: <SunOutlined /> },
  { label: "Pôr do sol", value: formatTime(weather.sunset), icon: <MoonOutlined /> },
];

function WeatherDetailsSkeleton() {
  return (
    <div className={styles.page}>
      <Card className={styles.heroSkeleton}>
        <Skeleton active paragraph={{ rows: 4 }} />
      </Card>
      <Row gutter={[16, 16]}>
        {Array.from({ length: 6 }, (_, index) => (
          <Col key={index} {...METRIC_COLS}>
            <Card size="small">
              <Skeleton active title={false} paragraph={{ rows: 2 }} />
            </Card>
          </Col>
        ))}
      </Row>
    </div>
  );
}

export default function WeatherDetails() {
  const { cityName } = useParams();
  const navigate = useNavigate();
  const { data: weather, isLoading, error, reload } = useWeather(cityName);
  const notifyError = useApiFeedback();

  useEffect(() => {
    if (!weather) {
      return;
    }

    addCity(weather.city).catch((err) =>
      notifyError(err, "Não foi possível salvar a cidade no histórico.")
    );
  }, [weather, notifyError]);

  const backButton = (
    <Button icon={<ArrowLeftOutlined />} onClick={() => navigate("/p")}>
      Voltar
    </Button>
  );

  if (isLoading) {
    return <WeatherDetailsSkeleton />;
  }

  if (error?.response?.status === 404) {
    return (
      <EmptyState description={`Cidade "${cityName}" não encontrada.`}>
        {backButton}
      </EmptyState>
    );
  }

  if (error) {
    return (
      <ErrorState
        error={error}
        fallback="Não foi possível carregar o clima agora."
        onRetry={reload}
      />
    );
  }

  return (
    <div className={styles.page}>
      <section
        className={styles.hero}
        style={weather.imageUrl ? { backgroundImage: `url("${weather.imageUrl}")` } : undefined}
      >
        <div className={styles.overlay}>
          <div>{backButton}</div>
          <div>
            <div className={styles.summary}>
              <img
                src={weatherIconUrl(weather.icon)}
                alt=""
                className={styles.icon}
              />
              <div>
                <Typography.Title level={1} className={styles.temperature}>
                  {weather.temperature}°C
                </Typography.Title>
                <Typography.Text className={styles.description}>
                  {weather.description}
                </Typography.Text>
              </div>
            </div>
            <Typography.Title level={2} className={styles.city}>
              {weather.city}, {weather.country}
            </Typography.Title>
          </div>
        </div>
      </section>

      <Row gutter={[16, 16]}>
        {toMetrics(weather).map((metric) => (
          <Col key={metric.label} {...METRIC_COLS}>
            <Card size="small" className={styles.metric}>
              <Statistic
                title={
                  <Space size="small">
                    {metric.icon}
                    {metric.label}
                  </Space>
                }
                value={metric.value}
              />
            </Card>
          </Col>
        ))}
      </Row>

      <ErrorBoundary
        fallback={<ErrorState error={null} fallback="Não foi possível carregar a previsão agora." />}
      >
        <Suspense
          fallback={
            <Card>
              <Skeleton active paragraph={{ rows: 6 }} />
            </Card>
          }
        >
          <ForecastSection cityName={cityName} />
        </Suspense>
      </ErrorBoundary>
    </div>
  );
}
