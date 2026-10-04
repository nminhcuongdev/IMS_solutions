import { apiCall } from "./client";

export interface LoginResponse {
  token: string;
  expiresIn: number;
  tokenType: string;
  userCode: string;
  userName: string;
  roleCode: string;
  isSuperAdmin: boolean;
}

export const login = (userId: string, password: string) =>
  apiCall<LoginResponse>("/auth/login", { method: "POST", body: { userId, password }, skipAuth: true });

export const logout = () => apiCall<void>("/auth/logout", { method: "POST" });
