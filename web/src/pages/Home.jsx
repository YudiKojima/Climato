import React, { useEffect, useState } from "react";
import { getAllCities } from "../services/api";
import CityCard from "../components/CityCard";
import { Link, useNavigate } from "react-router-dom";
import { Input, Button, Row, Col, Typography, Divider } from "antd";
import { StarOutlined } from "@ant-design/icons";
import Loading from "../components/Loading";
import img from "../assets/Climato sem fundo preto.png";
import Footer from "../components/Footer";

const { Search } = Input;
const { Title } = Typography;

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
      navigate(`/p/weather/${encodeURIComponent(search)}`);
    }
  };

  const handleCardClick = (city) => {
    navigate(`/p/weather/${encodeURIComponent(city.name)}`);
  };

  const handleSignOut = () => {
    localStorage.removeItem("userId");
    navigate("/signin");
  };

  if (isLoading) {
    return (
      <div
        style={{
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          height: "100vh",
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
            icon={<StarOutlined />}
            style={{ marginRight: 8 }}
            onClick={() => navigate("/p/favorites")}
          >
            Ver Favoritos
          </Button>
          <Button onClick={handleSignOut}>Sair</Button>
        </div>
      </div>

      <div
        style={{
          width: 1000,
          margin: "3rem auto",
          padding: "0 1rem",
          flex: 1,
        }}
      >
        <Title level={3} style={{ textAlign: "center" }}>
          Pesquise agora o clima da sua cidade!
        </Title>

        <Search
          placeholder="Buscar cidade..."
          enterButton="Buscar"
          size="large"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onSearch={handleSearch}
          style={{ marginBottom: "2rem" }}
        />

        <Divider orientation="left">Cidades pesquisadas</Divider>
        {notFound ? (
          <p style={{ textAlign: "center", color: "#999" }}>
            Nenhuma cidade encontrada.
          </p>
        ) : (
          <Row gutter={[24, 24]}>
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
        )}
      </div>

      <Footer />
    </div>
  );
};

export default Home;
