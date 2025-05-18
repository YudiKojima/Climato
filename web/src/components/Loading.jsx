import React from "react";
import { Spin } from "antd";
import { LoadingOutlined } from "@ant-design/icons";

const Loading = ({ className, fontSize = 24 }) => {
  return (
    <Spin
      className={className}
      indicator={<LoadingOutlined style={{ fontSize }} />}
    />
  );
};

export default Loading;
