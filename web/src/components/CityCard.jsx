import React from "react";
import { Card, Button, Typography, message } from "antd";
import { StarFilled, StarOutlined, CloseOutlined } from "@ant-design/icons";
import { toggleFavorite, DeleteCity } from "../services/api";

const { Title, Text } = Typography;

const CityCard = ({ city, onToggleFavorite, onClick, onDelete }) => {
  const handleFavoriteClick = async (e) => {
    e.stopPropagation();
    await toggleFavorite(city.id);
    onToggleFavorite();
  };

  const handleDeleteClick = async (e) => {
    e.stopPropagation();

    if (city.isFavorite) {
      alert("Não é possível remover uma cidade favorita.");
      return;
    }

    await DeleteCity(city.id);
    onDelete();
  };

  return (
    <Card
      hoverable
      onClick={onClick}
      styles={{
        body: { padding: "1rem" },
      }}
      style={{ width: "100%", borderRadius: 12 }}
    >
      <Button
        type="text"
        icon={<CloseOutlined />}
        onClick={handleDeleteClick}
        style={{
          position: "absolute",
          top: 8,
          right: 8,
        }}
      />

      <Title level={4}>
        {city.name} - {city.country}
      </Title>
      <Text type="secondary">
        Favorita: {city.isFavorite ? "⭐ Sim" : "☆ Não"}
      </Text>

      <div style={{ marginTop: 20 }}>
        <Button
          icon={city.isFavorite ? <StarFilled /> : <StarOutlined />}
          onClick={handleFavoriteClick}
          type={city.isFavorite ? "default" : "primary"}
          size="small"
        >
          {city.isFavorite
            ? "Remover dos favoritos"
            : "Adicionar aos favoritos"}
        </Button>
      </div>
    </Card>
  );
};

export default CityCard;
