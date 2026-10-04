import { apiCall } from "./client";


export interface AlertRow {
  code: string;
  name: string | null;
  extra: string | null;
}

export interface AlertBlock {
  alertTypeCode: string;
  labelKey: string;
  extraLabelKey: string;
  total: number;
  rows: AlertRow[];
}

export const listAlerts = () => apiCall<AlertBlock[]>("/alerts");
