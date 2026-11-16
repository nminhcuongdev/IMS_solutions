import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";


export type ShippingStatusKey =
  | "statusNotShipped" | "statusPartialShip" | "statusShipped" | "statusCancelled";

export type ShippingStatusAction = "done" | "notDone" | "cancelled";

export interface ShippingPlanRow {
  slipNo: string;
  serialNo: string;
  customerCode: string | null;
  customerName: string | null;
  itemCode: string;
  itemName: string | null;
  plannedQty: number;
  unit: string | null;
  actualQty: number;
  packingTypeCode: string | null;
  packingQty: number | null;
  scheduledDate: string | null;
  gateCode: string | null;
  invoiceNo: string | null;
  status: ShippingStatusKey;
  shortageReason: string | null;
  labelIssued: boolean;
  createdAt: string;
  createdBy: string | null;
}

export interface ShippingPlanWrite {
  slipNo: string;
  serialNo?: string | null;
  customerCode?: string | null;
  itemCode: string;
  plannedQty: number;
  scheduledDate?: string | null;
  factoryCode?: string | null;
  gateCode?: string | null;
  invoiceNo?: string | null;
}

export interface ShippingResultRow {
  no: number;
  createdAt: string;
  itemCode: string;
  itemName: string | null;
  qty: number;
  unit: string | null;
  slipNo: string | null;
  serialNo: string | null;
  customerCode: string | null;
  customerName: string | null;
  locationCode: string | null;
  lotNo: string | null;
  expiryDate: string | null;
  scheduledDate: string | null;
  invoiceNo: string | null;
  worker: string | null;
  status: ShippingStatusKey;
  shortageReason: string | null;
}

export interface ShippingStatusChangeResult {
  updated: number;
  skipped: string[];
}

export interface ShippingScheduleQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  slipNo?: string;
  status?: string;
  q?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export interface ShippingResultQuery {
  dateFrom?: string;
  dateTo?: string;
  itemCode?: string;
  itemName?: string;
  slipNo?: string;
  slipNoPresence?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

type QueryRecord = Record<string, string | number | boolean | undefined | null>;

export const listShippingSchedule = (
  q: ShippingScheduleQuery = {},
): Promise<ApiListSuccess<ShippingPlanRow>> =>
  apiList<ShippingPlanRow>("/schedules/shipping", { query: q as QueryRecord });

export const createShippingPlan = (body: ShippingPlanWrite) =>
  apiCall<ShippingPlanRow>("/schedules/shipping", { method: "POST", body });

export const changeShippingStatus = (
  lines: { slipNo: string; serialNo: string }[], status: ShippingStatusAction,
) =>
  apiCall<ShippingStatusChangeResult>("/schedules/shipping/status", {
    method: "PUT", body: { lines, status },
  });

export const registerShippingShortage = (
  slipNo: string, shortageQty: number, reasonCode: string, serialNo?: string | null,
) =>
  apiCall<ShippingPlanRow>(`/schedules/shipping/${encodeURIComponent(slipNo)}/shortage`, {
    method: "POST", body: { shortageQty, reasonCode, serialNo },
  });

export const listShippingResults = (
  q: ShippingResultQuery = {},
): Promise<ApiListSuccess<ShippingResultRow>> =>
  apiList<ShippingResultRow>("/results/shipping", { query: q as QueryRecord });

function qsOf(q: Record<string, unknown>): string {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(q)) {
    if (v === undefined || v === null || v === "") continue;
    p.set(k, String(v));
  }
  const s = p.toString();
  return s ? `?${s}` : "";
}

export const exportShippingResultCsv = (q: ShippingResultQuery = {}) =>
  apiDownload("/export/shipping-result" + qsOf({ ...q, page: undefined, pageSize: undefined }));

export const exportShippingScheduleCsv = (q: ShippingScheduleQuery = {}) =>
  apiDownload("/export/shipping-schedule" + qsOf({ ...q, page: undefined, pageSize: undefined }));
