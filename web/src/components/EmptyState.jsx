import { Empty } from "antd";
import styles from "./EmptyState.module.css";

export default function EmptyState({ description, children }) {
  return (
    <Empty
      className={styles.root}
      image={Empty.PRESENTED_IMAGE_SIMPLE}
      description={description}
    >
      {children}
    </Empty>
  );
}
