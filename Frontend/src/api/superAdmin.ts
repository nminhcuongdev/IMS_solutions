import { apiCall } from "./client";

export interface GeneralItem {
  configCode: string;
  configValue: string | null;
  category: string | null;
}

export interface VisibilityRow {
  targetType: string;
  targetCode: string;
  functionName: string;
  largeCode: string | null;
  largeName: string | null;
  isVisible: boolean;
}

export interface VisibilityItem {
  targetType: string;
  targetCode: string;
  isVisible: boolean;
}

export interface DefaultMasterRow {
  code: string;
  nameLang1: string;
  nameLang2: string;
  nameLang3: string;
}

export interface DbInfo {
  server: string;
  userName: string;
  table: string;
}

export type DefaultMasterType = "status" | "packing-type" | "hold-name";

export const getGeneral = () => apiCall<GeneralItem[]>("/super-admin/general");

export const saveGeneral = (items: GeneralItem[]) =>
  apiCall<GeneralItem[]>("/super-admin/general", { method: "PUT", body: { items } });

export const getVisibility = () => apiCall<VisibilityRow[]>("/super-admin/function-visibility");

export const saveVisibility = (items: VisibilityItem[]) =>
  apiCall<VisibilityRow[]>("/super-admin/function-visibility", { method: "PUT", body: { items } });

export const getDefaultMaster = (type: DefaultMasterType) =>
  apiCall<DefaultMasterRow[]>(`/super-admin/default-master/${type}`);

export const saveDefaultMaster = (type: DefaultMasterType, items: DefaultMasterRow[]) =>
  apiCall<DefaultMasterRow[]>(`/super-admin/default-master/${type}`, { method: "PUT", body: { items } });

export const getDbInfo = () => apiCall<DbInfo>("/super-admin/db-info");
