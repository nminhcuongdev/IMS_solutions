import { apiCall } from "./client";

export interface PermissionRow {
  permissionCode: string;
  largeCode: string;
  largeName: string;
  functionCode: string;
  functionName: string;
  action: string;
  general: boolean;
  worker: boolean;
}

export interface PermissionItem {
  roleCode: "General" | "Worker";
  permissionCode: string;
  isAllowed: boolean;
}

export const listPermissions = () => apiCall<PermissionRow[]>("/permissions");

export const savePermissions = (items: PermissionItem[]) =>
  apiCall<PermissionRow[]>("/permissions", { method: "PUT", body: { items } });
