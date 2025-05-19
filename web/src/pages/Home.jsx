import React, { useEffect, useState } from "react";
import { getAllCities } from "../services/api";
import CityCard from "../components/CityCard";
import { useNavigate } from "react-router-dom";
import { Input, Button, Typography, Row, Col, Space } from "antd";
import { StarOutlined } from "@ant-design/icons";
import Loading from "../components/Loading";
import img from "../assets/Climato sem fundo preto.png";

const { Title } = Typography;
const { Search } = Input;

const Home = () => {
  const [cities, setCities] = useState([]);
  const [search, setSearch] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [notFound, setNotFound] = useState(false);
  const navigate = useNavigate();

  const fetchCities = async () => {
    try {
      const data = await getAllCities();

      if (data) {
        setCities(data);
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

  useEffect(() => {
    fetchCities();
  }, []);

  const handleSearch = () => {
    if (search.trim() !== "") {
      navigate(`/weather/${encodeURIComponent(search)}`);
    }
  };

  const handleCardClick = (city) => {
    navigate(`/weather/${encodeURIComponent(city.name)}`);
  };

  if (isLoading) {
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
    <div style={{ padding: "1rem", maxWidth: 1000, margin: "0 auto" }}>
      <Space direction="vertical" style={{ width: "100%" }} size="large">
        <img
          src={img}
          alt="Climato Logo"
          style={{
            display: "block",
            margin: "0 auto",
            maxWidth: "200px",
          }}
        />

        <Button
          type="primary"
          icon={<StarOutlined />}
          onClick={() => navigate("/favorites")}
        >
          Ver Favoritos
        </Button>

        <Search
          style={{
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            maxWidth: "100%",
          }}
          placeholder="Buscar cidade..."
          enterButton="Buscar"
          size="large"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onSearch={handleSearch}
        />

        <Row gutter={[16, 16]}>
          {cities.map((city) => (
            <Col xs={24} sm={12} md={8} lg={6} key={city.id}>
              <CityCard
                city={city}
                onToggleFavorite={fetchCities}
                onClick={() => handleCardClick(city)}
                onDelete={fetchCities}
              />
            </Col>
          ))}
        </Row>
      </Space>
    </div>
  );
};

export default Home;
