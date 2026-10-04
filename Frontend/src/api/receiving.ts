import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";


export type ReceivingStatusKey =
  | "statusNotArrived" | "statusPartial" | "statusArrived" | "statusCancelled";

export type ReceivingStatusAction = "done" | "notDone" | "cancelled";

export interface ReceivingPlanRow {
  poNo: string;
  serialNo: string;
  supplierCode: string | null;
  supplierName: string | null;
  itemCode: string;
  itemName: string | null;
  plannedQty: number;
  unit: string | null;
  actualQty: number;
  packingTypeCode: string | null;
  packingQty: number | null;
  scheduledDate: string | null;
  invoiceNo: string | null;
  status: ReceivingStatusKey;
  shortageReason: string | null;
  lotNo: string | null;
  locationCode: string | null;
  labelIssued: boolean;
  createdAt: string;
  createdBy: string | null;
}

export interface ReceivingResultRow {
  no: number;
  createdAt: string;
  itemCode: string;
  itemName: string | null;
  qty: number;
  unit: string | null;
  poNo: string | null;
  serialNo: string | null;
  invoiceNo: string | null;
  locationCode: string | null;
  worker: string | null;
  status: ReceivingStatusKey;
  shortageReason: string | null;
  expiryDate: string | null;
  lotNo: string | null;
  supplierCode: string | null;
  supplierName: string | null;
  scheduledDate: string | null;
}

export interface ReceivingPlanWrite {
  poNo: string;
  serialNo?: string | null;
  supplierCode?: string | null;
  itemCode: string;
  plannedQty: number;
  scheduledDate?: string | null;
  invoiceNo?: string | null;
  factoryCode?: string | null;
}

export interface ReceivingStatusChangeResult {
  updated: number;
  skipped: string[];
}

export interface ReceivingScheduleQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  poNo?: string;
  status?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface ReceivingResultQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  poNo?: string;
  poNoPresence?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

type QueryRecord = Record<string, string | number | boolean | undefined | null>;

export const listReceivingSchedule = (
  q: ReceivingScheduleQuery = {},
): Promise<ApiListSuccess<ReceivingPlanRow>> =>
  apiList<ReceivingPlanRow>("/receiving-schedule", { query: q as QueryRecord });

export const createReceivingPlan = (body: ReceivingPlanWrite) =>
  apiCall<ReceivingPlanRow>("/receiving-schedule", { method: "POST", body });

export const changeReceivingStatus = (
  lines: { poNo: string; serialNo: string }[], status: ReceivingStatusAction,
) =>
  apiCall<ReceivingStatusChangeResult>("/receiving-schedule/status", {
    method: "PUT", body: { lines, status },
  });

export const registerReceivingShortage = (
  poNo: string, shortageQty: number, reasonCode: string, serialNo?: string | null,
) =>
  apiCall<ReceivingPlanRow>(`/receiving-schedule/${encodeURIComponent(poNo)}/shortage`, {
    method: "POST", body: { shortageQty, reasonCode, serialNo },
  });

export const listReceivingResult = (
  q: ReceivingResultQuery = {},
): Promise<ApiListSuccess<ReceivingResultRow>> =>
  apiList<ReceivingResultRow>("/receiving-result", { query: q as QueryRecord });

function qsOf(q: Record<string, unknown>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(q)) {
    if (v === undefined || v === null || v === "") continue;
    p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : "";
}

export const exportReceivingScheduleCsv = (q: ReceivingScheduleQuery = {}) =>
  apiDownload("/receiving-schedule/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));

export const exportReceivingResultCsv = (q: ReceivingResultQuery = {}) =>
  apiDownload("/receiving-result/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));
