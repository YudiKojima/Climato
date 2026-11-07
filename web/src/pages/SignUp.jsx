import { useState } from "react";
import { Button, Form, Input, Typography } from "antd";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../app/AuthContext";
import useApiFeedback from "../hooks/useApiFeedback";
import { register } from "../services/auth";
import styles from "./Auth.module.css";

export default function SignUp() {
  const navigate = useNavigate();
  const { signIn } = useAuth();
  const notifyError = useApiFeedback();
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (values) => {
    setIsLoading(true);
    try {
      signIn(await register(values));
      navigate("/p", { replace: true });
    } catch (err) {
      notifyError(err, "Erro ao criar conta.");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <div>
        <Typography.Title level={3}>Criar conta</Typography.Title>
        <Typography.Text type="secondary">
          Salve suas cidades e acompanhe o clima de cada uma.
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
          label="Nome"
          name="name"
          rules={[{ required: true, message: "Digite seu nome" }]}
        >
          <Input autoComplete="name" placeholder="Digite seu nome" />
        </Form.Item>

        <Form.Item
          label="E-mail"
          name="email"
          rules={[
            { required: true, message: "Digite seu e-mail" },
            { type: "email", message: "E-mail inválido" },
          ]}
        >
          <Input autoComplete="email" placeholder="voce@exemplo.com" />
        </Form.Item>

        <Form.Item
          label="Senha"
          name="password"
          rules={[
            { required: true, message: "Digite sua senha" },
            { min: 8, message: "A senha deve ter pelo menos 8 caracteres" },
          ]}
        >
          <Input.Password autoComplete="new-password" placeholder="Mínimo de 8 caracteres" />
        </Form.Item>

        <Button type="primary" htmlType="submit" block loading={isLoading}>
          Criar conta
        </Button>
      </Form>

      <Typography.Text className={styles.switch}>
        Já tem uma conta? <Link to="/signin">Acessar conta</Link>
      </Typography.Text>
    </>
  );
}
