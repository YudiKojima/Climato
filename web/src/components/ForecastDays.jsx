import { Card, Typography } from "antd";
import { formatWeekday } from "../utils/formatWeekday";
import { weatherIconUrl } from "../utils/weatherIconUrl";
import styles from "./ForecastDays.module.css";

export default function ForecastDays({ days }) {
  return (
    <ul className={styles.grid} aria-label="Previsão por dia">
      {days.map((day) => (
        <li key={day.date}>
          <Card size="small" className={styles.day}>
            <Typography.Text strong>{formatWeekday(day.date)}</Typography.Text>
            <img src={weatherIconUrl(day.icon)} alt="" className={styles.icon} />
            <Typography.Text type="secondary" className={styles.description}>
              {day.description}
            </Typography.Text>
            <Typography.Text>
              <strong>{Math.round(day.tempMax)}°</strong> / {Math.round(day.tempMin)}°
            </Typography.Text>
            <Typography.Text type="secondary">Chuva {day.precipitationProbability}%</Typography.Text>
          </Card>
        </li>
      ))}
    </ul>
  );
}
