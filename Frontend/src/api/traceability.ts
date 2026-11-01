import { apiCall, apiDownload, apiList } from "./client";

export type TraceDirection = "backward" | "forward";

export interface TraceLot {
  itemCode: string;
  lotNo: string | null;
  itemName: string | null;
  qty: number;
  poNo: string | null;
  receivedAt: string | null;
  soNo: string | null;
  customer: string | null;
  shippedAt: string | null;
  orderNo?: string | null;
  productionLotNo?: string | null;
  productionDate?: string | null;
}

export interface TraceNode {
  level: number;
  parentNo: number | null;
  type: string;
  itemCode: string;
  lotNo: string | null;
  qty: number;
  documentNo: string | null;
  partnerCode: string | null;
  occurredAt: string | null;
  expiryDate?: string | null;
  expiryCheck?: string | null;
  expiryState?: string | null;
  itemName?: string | null;
  productionLotNo?: string | null;
  processName?: string | null;
  unit?: string | null;
  processCode?: string | null;
  inputQty?: number | null;
  returnedQty?: number | null;
  partnerName?: string | null;
}

export interface TraceBalance {
  itemCode: string;
  lotNo: string | null;
  receivedQty: number;
  producedQty: number;
  consumedQty: number;
  returnedInputQty: number;
  shippedQty: number;
  customerReturnQty: number;
  adjustmentQty: number;
  stockQty: number;
  expectedQty: number;
  diffQty: number;
  ok: boolean;
}

export interface TraceEvent {
  type: string;
  qty: number;
  documentNo: string | null;
  partnerCode: string | null;
  occurredAt: string;
  serialNo: string | null;
}

export interface TraceResult {
  itemCode: string;
  lotNo: string | null;
  nodes: TraceNode[];
  multipleSuppliers: boolean;
  balance: TraceBalance;
}

export type TraceSearch = Record<string, string | number | undefined>;

export const listTraceLots = (query: TraceSearch) => apiList<TraceLot>("/traceability/lots", { query });
export const getTrace = (direction: TraceDirection, itemCode: string, lotNo: string) => apiCall<TraceResult>(`/traceability/${direction}`, { query: { itemCode, lotNo } });
export const getTraceEvents = (itemCode: string, lotNo: string) => apiList<TraceEvent>("/traceability/events", { query: { itemCode, lotNo } });
export const getTraceBalance = (itemCode: string, lotNo: string) => apiCall<TraceBalance>("/traceability/balance", { query: { itemCode, lotNo } });
export const exportTrace = (direction: TraceDirection, itemCode: string, lotNo: string) => apiDownload(`/export/traceability?${new URLSearchParams({ direction, itemCode, lotNo })}`);
