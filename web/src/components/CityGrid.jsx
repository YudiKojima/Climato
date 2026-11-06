import { Col, Row } from "antd";
import CityCard from "./CityCard";
import CityGridSkeleton from "./CityGridSkeleton";
import EmptyState from "./EmptyState";
import ErrorState from "./ErrorState";

export default function CityGrid({
  cities,
  isLoading,
  error,
  reload,
  emptyDescription,
  onOpen,
}) {
  if (error) {
    return (
      <ErrorState
        error={error}
        fallback="Não foi possível carregar as cidades."
        onRetry={reload}
      />
    );
  }

  if (isLoading && !cities) {
    return <CityGridSkeleton />;
  }

  if (cities.length === 0) {
    return <EmptyState description={emptyDescription} />;
  }

  return (
    <Row gutter={[16, 16]}>
      {cities.map((city) => (
        <Col key={city.id} xs={24} sm={12} md={8} lg={6}>
          <CityCard city={city} onChange={reload} onOpen={onOpen} />
        </Col>
      ))}
    </Row>
  );
}
