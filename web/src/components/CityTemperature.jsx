import { Skeleton, Tooltip, Typography } from "antd";
import useWeather from "../hooks/useWeather";
import { weatherIconUrl } from "../utils/weatherIconUrl";
import styles from "./CityTemperature.module.css";

export default function CityTemperature({ cityName }) {
  const { data, isLoading, error } = useWeather(cityName);

  if (isLoading) {
    return <Skeleton.Button active size="small" />;
  }

  if (error) {
    return (
      <Tooltip title="Clima indisponível">
        <Typography.Text type="secondary" aria-label="Clima indisponível">
          —
        </Typography.Text>
      </Tooltip>
    );
  }

  return (
    <div className={styles.root}>
      <img src={weatherIconUrl(data.icon)} alt={data.description} className={styles.icon} />
      <Typography.Text strong className={styles.value}>
        {Math.round(data.temperature)}°C
      </Typography.Text>
    </div>
  );
}
