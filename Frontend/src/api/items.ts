import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface Item {
  itemCode: string; itemName: string;
  itemGroupCode: string;
  uomCode: string | null;
  packingTypeCode: string | null;
  packageQty: number;
  expirationPeriod: number | null;
  barcode: string | null;
  surplusPoint: number | null;
  reorderLevel1: number | null;
  reorderLevel2: number | null;
  defaultLocationCode: string | null;
  isLotManaged: boolean;
  htScanRequired: boolean;
  isActive: boolean;
  remark1: string | null; remark2: string | null; remark3: string | null;
  remark4: string | null; remark5: string | null;
  createdAt: string; updatedAt: string | null;
}
export interface ItemWrite {
  itemCode: string; itemName: string;
  itemGroupCode: string;
  uomCode?: string | null;
  packingTypeCode?: string | null;
  packageQty?: number;
  expirationPeriod?: number | null;
  barcode?: string | null;
  surplusPoint?: number | null;
  reorderLevel1?: number | null;
  reorderLevel2?: number | null;
  defaultLocationCode?: string | null;
  isLotManaged?: boolean;
  htScanRequired?: boolean;
  isActive?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}

const P = "/masters/items";
export const listItems = (q: { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean; itemGroupCode?: string } = {}): Promise<ApiListSuccess<Item>> =>
  apiList<Item>(P, { query: q as any });
export const getItem = (code: string) => apiCall<Item>(`${P}/${encodeURIComponent(code)}`);
export const createItem = (body: ItemWrite) => apiCall<Item>(P, { method: "POST", body });
export const updateItem = (code: string, body: ItemWrite) =>
  apiCall<Item>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteItem = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportItemsCsv = () => apiDownload("/export/items");
