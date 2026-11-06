import { useState } from "react";
import { Button, Form, Input, Typography } from "antd";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../app/AuthContext";
import useApiFeedback from "../hooks/useApiFeedback";
import { login } from "../services/auth";
import styles from "./Auth.module.css";

export default function SignIn() {
  const navigate = useNavigate();
  const { signIn } = useAuth();
  const notifyError = useApiFeedback();
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (values) => {
    setIsLoading(true);
    try {
      signIn(await login(values));
      navigate("/p", { replace: true });
    } catch (err) {
      notifyError(err, "E-mail ou senha incorretos.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <div>
        <Typography.Title level={3}>Acessar conta</Typography.Title>
        <Typography.Text type="secondary">
          Entre para ver o clima das suas cidades.
        </Typography.Text>
      </div>

      <Form
        layout="vertical"
        validateTrigger="onBlur"
        requiredMark={false}
        size="large"
        onFinish={handleSubmit}
      >
        <Form.Item
          label="E-mail"
          name="email"
          rules={[
            { required: true, message: "Digite seu e-mail" },
            { type: "email", message: "Digite um e-mail válido" },
          ]}
        >
          <Input autoComplete="email" placeholder="voce@exemplo.com" />
        </Form.Item>

        <Form.Item
          label="Senha"
          name="password"
          rules={[{ required: true, message: "Digite sua senha" }]}
        >
          <Input.Password autoComplete="current-password" placeholder="Digite sua senha" />
        </Form.Item>

        <Button type="primary" htmlType="submit" block loading={isLoading}>
          Entrar
        </Button>
      </Form>

      <Typography.Text className={styles.switch}>
        Não tem uma conta? <Link to="/signup">Criar conta</Link>
      </Typography.Text>
    </>
  );
}
