import { apiCall } from "./client";
import type { CurrentPermissions } from "./CurrentPermissions";

export interface PermissionMatrixRow {
  permissionCode: string;
  device: "PC" | "HT";
  largeCode: string;
  largeName: string;
  functionCode: string;
  functionName: string;
  actionType: string;
  isAllowed: boolean;
  locked: boolean;
}

export interface PermissionMatrixItem {
  permissionCode: string;
  isAllowed: boolean;
}

export const getPermissionMatrix = (roleCode: string) =>
  apiCall<PermissionMatrixRow[]>("/permissions", { query: { roleCode } });

export const savePermissionMatrix = (roleCode: string, items: PermissionMatrixItem[]) =>
  apiCall<PermissionMatrixRow[]>("/permissions", { method: "PUT", body: { roleCode, items } });

export const getCurrentPermissions = () => apiCall<CurrentPermissions>("/permissions/me");
