import { apiCall, apiDownload, apiList } from "./client";


export const ST_IN_PROGRESS = 1;
export const ST_COMPLETED = 2;
export const ST_FINALIZED = 21;

export const LBL_NOT_STARTED = 0;
export const LBL_NORMAL = 14;
export const LBL_MISSING = 22;
export const LBL_OVER = 23;

export interface StocktakingSession {
  no: number;
  sessionName: string;
  status: number;
  statusLabelKey: string;
  startedAt: string | null;
  completedAt: string | null;
  acceptsResults: boolean;
  labelCount: number;
  scannedCount: number;
}

export interface StocktakingLabelRow {
  serialNo: string;
  itemCode: string;
  itemName: string | null;
  qty: number;
  unit: string | null;
  locationCode: string;
  lotNo: string | null;
  isScanned: boolean;
  status: number;
  statusLabelKey: string;
  expiryDate: string | null;
  isExpired: boolean;
  updatedAt: string | null;
}

export interface StocktakingGroupRow {
  itemCode: string;
  itemName: string | null;
  locationText: string;
  lotNo: string | null;
  plannedLabels: number;
  scannedLabels: number;
  plannedQty: number;
  resultQty: number;
  unit: string | null;
  updatedAt: string | null;
}

export interface StocktakingHistoryRow {
  itemCode: string;
  itemName: string | null;
  lotNo: string;
  stocktakingName: string;
  stocktakingDate: string;
  plannedQty: number;
  resultQty: number;
  missingQty: number;
  unit: string | null;
  badgeKey: string;
}

export interface LabelQuery {
  view?: "model" | "label";
  locationCode?: string;
  itemCode?: string;
  itemName?: string;
  notScanned?: boolean;
  statusDiff?: boolean;
  page?: number;
  pageSize?: number;
  sort?: string;
}

type QueryRecord = Record<string, string | number | boolean | undefined | null>;

export const getStocktakingSession = () =>
  apiCall<StocktakingSession | null>("/stocktaking/session");

export const startStocktaking = (sessionName: string) =>
  apiCall<StocktakingSession>("/stocktaking/session/start", {
    method: "POST", body: { sessionName },
  });

export const completeStocktaking = () =>
  apiCall<StocktakingSession>("/stocktaking/session/complete", { method: "POST" });

export const finalizeStocktaking = (serials: string[]) =>
  apiCall<StocktakingSession>("/stocktaking/session/finalize", {
    method: "POST", body: { serials },
  });

export const listStocktakingLabels = (q: LabelQuery = {}) =>
  apiList<StocktakingLabelRow>("/stocktaking/labels", { query: { ...q, view: "label" } as QueryRecord });

export const listStocktakingGroups = (q: LabelQuery = {}) =>
  apiList<StocktakingGroupRow>("/stocktaking/labels", { query: { ...q, view: "model" } as QueryRecord });

export const listStocktakingHistory = (q: { q?: string; page?: number; pageSize?: number; sort?: string } = {}) =>
  apiList<StocktakingHistoryRow>("/stocktaking/history", { query: q as QueryRecord });

function qsOf(q: Record<string, unknown>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(q)) {
    if (v === undefined || v === null || v === "" || v === false) continue;
    p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : "";
}

export const exportStocktakingLabelsCsv = (q: LabelQuery = {}) =>
  apiDownload("/stocktaking/labels/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));

export const exportStocktakingHistoryCsv = (q: { q?: string } = {}) =>
  apiDownload("/stocktaking/history/export.csv" + qsOf(q));
