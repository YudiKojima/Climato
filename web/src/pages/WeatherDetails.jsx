import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { getWeatherByCity } from "../services/api";
import { Button, Empty, Typography } from "antd";
import "../styles/WeatherDetails.css";
import Loading from "../components/Loading";
import formatDatetime from "../components/formatDatetime";

const WeatherDetails = () => {
  const { cityName } = useParams();
  const [weather, setWeather] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchWeather = async () => {
      try {
        const data = await getWeatherByCity(cityName);

        if (data) {
          setWeather(data);
          setNotFound(false);
        } else {
          setNotFound(true);
        }
      } catch (error) {
        setNotFound(true);
      } finally {
        setIsLoading(false);
      }
    };

    fetchWeather();
  }, [cityName]);

  if (isLoading) {
    return (
      <div className="weather-details-card">
        <Loading fontSize={120} />
      </div>
    );
  }

  return (
    <div
      className="weather-details-background"
      style={{
        backgroundImage: `url(${weather?.imageUrl})`,
      }}
    >
      <div className="weather-details-card">
        {notFound ? (
          <>
            <Empty
              image="https://gw.alipayobjects.com/zos/antfincdn/ZHrcdLPrvN/empty.svg"
              styles={{ image: { height: 140 } }}
              description={
                <Typography.Text>
                  Cidade "<strong>{cityName}</strong>" não encontrada.
                </Typography.Text>
              }
            ></Empty>
          </>
        ) : (
          <>
            <h2>{`Clima em ${weather.city} - ${weather.country}`}</h2>

            <div className="weather-main-info">
              <img
                src={`https://openweathermap.org/img/wn/${weather.icon}@2x.png`}
                alt={weather.description}
              />
              <p className="temp">{weather.temperature}°C</p>
              <p className="desc">{weather.description}</p>
            </div>

            <div className="weather-grid">
              <div>
                <strong>Mín:</strong> {weather.tempMin}°C
              </div>
              <div>
                <strong>Máx:</strong> {weather.tempMax}°C
              </div>
              <div>
                <strong>Sensação:</strong> {weather.feelsLike}°C
              </div>
              <div>
                <strong>Umidade:</strong> {weather.humidity}%
              </div>
              <div>
                <strong>Pressão:</strong> {weather.pressure} hPa
              </div>
              <div>
                <strong>Vento:</strong> {weather.windSpeed} km/h
              </div>
              <div>
                <strong>Direção:</strong> {weather.windDirection}°
              </div>
              <div>
                <strong>Nascer do Sol:</strong>{" "}
                {formatDatetime(weather.sunrise)}
              </div>
              <div>
                <strong>Pôr do Sol:</strong> {formatDatetime(weather.sunset)}
              </div>
            </div>
          </>
        )}
        <Button
          style={{ marginTop: 20 }}
          type="primary"
          onClick={() => navigate("/p")}
        >
          Voltar
        </Button>
      </div>
    </div>
  );
};

export default WeatherDetails;
