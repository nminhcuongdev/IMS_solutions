import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";


export type ReturnStatusKey =
  | "statusPendingReturn" | "statusPartialReturn" | "statusReturned" | "statusCancelled";

export type ReturnStatusAction = "done" | "notDone" | "cancelled";

export interface ReturnPlanRow {
  returnNo: string;
  serialNo: string;
  returnedByCode: string | null;
  returnedByName: string | null;
  itemCode: string;
  itemName: string | null;
  expectedQty: number;
  unit: string | null;
  actualQty: number;
  returnDate: string | null;
  invoiceNo: string | null;
  status: ReturnStatusKey;
  shortageReason: string | null;
  lotNo: string | null;
  labelIssued: boolean;
  createdAt: string;
  createdBy: string | null;
}

export interface ReturnPlanWrite {
  returnNo: string;
  serialNo?: string | null;
  returnedByCode?: string | null;
  itemCode: string;
  expectedQty: number;
  returnDate?: string | null;
  invoiceNo?: string | null;
  factoryCode?: string | null;
  lotNo?: string | null;
}

export interface ReturnStatusChangeResult {
  updated: number;
  skipped: string[];
}

export interface ReturnScheduleQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  returnNo?: string;
  status?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

type QueryRecord = Record<string, string | number | boolean | undefined | null>;

export const listReturnsSchedule = (
  q: ReturnScheduleQuery = {},
): Promise<ApiListSuccess<ReturnPlanRow>> =>
  apiList<ReturnPlanRow>("/returns-schedule", { query: q as QueryRecord });

export const createReturnPlan = (body: ReturnPlanWrite) =>
  apiCall<ReturnPlanRow>("/returns-schedule", { method: "POST", body });

export const changeReturnStatus = (
  lines: { returnNo: string; serialNo: string }[], status: ReturnStatusAction,
) =>
  apiCall<ReturnStatusChangeResult>("/returns-schedule/status", {
    method: "PUT", body: { lines, status },
  });

export const registerReturnShortage = (
  returnNo: string, shortageQty: number, reasonCode: string, serialNo?: string | null,
) =>
  apiCall<ReturnPlanRow>(`/returns-schedule/${encodeURIComponent(returnNo)}/shortage`, {
    method: "POST", body: { shortageQty, reasonCode, serialNo },
  });

function qsOf(q: Record<string, unknown>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(q)) {
    if (v === undefined || v === null || v === "") continue;
    p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : "";
}

export const exportReturnsScheduleCsv = (q: ReturnScheduleQuery = {}) =>
  apiDownload("/returns-schedule/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));
