import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { register, getApiErrorMessage } from "../services/api";
import { hasValidSession, saveSession } from "../services/session";
import img from "../assets/Climato sem fundo preto.png";
import "../styles/Containers.css";
import { Button, Form, Input, Typography, message } from "antd";

const { Title } = Typography;

const SignUp = () => {
  const [form] = Form.useForm();
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (values) => {
    try {
      setIsLoading(true);
      const auth = await register(values);
      saveSession(auth);
      navigate("/p");
    } catch (err) {
      message.error(getApiErrorMessage(err, "Erro ao criar conta."));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (hasValidSession()) navigate("/p");
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
          Criar nova conta
        </Title>

        <Form
          validateTrigger="onBlur"
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          className="auth-form"
        >
          <Form.Item
            label="Nome"
            name="name"
            rules={[{ required: true, message: "Digite seu nome" }]}
          >
            <Input style={{ padding: "8px" }} placeholder="Digite seu nome" />
          </Form.Item>

          <Form.Item
            label="Email"
            name="email"
            rules={[
              { required: true, message: "Digite seu e-mail" },
              { type: "email", message: "E-mail inválido" },
            ]}
          >
            <Input style={{ padding: "8px" }} placeholder="Digite seu e-mail" />
          </Form.Item>

          <Form.Item
            label="Senha"
            name="password"
            rules={[
              { required: true, message: "Digite sua senha" },
              { min: 8, message: "A senha deve ter pelo menos 8 caracteres" },
            ]}
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
              loading={isLoading}
              block
            >
              Criar conta
            </Button>
          </Form.Item>
        </Form>

        <div style={{ textAlign: "center", marginTop: "0.5rem" }}>
          <span>Retornar para </span>
          <a
            onClick={() => navigate("/signin")}
            style={{
              color: "#1677ff",
              textDecoration: "underline",
              cursor: "pointer",
              fontWeight: "bold",
            }}
          >
            Acessar conta
          </a>
        </div>
      </div>
    </div>
  );
};

export default SignUp;
