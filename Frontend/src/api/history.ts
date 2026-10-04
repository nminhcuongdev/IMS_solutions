import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface PrintHistoryRow {
  no: number;
  workedDate: string;
  category: string;
  docNo: string | null;
  scheduledDate: string | null;
  itemCode: string | null;
  itemName: string | null;
  qty: number | null;
  uomCode: string | null;
  packageCount: number;
  spec: string | null;
  status: number;
  statusName: string | null;
  createdBy: string | null;
  workerName: string | null;
}

export interface RfidGateHistoryRow {
  no: number;
  workedDate: string;
  itemCode: string | null;
  itemName: string | null;
  gateCode: string;
  serialNo: string | null;
  lotNo: string | null;
  directionType: string | null;
  status: number;
}

export interface PrintHistoryQuery {
  dateFrom?: string;
  dateTo?: string;
  category?: string;
  status?: number;
  item?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface RfidHistoryQuery {
  dateFrom?: string;
  dateTo?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

type QueryRecord = Record<string, string | number | boolean | undefined | null>;

export const listPrintHistory = (q: PrintHistoryQuery = {}): Promise<ApiListSuccess<PrintHistoryRow>> =>
  apiList<PrintHistoryRow>("/print-history", { query: q as QueryRecord });

export const listRfidHistory = (q: RfidHistoryQuery = {}): Promise<ApiListSuccess<RfidGateHistoryRow>> =>
  apiList<RfidGateHistoryRow>("/rfid-gate-history", { query: q as QueryRecord });

function qs(q: Record<string, unknown>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(q)) {
    if (v === undefined || v === null || v === "") continue;
    p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : "";
}

export const exportPrintHistoryCsv = (q: PrintHistoryQuery = {}) =>
  apiDownload("/print-history/export.csv" + qs({ ...q, page: undefined, pageSize: undefined }));

export const exportRfidHistoryCsv = (q: RfidHistoryQuery = {}) =>
  apiDownload("/rfid-gate-history/export.csv" + qs({ ...q, page: undefined, pageSize: undefined }));

export interface PrintHistoryWrite {
  category: string;
  docNo?: string | null;
  itemCode?: string | null;
  qty?: number | null;
  uomCode?: string | null;
  packageCount?: number;
  spec?: string | null;
  scheduledDate?: string | null;
  reprint?: boolean;
}

export const createPrintHistory = (body: PrintHistoryWrite) =>
  apiCall<PrintHistoryRow>("/print-history", { method: "POST", body });

export const reprintPrintHistory = (
  no: number, body: { deviceCode: string; count: number; fields: Record<string, string> },
) => apiCall<PrintHistoryRow>(`/print-history/${no}/reprint`, { method: "POST", body });
