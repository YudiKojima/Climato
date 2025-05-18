import React, { useEffect, useState } from "react";
import { getAllFavorites } from "../services/api";
import CityCard from "../components/CityCard";
import { useNavigate } from "react-router-dom";
import { Typography, Button, Row, Col, Space, Empty } from "antd";
import { ArrowLeftOutlined } from "@ant-design/icons";
import Loading from "../components/Loading";

const { Title } = Typography;

const Favorites = () => {
  const [favorites, setFavorites] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const navigate = useNavigate();

  const fetchFavorites = async () => {
    try {
      const data = await getAllFavorites();
      setFavorites(data);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchFavorites();
  }, []);

  const handleCardClick = (cityName) => {
    navigate(`/weather/${encodeURIComponent(cityName)}`);
  };

  if (isLoading && favorites.length == 0) {
    return (
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          height: "100vh",
          width: "100vw",
        }}
      >
        <Loading fontSize={120} />
      </div>
    );
  }

  return (
    <div style={{ padding: "2rem", maxWidth: 1000, margin: "0 auto" }}>
      <Space direction="vertical" style={{ width: "100%" }} size="large">
        <Title level={2} style={{ textAlign: "center" }}>
          ⭐ Cidades Favoritas
        </Title>

        <Button
          icon={<ArrowLeftOutlined />}
          onClick={() => navigate("/")}
          type="default"
        >
          Voltar
        </Button>

        {favorites.length > 0 ? (
          <Row gutter={[16, 16]}>
            {favorites.map((city) => (
              <Col xs={24} sm={12} md={8} lg={6} key={city.id}>
                <CityCard
                  city={city}
                  onToggleFavorite={fetchFavorites}
                  onClick={() => handleCardClick(city.name)}
                />
              </Col>
            ))}
          </Row>
        ) : (
          <Empty description="Nenhuma cidade favoritada ainda." />
        )}
      </Space>
    </div>
  );
};

export default Favorites;
