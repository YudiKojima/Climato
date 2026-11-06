import { Col, Layout, Row, Typography } from "antd";
import { Outlet } from "react-router-dom";
import ThemeToggle from "../components/ThemeToggle";
import { useThemeMode } from "../theme/ThemeContext";
import logoLight from "../assets/Climato sem fundo preto.png";
import logoDark from "../assets/Climato sem fundo.png";
import styles from "./AuthLayout.module.css";

export default function AuthLayout() {
  const { mode } = useThemeMode();

  return (
    <Layout className={styles.root}>
      <Row className={styles.row}>
        <Col xs={0} md={12} lg={14} className={styles.aside}>
          <div className={styles.overlay}>
            <img src={logoDark} alt="" className={styles.asideLogo} />
            <Typography.Title level={2} className={styles.tagline}>
              O clima de qualquer cidade, com a cara dela.
            </Typography.Title>
          </div>
        </Col>
        <Col xs={24} md={12} lg={10} className={styles.main}>
          <div className={styles.toggle}>
            <ThemeToggle />
          </div>
          <div className={styles.form}>
            <img
              src={mode === "dark" ? logoDark : logoLight}
              alt="Climato"
              className={styles.logo}
            />
            <Outlet />
          </div>
        </Col>
      </Row>
    </Layout>
  );
}
