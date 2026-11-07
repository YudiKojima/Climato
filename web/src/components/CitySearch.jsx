import { useState } from "react";
import { Card, Input, Skeleton, Typography } from "antd";
import { EnvironmentOutlined } from "@ant-design/icons";
import { useNavigate } from "react-router-dom";
import useCitySearch from "../hooks/useCitySearch";
import EmptyState from "./EmptyState";
import styles from "./CitySearch.module.css";

const LIST_ID = "city-search-results";
const optionId = (index) => `${LIST_ID}-${index}`;

export default function CitySearch() {
  const navigate = useNavigate();
  const { query, results, isSearching, search, clear } = useCitySearch();
  const [activeIndex, setActiveIndex] = useState(0);

  const open = (location) => navigate(`/p/weather/${encodeURIComponent(location.name)}`);

  const handleSearch = (value) => {
    if (results.length > 0 && value.trim() === query) {
      open(results[activeIndex]);
      return;
    }

    setActiveIndex(0);
    search(value);
  };

  const handleKeyDown = (e) => {
    if (e.key === "Escape") {
      clear();
      return;
    }

    if (results.length === 0) {
      return;
    }

    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((index) => (index + 1) % results.length);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((index) => (index - 1 + results.length) % results.length);
    }
  };

  const handleChange = (e) => {
    if (query && e.target.value.trim() !== query) {
      clear();
    }
  };

  return (
    <div className={styles.root}>
      <Input.Search
        placeholder="Buscar cidade..."
        enterButton="Buscar"
        size="large"
        allowClear
        loading={isSearching}
        role="combobox"
        aria-autocomplete="list"
        aria-expanded={results.length > 0}
        aria-controls={results.length > 0 ? LIST_ID : undefined}
        aria-activedescendant={results.length > 0 ? optionId(activeIndex) : undefined}
        onSearch={handleSearch}
        onKeyDown={handleKeyDown}
        onChange={handleChange}
      />

      {isSearching && (
        <Card className={styles.panel}>
          <Skeleton active title={false} paragraph={{ rows: 3 }} />
        </Card>
      )}

      {!isSearching && query && results.length === 0 && (
        <Card className={styles.panel}>
          <EmptyState description={`Nenhuma cidade encontrada para "${query}".`} />
        </Card>
      )}

      {!isSearching && results.length > 0 && (
        <Card className={styles.panel} size="small">
          <ul id={LIST_ID} role="listbox" aria-label="Cidades encontradas" className={styles.list}>
            {results.map((location, index) => (
              <li
                key={optionId(index)}
                id={optionId(index)}
                role="option"
                aria-selected={index === activeIndex}
                className={index === activeIndex ? `${styles.option} ${styles.active}` : styles.option}
                onMouseEnter={() => setActiveIndex(index)}
                onClick={() => open(location)}
              >
                <EnvironmentOutlined className={styles.icon} />
                <span className={styles.name}>{location.name}</span>
                <Typography.Text type="secondary">
                  {[location.state, location.country].filter(Boolean).join(", ")}
                </Typography.Text>
              </li>
            ))}
          </ul>
        </Card>
      )}
    </div>
  );
}
