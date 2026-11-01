import { apiCall, apiDownload, apiList, clearToken, getToken } from "./client";
import { ApiCallError, type ApiError, type ApiListSuccess, type ApiSuccess } from "./types";
import type { ImportResult } from "./imports";
import type { ProductionOrderRow } from "./productionTypes/ProductionOrderRow";
import type { ProductionLineRow } from "./productionTypes/ProductionLineRow";
import type { ProductionWork } from "./productionTypes/ProductionWork";
import type { ProductionFg } from "./productionTypes/ProductionFg";
import type { ProductionDetail } from "./productionTypes/ProductionDetail";
import type { ProductionWorks } from "./productionTypes/ProductionWorks";
import type { ProductionQuery } from "./productionTypes/ProductionQuery";

export type { ProductionOrderRow, ProductionLineRow, ProductionWork, ProductionFg, ProductionDetail, ProductionWorks, ProductionQuery };

export const listProductionOrders = (q: ProductionQuery) =>
  apiList<ProductionOrderRow | ProductionLineRow>("/production-order", { query: q as Record<string, string | number | boolean | undefined> });
export const getProductionOrder = (no: string) => apiCall<ProductionDetail>(`/production-order/${encodeURIComponent(no)}`);
export const getProductionLines = (no: string) => apiCall<ProductionLineRow[]>(`/production-order/${encodeURIComponent(no)}/lines`);
export const getProductionWorks = (no: string) => apiCall<ProductionWorks>(`/production-order/${encodeURIComponent(no)}/works`);
export const setProductionStatus = (orderNos: string[], status: string) =>
  apiCall<unknown>("/production-order/status", { method: "PUT", body: { orderNos, status } });
export const setProductionPicking = (items: { orderNo: string; lineNo?: number }[], picked: boolean) =>
  apiCall<unknown>("/production-order/picking", { method: "PUT", body: { items, picked } });
export const listProductionResults = (q: ProductionQuery): Promise<ApiListSuccess<ProductionFg | ProductionWork>> =>
  apiList<ProductionFg | ProductionWork>("/production-result", { query: q as Record<string, string | number | boolean | undefined> });
export const reverseProduction = (kind: "consumption" | "output" | "return", no: number, reasonCode: string) =>
  apiCall<unknown>(`/production-${kind}/${no}/reverse`, { method: "POST", body: { reasonCode } });
export const exportProductionOrders = () => apiDownload("/export/production-order");
export const exportProductionResults = () => apiDownload("/export/production-result");

export async function importProductionOrders(file: File): Promise<ImportResult & { lineCount?: number }> {
  const explicit = (import.meta.env.VITE_API_BASE as string | undefined)?.trim();
  const base = explicit?.replace(/\/+$/, "") ?? `${window.location.protocol}//${window.location.hostname}:${(import.meta.env.VITE_API_PORT as string | undefined)?.trim() || "8091"}`;
  const body = new FormData(); body.append("file", file);
  const token = getToken();
  const res = await fetch(`${base}/import/production-order`, { method: "POST", headers: token ? { Authorization: `Bearer ${token}` } : {}, body });
  if (res.status === 401) clearToken();
  const raw = await res.text();
  const json = raw ? JSON.parse(raw) : {};
  if (!res.ok || json.success === false) throw new ApiCallError((json as ApiError).error ?? {
    code: `HTTP-${res.status}`, httpStatus: res.status, severity: "Error", message: raw || res.statusText,
  });
  return (json as ApiSuccess<ImportResult & { lineCount?: number }>).data;
}
