import { apiCall } from "./client";

export interface RoleRow {
  roleCode: string;
  roleName: string;
  isBuiltin: boolean;
  userCount: number;
}

export interface RoleWrite {
  roleCode?: string;
  roleName: string;
}

const P = "/masters/roles";
export const listRoles = () => apiCall<RoleRow[]>(P);
export const createRole = (body: RoleWrite) => apiCall<RoleRow>(P, { method: "POST", body });
export const updateRole = (code: string, body: RoleWrite) =>
  apiCall<RoleRow>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteRole = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
