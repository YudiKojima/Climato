import { useState } from "react";
import { App, Button, Card, Tooltip, Typography } from "antd";
import { DeleteOutlined, StarFilled, StarOutlined } from "@ant-design/icons";
import useApiFeedback from "../hooks/useApiFeedback";
import { deleteCity, toggleFavorite } from "../services/cities";
import styles from "./CityCard.module.css";

export default function CityCard({ city, onChange, onOpen }) {
  const { message, modal } = App.useApp();
  const notifyError = useApiFeedback();
  const [isSaving, setIsSaving] = useState(false);

  const handleFavorite = async (e) => {
    e.stopPropagation();
    setIsSaving(true);
    try {
      await toggleFavorite(city.id);
      message.success(
        city.isFavorite ? "Removida dos favoritos." : "Adicionada aos favoritos."
      );
      onChange();
    } catch (err) {
      notifyError(err, "Não foi possível atualizar a favorita.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = (e) => {
    e.stopPropagation();
    modal.confirm({
      title: `Remover ${city.name} do histórico?`,
      okText: "Remover",
      okButtonProps: { danger: true },
      cancelText: "Cancelar",
      onOk: async () => {
        try {
          await deleteCity(city.id);
          onChange();
        } catch (err) {
          notifyError(err, "Não foi possível remover a cidade.");
        }
      },
    });
  };

  return (
    <Card hoverable className={styles.card} onClick={() => onOpen(city)}>
      <div className={styles.header}>
        <div>
          <Typography.Title level={4} className={styles.title}>
            {city.name}
          </Typography.Title>
          <Typography.Text type="secondary">{city.country}</Typography.Text>
        </div>
        {!city.isFavorite && (
          <Tooltip title="Remover do histórico">
            <Button
              type="text"
              size="small"
              aria-label={`Remover ${city.name} do histórico`}
              icon={<DeleteOutlined />}
              onClick={handleDelete}
            />
          </Tooltip>
        )}
      </div>
      <Button
        className={styles.favorite}
        size="small"
        type={city.isFavorite ? "default" : "primary"}
        icon={city.isFavorite ? <StarFilled /> : <StarOutlined />}
        loading={isSaving}
        onClick={handleFavorite}
      >
        {city.isFavorite ? "Favorita" : "Favoritar"}
      </Button>
    </Card>
  );
}
