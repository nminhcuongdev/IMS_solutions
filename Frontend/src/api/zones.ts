import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface Zone {
  zoneCode: string;
  zoneName: string;
  factoryCode: string;
  isActive: boolean;
  remark1: string | null;
  remark2: string | null;
  remark3: string | null;
  remark4: string | null;
  remark5: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface ZoneWrite {
  zoneCode: string;
  zoneName: string;
  factoryCode: string;
  isActive?: boolean;
  remark1?: string | null;
  remark2?: string | null;
  remark3?: string | null;
  remark4?: string | null;
  remark5?: string | null;
}

export interface ZoneListQuery {
  page?: number;
  pageSize?: number;
  q?: string;
  sort?: string;
  isActive?: boolean;
}

export const listZones = (q: ZoneListQuery = {}): Promise<ApiListSuccess<Zone>> =>
  apiList<Zone>("/masters/zones", { query: q as Record<string, string | number | boolean | undefined> });

export const getZone = (code: string) => apiCall<Zone>(`/masters/zones/${encodeURIComponent(code)}`);

export const createZone = (body: ZoneWrite) =>
  apiCall<Zone>("/masters/zones", { method: "POST", body });

export const updateZone = (code: string, body: ZoneWrite) =>
  apiCall<Zone>(`/masters/zones/${encodeURIComponent(code)}`, { method: "PUT", body });

export const deleteZone = (code: string) =>
  apiCall<void>(`/masters/zones/${encodeURIComponent(code)}`, { method: "DELETE" });

export const exportZonesCsv = () => apiDownload("/masters/zones/export.csv");
