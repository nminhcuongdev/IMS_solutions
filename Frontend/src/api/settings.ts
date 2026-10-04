import { apiCall } from "./client";

export interface AppSettings {
  invAggDefault: string;
  masterPrinterDefault: string | null;
  schedulePrinterDefault: string | null;
}

export const getSettings = () => apiCall<AppSettings>("/settings");

export const saveSettings = (body: AppSettings) =>
  apiCall<AppSettings>("/settings", { method: "PUT", body });
