import { apiCall } from "./client";

export interface MenuFunction {
  functionCode: string;
  functionName: string;
  displayOrder: number | null;
}

export interface MenuGroup {
  largeCode: string;
  largeName: string;
  displayOrder: number | null;
  functions: MenuFunction[];
}

export interface MenuResponse {
  userCode: string;
  roleCode: string;
  isSuperAdmin: boolean;
  groups: MenuGroup[];
}

export const fetchMenu = () => apiCall<MenuResponse>("/menu");
