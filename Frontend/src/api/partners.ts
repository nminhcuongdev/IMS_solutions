import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface Partner {
  partnerCode: string; partnerName: string;
  isSupplier: boolean; isCustomer: boolean;
  country: string | null; address: string | null;
  isActive: boolean;
  remark1: string | null; remark2: string | null; remark3: string | null;
  remark4: string | null; remark5: string | null;
  createdAt: string; updatedAt: string | null;
}
export interface PartnerWrite {
  partnerCode: string; partnerName: string;
  isSupplier?: boolean; isCustomer?: boolean;
  country?: string | null; address?: string | null;
  isActive?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}

const P = "/masters/partners";
export const listPartners = (q: { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean; isSupplier?: boolean; isCustomer?: boolean } = {}): Promise<ApiListSuccess<Partner>> =>
  apiList<Partner>(P, { query: q as any });
export const getPartner = (code: string) => apiCall<Partner>(`${P}/${encodeURIComponent(code)}`);
export const createPartner = (body: PartnerWrite) => apiCall<Partner>(P, { method: "POST", body });
export const updatePartner = (code: string, body: PartnerWrite) =>
  apiCall<Partner>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deletePartner = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportPartnersCsv = () => apiDownload(`${P}/export.csv`);
