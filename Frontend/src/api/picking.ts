import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";


export type PickingStatusKey = "statusNotPicked" | "statusPicked" | "statusCancelled";

export type PickingStatusAction = "done" | "notDone" | "cancelled";

export interface PickingPlanRow {
  soNo: string;
  serialNo: string;
  itemCode: string;
  itemName: string | null;
  locationCode: string | null;
  locationName: string | null;
  plannedQty: number;
  unit: string | null;
  pickedQty: number;
  scheduledDate: string | null;
  status: PickingStatusKey;
  createdAt: string;
  createdBy: string | null;
}

export interface PickingPlanWrite {
  soNo: string;
  serialNo?: string | null;
  itemCode: string;
  locationCode?: string | null;
  plannedQty: number;
  scheduledDate?: string | null;
}

export interface PickingResultRow {
  no: number;
  createdAt: string;
  itemCode: string;
  itemName: string | null;
  qty: number;
  unit: string | null;
  soNo: string;
  serialNo: string;
  scheduledDate: string | null;
  locationCode: string;
  locationName: string | null;
  lotNo: string | null;
  worker: string | null;
}

export interface PickingStatusChangeResult {
  updated: number;
  skipped: string[];
}

export interface PickingScheduleQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  soNo?: string;
  status?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface PickingResultQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  soNo?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

type QueryRecord = Record<string, string | number | boolean | undefined | null>;

export const listPickingSchedule = (
  q: PickingScheduleQuery = {},
): Promise<ApiListSuccess<PickingPlanRow>> =>
  apiList<PickingPlanRow>("/picking-schedule", { query: q as QueryRecord });

export const createPickingPlan = (body: PickingPlanWrite) =>
  apiCall<PickingPlanRow>("/picking-schedule", { method: "POST", body });

export const changePickingStatus = (
  lines: { soNo: string; serialNo: string }[], status: PickingStatusAction,
) =>
  apiCall<PickingStatusChangeResult>("/picking-schedule/status", {
    method: "PUT", body: { lines, status },
  });

export const listPickingResults = (
  q: PickingResultQuery = {},
): Promise<ApiListSuccess<PickingResultRow>> =>
  apiList<PickingResultRow>("/picking-result", { query: q as QueryRecord });

function qsOf(q: Record<string, unknown>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(q)) {
    if (v === undefined || v === null || v === "") continue;
    p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : "";
}

export const exportPickingScheduleCsv = (q: PickingScheduleQuery = {}) =>
  apiDownload("/picking-schedule/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));

export const exportPickingResultCsv = (q: PickingResultQuery = {}) =>
  apiDownload("/picking-result/export.csv" + qsOf({ ...q, page: undefined, pageSize: undefined }));
