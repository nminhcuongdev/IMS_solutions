import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export const ITEM_GROUP_CATEGORY_LABELS: Record<number, [string, string]> = {
  1: ["igCategory1", "1. Finished Goods"],
  2: ["igCategory2", "2. Semi-Finished"],
  3: ["igCategory3", "3. Work in Progress"],
  4: ["igCategory4", "4. Raw Materials"],
  5: ["igCategory5", "5. Purchased Parts"],
  6: ["igCategory6", "6. Outsourced Processing"],
  7: ["igCategory7", "7. Auxiliary Materials"],
};

export interface ItemGroup {
  itemGroupCode: string; itemGroupName: string; category: number; isActive: boolean;
  remark1: string | null; remark2: string | null; remark3: string | null;
  remark4: string | null; remark5: string | null;
  createdAt: string; updatedAt: string | null;
}
export interface ItemGroupWrite {
  itemGroupCode: string; itemGroupName: string; category: number;
  isActive?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}

const P = "/masters/item-groups";
export const listItemGroups = (q: { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean } = {}): Promise<ApiListSuccess<ItemGroup>> =>
  apiList<ItemGroup>(P, { query: q as any });
export const getItemGroup = (code: string) => apiCall<ItemGroup>(`${P}/${encodeURIComponent(code)}`);
export const createItemGroup = (body: ItemGroupWrite) => apiCall<ItemGroup>(P, { method: "POST", body });
export const updateItemGroup = (code: string, body: ItemGroupWrite) =>
  apiCall<ItemGroup>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteItemGroup = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportItemGroupsCsv = () => apiDownload(`${P}/export.csv`);
