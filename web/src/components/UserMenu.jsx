import { Avatar, Button, Dropdown } from "antd";
import { HomeOutlined, LogoutOutlined, StarOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import { useAuth } from "../app/AuthContext";
import styles from "./UserMenu.module.css";

const navigationItems = [
  { key: "/p", label: "Início", icon: <HomeOutlined /> },
  { key: "/p/favorites", label: "Favoritos", icon: <StarOutlined /> },
  { type: "divider" },
];

export default function UserMenu({ showName, showNavigation }) {
  const { userName, signOut } = useAuth();
  const navigate = useNavigate();

  const items = [
    ...(showNavigation ? navigationItems : []),
    { key: "signout", label: "Sair", icon: <LogoutOutlined />, danger: true },
  ];

  const handleClick = ({ key }) => {
    if (key === "signout") {
      signOut();
      navigate("/signin", { replace: true });
      return;
    }

    navigate(key);
  };

  return (
    <Dropdown
      menu={{ items, onClick: handleClick }}
      trigger={["click"]}
      placement="bottomRight"
    >
      <Button type="text" className={styles.trigger} aria-label="Menu do usuário">
        <Avatar size="small" className={styles.avatar}>
          {userName.charAt(0).toUpperCase()}
        </Avatar>
        {showName && <span>{userName}</span>}
      </Button>
    </Dropdown>
  );
}
