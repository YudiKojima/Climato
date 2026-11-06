import { Grid, Layout } from "antd";
import { Link, NavLink, Outlet } from "react-router-dom";
import ThemeToggle from "../components/ThemeToggle";
import UserMenu from "../components/UserMenu";
import { useThemeMode } from "../theme/ThemeContext";
import logoLight from "../assets/Climato sem fundo preto.png";
import logoDark from "../assets/Climato sem fundo.png";
import styles from "./AppLayout.module.css";

const navLinkClass = ({ isActive }) =>
  isActive ? `${styles.navLink} ${styles.active}` : styles.navLink;

export default function AppLayout() {
  const screens = Grid.useBreakpoint();
  const { mode } = useThemeMode();
  const isWide = Boolean(screens.md);

  return (
    <Layout className={styles.root}>
      <Layout.Header className={styles.header}>
        <Link to="/p" className={styles.brand}>
          <img
            src={mode === "dark" ? logoDark : logoLight}
            alt="Climato"
            className={styles.logo}
          />
        </Link>
        <nav className={styles.actions}>
          {isWide && (
            <>
              <NavLink to="/p" end className={navLinkClass}>
                Início
              </NavLink>
              <NavLink to="/p/favorites" className={navLinkClass}>
                Favoritos
              </NavLink>
            </>
          )}
          <ThemeToggle />
          <UserMenu showName={isWide} showNavigation={!isWide} />
        </nav>
      </Layout.Header>
      <Layout.Content className={styles.content}>
        <div className={styles.container}>
          <Outlet />
        </div>
      </Layout.Content>
      <Layout.Footer className={styles.footer}>
        Climato © {new Date().getFullYear()}
      </Layout.Footer>
    </Layout>
  );
}
