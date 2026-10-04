import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export const REASON_CATEGORIES = ["Shortage", "Defect", "Adjustment", "Hold"] as const;
export type ReasonCategory = typeof REASON_CATEGORIES[number];

export interface Reason {
  reasonCode: string; reasonName: string; reasonCategory: ReasonCategory; isActive: boolean;
  remark1: string | null; remark2: string | null; remark3: string | null;
  remark4: string | null; remark5: string | null;
  createdAt: string; updatedAt: string | null;
}
export interface ReasonWrite {
  reasonCode: string; reasonName: string; reasonCategory: ReasonCategory;
  isActive?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}

const P = "/masters/reasons";
export const listReasons = (q: { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean; category?: ReasonCategory } = {}): Promise<ApiListSuccess<Reason>> =>
  apiList<Reason>(P, { query: q as any });
export const getReason = (code: string) => apiCall<Reason>(`${P}/${encodeURIComponent(code)}`);
export const createReason = (body: ReasonWrite) => apiCall<Reason>(P, { method: "POST", body });
export const updateReason = (code: string, body: ReasonWrite) =>
  apiCall<Reason>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteReason = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportReasonsCsv = () => apiDownload(`${P}/export.csv`);
