import { Input, Typography } from "antd";
import { useNavigate } from "react-router-dom";
import CityGrid from "../components/CityGrid";
import useCities from "../hooks/useCities";
import styles from "./Home.module.css";

export default function Home() {
  const navigate = useNavigate();
  const { data, isLoading, error, reload } = useCities();

  const openCity = (name) => navigate(`/p/weather/${encodeURIComponent(name)}`);

  const handleSearch = (value) => {
    const name = value.trim();

    if (name) {
      openCity(name);
    }
  };

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <Typography.Title level={2}>Como está o tempo hoje?</Typography.Title>
        <Typography.Paragraph type="secondary">
          Pesquise uma cidade para ver o clima agora.
        </Typography.Paragraph>
        <Input.Search
          className={styles.search}
          placeholder="Buscar cidade..."
          enterButton="Buscar"
          size="large"
          allowClear
          onSearch={handleSearch}
        />
      </section>

      <section>
        <Typography.Title level={4}>Cidades pesquisadas</Typography.Title>
        <CityGrid
          cities={data}
          isLoading={isLoading}
          error={error}
          reload={reload}
          emptyDescription="Pesquise uma cidade para começar."
          onOpen={(city) => openCity(city.name)}
        />
      </section>
    </div>
  );
}
