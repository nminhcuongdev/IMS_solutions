import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface Process {
  processCode: string;
  processName: string;
  displayOrder: number | null;
  isActive: boolean;
  remark1: string | null;
  remark2: string | null;
  remark3: string | null;
  remark4: string | null;
  remark5: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export type ProcessWrite = Pick<Process, "processCode" | "processName" | "displayOrder" | "isActive" | "remark1" | "remark2" | "remark3" | "remark4" | "remark5">;
const path = "/masters/processes";
export const listProcesses = (query: { q?: string; page?: number; pageSize?: number; sort?: string } = {}): Promise<ApiListSuccess<Process>> => apiList(path, { query });
export const createProcess = (body: ProcessWrite) => apiCall<Process>(path, { method: "POST", body });
export const updateProcess = (code: string, body: ProcessWrite) => apiCall<Process>(`${path}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteProcess = (code: string) => apiCall<void>(`${path}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportProcessesCsv = () => apiDownload("/export/processes");
