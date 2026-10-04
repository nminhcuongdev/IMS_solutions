import { apiCall } from "./client";

export interface DefaultValue {
  valueType: string;
  valueCode: string;
  nameLang1: string;
  nameLang2: string;
  nameLang3: string;
  displayOrder: number;
  isActive: boolean;
}

export const listDefaultValues = (type: string) =>
  apiCall<DefaultValue[]>(`/masters/default-values/${encodeURIComponent(type)}`);
