import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface Factory {
  factoryCode: string; factoryName: string;
  country: string | null; address: string | null;
  isActive: boolean; isDefault: boolean;
  remark1: string | null; remark2: string | null; remark3: string | null;
  remark4: string | null; remark5: string | null;
  createdAt: string; updatedAt: string | null;
}
export interface FactoryWrite {
  factoryCode: string; factoryName: string;
  country?: string | null; address?: string | null;
  isActive?: boolean; isDefault?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}
export interface FactoryListQuery { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean; }

const P = "/masters/factories";
export const listFactories = (q: FactoryListQuery = {}): Promise<ApiListSuccess<Factory>> =>
  apiList<Factory>(P, { query: q as any });
export const getFactory = (code: string) => apiCall<Factory>(`${P}/${encodeURIComponent(code)}`);
export const createFactory = (body: FactoryWrite) => apiCall<Factory>(P, { method: "POST", body });
export const updateFactory = (code: string, body: FactoryWrite) =>
  apiCall<Factory>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteFactory = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportFactoriesCsv = () => apiDownload("/export/factories");
