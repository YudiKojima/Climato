import { Typography } from "antd";
import CityGrid from "../components/CityGrid";
import useFavorites from "../hooks/useFavorites";

export default function Favorites() {
  const { data, isLoading, error, reload } = useFavorites();

  return (
    <section>
      <Typography.Title level={2}>Cidades favoritas</Typography.Title>
      <CityGrid
        cities={data}
        isLoading={isLoading}
        error={error}
        reload={reload}
        emptyDescription="Nenhuma cidade favoritada ainda."
      />
    </section>
  );
}
