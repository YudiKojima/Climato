import { Card, Col, Row, Skeleton } from "antd";

export default function CityGridSkeleton({ count = 4 }) {
  return (
    <Row gutter={[16, 16]}>
      {Array.from({ length: count }, (_, index) => (
        <Col key={index} xs={24} sm={12} md={8} lg={6}>
          <Card>
            <Skeleton active paragraph={{ rows: 2 }} />
          </Card>
        </Col>
      ))}
    </Row>
  );
}
