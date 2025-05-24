import React, { useEffect, useState } from "react";
import { getAllFavorites } from "../services/api";
import CityCard from "../components/CityCard";
import { Link, useNavigate } from "react-router-dom";
import { Typography, Button, Row, Col, Space, Empty } from "antd";
import { ArrowLeftOutlined } from "@ant-design/icons";
import Loading from "../components/Loading";
import img from "../assets/Climato sem fundo preto.png";
import Footer from "../components/Footer";

const { Title } = Typography;

const Favorites = () => {
  const [favorites, setFavorites] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
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
    navigate(`/p/weather/${encodeURIComponent(cityName)}`);
  };

  const handleSignOut = () => {
    localStorage.removeItem("userId");
    navigate("/signin");
  };

  if (isLoading && favorites.length === 0) {
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
    <div
      style={{ display: "flex", flexDirection: "column", minHeight: "100vh" }}
    >
      <div
        style={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          padding: "0rem 2rem",
          backgroundColor: "#fff",
        }}
      >
        <Link to="/p">
          <img
            src={img}
            alt="Climato Logo"
            style={{ height: "80px", cursor: "pointer" }}
          />
        </Link>
        <div>
          <Button
            type="primary"
            icon={<ArrowLeftOutlined />}
            style={{ marginRight: 8 }}
            onClick={() => navigate("/p")}
          >
            Voltar para cidades
          </Button>
          <Button onClick={handleSignOut}>Sair</Button>
        </div>
      </div>

      <div style={{ padding: "2rem", width: 1000, margin: "0 auto", flex: 1 }}>
        <Space direction="vertical" style={{ width: "100%" }} size="large">
          <Title level={2} style={{ textAlign: "center" }}>
            ⭐ Cidades Favoritas
          </Title>

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

      <Footer />
    </div>
  );
};

export default Favorites;
