import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { GetUser } from "../services/api";
import img from "../assets/Climato sem fundo preto.png";
import "../styles/Containers.css";
import { Button, Input, Form, Typography, message } from "antd";

const { Title } = Typography;

const SignIn = () => {
  const [form] = Form.useForm();
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (values) => {
    try {
      setIsLoading(true);
      const user = await GetUser(values);
      localStorage.setItem("userId", user.id);
      navigate("/p");
    } catch (err) {
      alert("E-mail ou Senha incorreta.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const userId = localStorage.getItem("userId");
    if (userId) navigate("/p");
  }, [navigate]);

  return (
    <div className="auth-page">
      <div className="auth-container">
        <img
          src={img}
          alt="Climato Logo"
          style={{
            maxWidth: "180px",
            display: "block",
            margin: "0 auto 24px",
          }}
        />
        <Title level={3} style={{ textAlign: "center" }}>
          Acessar conta
        </Title>

        <Form
          validateTrigger="onBlur"
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          className="auth-form"
        >
          <Form.Item
            label="Email"
            name="email"
            rules={[
              { required: true, message: "Digite seu e-mail" },
              { type: "email", message: "Digite um e-mail válido" },
            ]}
          >
            <Input style={{ padding: "8px" }} placeholder="Digite seu e-mail" />
          </Form.Item>

          <Form.Item
            label="Senha"
            name="password"
            rules={[{ required: true, message: "Digite sua senha" }]}
          >
            <Input.Password
              style={{ padding: "8px" }}
              placeholder="Digite sua senha"
            />
          </Form.Item>

          <Form.Item>
            <Button
              style={{ marginTop: "1.5rem" }}
              type="primary"
              htmlType="submit"
              block
              loading={isLoading}
            >
              Entrar
            </Button>
          </Form.Item>
        </Form>

        <div style={{ textAlign: "center", marginTop: "0.5rem" }}>
          <span>Não tem uma conta? </span>
          <a
            onClick={() => navigate("/signup")}
            style={{
              color: "#1677ff",
              textDecoration: "underline",
              cursor: "pointer",
              fontWeight: "bold",
            }}
          >
            Criar conta
          </a>
        </div>
      </div>
    </div>
  );
};

export default SignIn;
