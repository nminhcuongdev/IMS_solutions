import { apiCall } from "./client";

export type ScannedFilter = "all" | "yes" | "no";

export interface StockScanRow {
  epc: string;
  itemCode: string | null;
  itemName: string | null;
  itemCategory: string | null;
  isScanned: boolean;
  workerCode: string | null;
  workerName: string | null;
}

export interface StockScanBoard {
  scanned: number;
  total: number;
  rows: StockScanRow[];
}

export interface StockScanQuery {
  itemCode?: string;
  itemName?: string;
  scanned?: ScannedFilter;
}

export const listStockScan = (q: StockScanQuery = {}) =>
  apiCall<StockScanBoard>("/stock-scan", {
    query: { itemCode: q.itemCode, itemName: q.itemName, scanned: q.scanned === "all" ? undefined : q.scanned },
  });

export const resetStockScan = () =>
  apiCall<StockScanBoard>("/stock-scan/reset", { method: "POST" });
