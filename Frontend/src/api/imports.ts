import { ApiCallError, type ApiError, type ApiSuccess } from "./types";
import { API_BASE, apiDownload, clearToken, getToken } from "./client";


export interface ImportRowError {
  row: number;
  code: string;
  message: string;
  detail?: string | null;
}

export interface ImportResult {
  importedCount: number;
  skippedCount: number;
  errors: ImportRowError[];
}

export type ImportKind = "receiving-schedule" | "shipping-schedule" | "returns-schedule" | "picking-schedule";

export const IMPORT_COLUMNS: Record<ImportKind, { columns: string[]; required: string[] }> = {
  "receiving-schedule": {
    columns: ["po_no", "po_branch_no", "partner_code", "item_code", "qty", "factory_code", "scheduled_date"],
    required: ["po_no", "item_code", "qty"],
  },
  "shipping-schedule": {
    columns: ["so_no", "so_branch_no", "partner_code", "item_code", "qty", "factory_code", "scheduled_date",
      "gate_code", "invoice_no"],
    required: ["so_no", "item_code", "qty"],
  },
  "returns-schedule": {
    columns: ["return_no", "return_branch_no", "partner_code", "item_code", "qty", "factory_code",
      "scheduled_date", "invoice_no", "lot_no"],
    required: ["return_no", "item_code", "qty"],
  },
  "picking-schedule": {
    columns: ["so_no", "so_branch_no", "item_code", "location_code", "qty", "scheduled_date"],
    required: ["so_no", "item_code", "qty"],
  },
};

export async function importSchedule(kind: ImportKind, file: File): Promise<ImportResult> {
  const fd = new FormData();
  fd.append("file", file);

  const token = getToken();
  const res = await fetch(`${API_BASE}/import/${kind}`, {
    method: "POST",
    headers: token ? { Authorization: `Bearer ${token}` } : {},
    body: fd,
  });

  if (res.status === 401) clearToken();

  const text = await res.text();
  const json = text ? JSON.parse(text) : {};
  if (!res.ok || json?.success === false) {
    const err = (json as ApiError).error ?? {
      code: `HTTP-${res.status}`, httpStatus: res.status, severity: "Error",
      message: text || res.statusText,
    };
    throw new ApiCallError(err);
  }
  return (json as ApiSuccess<ImportResult>).data;
}

export const downloadImportTemplate = (kind: ImportKind) =>
  apiDownload(`/schedules/${kind.replace("-schedule", "")}/template.xlsx`);
