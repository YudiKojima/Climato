import { Typography } from "antd";
import CityGrid from "../components/CityGrid";
import CitySearch from "../components/CitySearch";
import useCities from "../hooks/useCities";
import styles from "./Home.module.css";

export default function Home() {
  const { data, isLoading, error, reload } = useCities();

  return (
    <div className={styles.page}>
      <section className={styles.hero}>
        <Typography.Title level={2}>Como está o tempo hoje?</Typography.Title>
        <Typography.Paragraph type="secondary">
          Pesquise uma cidade e escolha na lista para ver o clima agora.
        </Typography.Paragraph>
        <CitySearch />
      </section>

      <section>
        <Typography.Title level={4}>Cidades pesquisadas</Typography.Title>
        <CityGrid
          cities={data}
          isLoading={isLoading}
          error={error}
          reload={reload}
          emptyDescription="Pesquise uma cidade para começar."
        />
      </section>
    </div>
  );
}
