import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface LanguageRow {
  labelKey: string;
  deviceScope: string | null;
  en: string | null;
  ja: string | null;
  vi: string | null;
}

export interface LanguageItem {
  labelKey: string;
  languageCode: "en" | "ja" | "vi";
  labelText: string;
  deviceScope: string | null;
}

export interface LanguageQuery {
  q?: string;
  labelKey?: string;
  deviceScope?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export const listLanguageData = (q: LanguageQuery = {}): Promise<ApiListSuccess<LanguageRow>> =>
  apiList<LanguageRow>("/language-data", { query: q as Record<string, string | number | undefined> });

export const saveLanguageData = (items: LanguageItem[]) =>
  apiCall<number>("/language-data", { method: "PUT", body: { items } });

export const exportLanguageDataCsv = (q: LanguageQuery = {}) => {
  const p = new URLSearchParams();
  if (q.q) p.set("q", q.q);
  if (q.deviceScope) p.set("deviceScope", q.deviceScope);
  if (q.sort) p.set("sort", q.sort);
  const qs = p.toString();
  return apiDownload("/language-data/export.csv" + (qs ? `?${qs}` : ""));
};
