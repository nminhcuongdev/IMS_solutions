import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface AppUser {
  userCode: string; userName: string; roleCode: string; roleName: string | null;
  factoryCode: string | null;
  defaultLanguage: string | null;
  isActive: boolean;
  remark1: string | null; remark2: string | null; remark3: string | null;
  remark4: string | null; remark5: string | null;
  createdAt: string; updatedAt: string | null;
}
export interface UserWrite {
  userCode: string; userName: string; roleCode: string;
  password?: string | null;
  factoryCode?: string | null;
  defaultLanguage?: string | null;
  isActive?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}

const P = "/masters/users";
export const listUsers = (q: { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean; roleCode?: string } = {}): Promise<ApiListSuccess<AppUser>> =>
  apiList<AppUser>(P, { query: q as any });
export const getUser = (code: string) => apiCall<AppUser>(`${P}/${encodeURIComponent(code)}`);
export const createUser = (body: UserWrite) => apiCall<AppUser>(P, { method: "POST", body });
export const updateUser = (code: string, body: UserWrite) =>
  apiCall<AppUser>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteUser = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportUsersCsv = () => apiDownload("/export/users");
