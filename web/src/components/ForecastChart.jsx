import { theme } from "antd";
import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { formatWeekday } from "../utils/formatWeekday";
import styles from "./ForecastChart.module.css";

export default function ForecastChart({ days }) {
  const { token } = theme.useToken();

  const data = days.map((day) => ({
    day: formatWeekday(day.date),
    min: Math.round(day.tempMin),
    max: Math.round(day.tempMax),
    rain: day.precipitationProbability,
  }));

  const tick = { fill: token.colorTextSecondary, fontSize: token.fontSizeSM };

  return (
    <div
      className={styles.root}
      role="img"
      aria-label="Gráfico de temperatura mínima, máxima e chance de chuva dos próximos dias. Os valores estão nos cards abaixo."
    >
      <ResponsiveContainer width="100%" height="100%">
        <ComposedChart data={data} margin={{ top: 8, right: 0, bottom: 0, left: -16 }}>
          <CartesianGrid stroke={token.colorBorderSecondary} vertical={false} />
          <XAxis dataKey="day" tick={tick} stroke={token.colorBorder} />
          <YAxis yAxisId="temp" unit="°" tick={tick} stroke={token.colorBorder} />
          <YAxis
            yAxisId="rain"
            orientation="right"
            unit="%"
            domain={[0, 100]}
            tick={tick}
            stroke={token.colorBorder}
          />
          <Tooltip
            contentStyle={{
              background: token.colorBgElevated,
              border: `1px solid ${token.colorBorderSecondary}`,
              borderRadius: token.borderRadius,
            }}
            labelStyle={{ color: token.colorText }}
          />
          <Legend
            formatter={(value) => <span style={{ color: token.colorTextSecondary }}>{value}</span>}
          />
          <Bar
            yAxisId="rain"
            dataKey="rain"
            name="Chance de chuva"
            unit="%"
            fill={token.colorPrimaryBorder}
            radius={[4, 4, 0, 0]}
          />
          <Line
            yAxisId="temp"
            type="monotone"
            dataKey="max"
            name="Máxima"
            unit="°C"
            stroke={token.colorWarning}
            strokeWidth={2}
          />
          <Line
            yAxisId="temp"
            type="monotone"
            dataKey="min"
            name="Mínima"
            unit="°C"
            stroke={token.colorPrimary}
            strokeWidth={2}
          />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
}
