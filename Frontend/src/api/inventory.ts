import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";


export interface StockRow {
  itemCode: string;
  serialNo: string;
  itemName: string | null;
  category: string | null;
  warehouse: string | null;
  locationCode: string | null;
  locationName: string | null;
  lotNo: string | null;
  isOnHold: boolean;
  holdReason: string | null;
  stockQty: number;
  reservedQty: number;
  availableQty: number;
  unit: string | null;
  reorderPoint: number | null;
  surplusPoint: number | null;
  overShort: number | null;
  updatedAt: string;
  expiryDate: string | null;
  remainingDays: number | null;
}

export interface StockGroupRow {
  key: string;
  name: string | null;
  category: string | null;
  warehouse: string | null;
  unit: string | null;
  stockQty: number;
  lotCount: number;
  itemCount: number;
  anyOnHold: boolean;
  earliestExpiry: string | null;
  remainingDays: number | null;
  reorderPoint: number | null;
}

export interface InventoryHistoryRow {
  no: number;
  createdAt: string;
  itemCode: string;
  itemName: string | null;
  lotNo: string | null;
  actionType: string;
  actionLabelKey: string;
  qty: number | null;
  unit: string | null;
  locationCode: string | null;
  reasonCode: string | null;
  reasonName: string | null;
  worker: string | null;
}

export interface StockKey { itemCode: string; serialNo: string }

export interface StockBulkResult {
  updated: number;
  skipped: string[];
}

export type AggAxis = "lot" | "item" | "location";

export interface StockQuery {
  warehouse?: string;
  itemCode?: string;
  itemName?: string;
  category?: string;
  lot?: string;
  expiration?: string;
  stockLevel?: string;
  hold?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface HistoryQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  action?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

type QueryRecord = Record<string, string | number | boolean | undefined | null>;

export const listStock = (q: StockQuery = {}): Promise<ApiListSuccess<StockRow>> =>
  apiList<StockRow>("/stock", { query: q as QueryRecord });

export const listStockGrouped = (
  axis: "item" | "location", q: StockQuery = {},
): Promise<ApiListSuccess<StockGroupRow>> =>
  apiList<StockGroupRow>("/stock", { query: { ...q, groupBy: axis } as QueryRecord });

export const adjustStock = (body: {
  itemCode: string; serialNo: string; stockQty: number; reasonCode: string;
}) => apiCall<StockRow>("/stock/adjust", { method: "POST", body });

export const holdStock = (keys: StockKey[], reasonCode: string) =>
  apiCall<StockBulkResult>("/stock/hold", { method: "POST", body: { keys, reasonCode } });

export const releaseHold = (keys: StockKey[]) =>
  apiCall<StockBulkResult>("/stock/release-hold", { method: "POST", body: { keys } });

export const disposeStock = (body: {
  itemCode: string; serialNo: string; reasonCode: string;
}) => apiCall<StockRow>("/stock/disposal", { method: "POST", body });

export const listInventoryHistory = (
  q: HistoryQuery = {},
): Promise<ApiListSuccess<InventoryHistoryRow>> =>
  apiList<InventoryHistoryRow>("/inventory-history", { query: q as QueryRecord });

export const restoreDisposal = (nos: number[]) =>
  apiCall<StockBulkResult>("/inventory-history/restore", { method: "POST", body: { nos } });

function qsOf(q: Record<string, unknown>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(q)) {
    if (v === undefined || v === null || v === "") continue;
    p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : "";
}

export const exportStockCsv = (q: StockQuery = {}) =>
  apiDownload("/stock/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));

export const exportInventoryHistoryCsv = (q: HistoryQuery = {}) =>
  apiDownload("/inventory-history/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));
