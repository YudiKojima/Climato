import client from "./client";

export const login = async (data) => {
  const response = await client.post("/auth/login", data);
  return response.data;
};

export const register = async (data) => {
  const response = await client.post("/auth/register", data);
  return response.data;
};

export const loginAsDemo = async () => {
  const response = await client.post("/auth/demo");
  return response.data;
};
