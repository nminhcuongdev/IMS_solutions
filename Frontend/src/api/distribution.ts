import { apiCall, apiList, getToken, clearToken } from "./client";
import { ApiCallError, type ApiError, type ApiSuccess } from "./types";

export type DeviceUpdateStatus = "latest" | "downloaded" | "waiting" | "attention" | "unconfirmed" | "notTarget";
export type RuleState = "Scheduled" | "Active" | "Stopped" | "Superseded" | "Error";

export interface InspectionCheck { id: string; status: "ok" | "warn" | "ng" | "na"; detail: string | null; }

export interface DistPackage {
  id: number; packageName: string; versionCode: number; versionName: string; fileName: string;
  originalFileName: string; sizeBytes: number; sha256: string; signerFingerprint: string;
  createdBy: string | null; createdAt: string; inUse: boolean;
}

export interface PackageUploadResult { ok: boolean; checks: InspectionCheck[]; package: DistPackage | null; }

export interface TargetQuery { models?: string[]; locations?: string[]; include?: string[]; exclude?: string[]; }

export interface RuleWrite extends TargetQuery {
  packageId: number; deliverAt?: string | null; endAt?: string | null; force?: boolean;
  minVersionCode?: number | null; noteJa?: string | null; noteEn?: string | null; noteVi?: string | null;
}

export interface RuleSummary { targets: number; latest: number; downloaded: number; waiting: number; attention: number; unconfirmed: number; }

export interface DistRule {
  id: number; packageId: number; versionName: string; versionCode: number; apkFile: string; sha256: string;
  models: string[]; locations: string[]; include: string[]; exclude: string[]; autoAdd: boolean;
  deliverAt: string; endAt: string | null; state: RuleState; force: boolean; minVersionCode: number | null;
  noteJa: string | null; noteEn: string | null; noteVi: string | null; publishedAt: string | null;
  activatedAt: string | null; error: string | null; stopReason: string | null; deploySteps: string[];
  createdBy: string | null; createdAt: string; summary: RuleSummary;
}

export interface DistDevice {
  deviceCode: string; deviceName: string; model: string | null; locationCode: string | null; isActive: boolean;
  versionCode: number | null; versionName: string | null; lastCheckAt: string | null; lastUpdateAt: string | null;
  downloadedAt: string | null; status: DeviceUpdateStatus; targetVersionCode: number | null; targetVersionName: string | null;
}

export interface VersionEntry {
  packageName: string; versionCode: number; versionName: string; updatedAt: string; apkFile: string; sha256: string;
  minVersionCode?: number; force?: boolean; notes?: Record<string, string>;
}
export interface VersionJson { apps: VersionEntry[]; }

export interface RuleDetail { rule: DistRule; targets: DistDevice[]; versionJson: VersionJson; }
export interface DeviceCheck { receivedAt: string; appVersion: number; }
export interface UnknownDevice { deviceCode: string; firstAt: string; lastAt: string; requestCount: number; }
export interface DistAudit { at: string; who: string; action: string; target: string | null; }
export interface DistSettings { packageName: string; uploadMb: number; retention: number; nDays: number; mDays: number; autoAdd: boolean; }
export interface DeviceAttributes { models: string[]; locations: string[]; }

const P = "/distribution";

export const listDistDevices = (status?: string) => apiList<DistDevice>(`${P}/devices`, { query: { status } });
export const listPackages = () => apiList<DistPackage>(`${P}/packages`);
export const deletePackage = (id: number) => apiCall<void>(`${P}/packages/${id}`, { method: "DELETE" });
export const getAttributes = () => apiCall<DeviceAttributes>(`${P}/attributes`);
export const previewTargets = (body: TargetQuery) => apiCall<DistDevice[]>(`${P}/targets/preview`, { method: "POST", body });
export const listRules = () => apiList<DistRule>(`${P}/rules`);
export const getRule = (id: number) => apiCall<RuleDetail>(`${P}/rules/${id}`);
export const createRule = (body: RuleWrite) => apiCall<DistRule>(`${P}/rules`, { method: "POST", body });
export const ruleAction = (id: number, action: "stop" | "resume" | "retry") =>
  apiCall<DistRule>(`${P}/rules/${id}/${action}`, { method: "POST" });
export const deleteRule = (id: number) => apiCall<void>(`${P}/rules/${id}`, { method: "DELETE" });
export const deviceHistory = (code: string) => apiList<DeviceCheck>(`${P}/devices/${encodeURIComponent(code)}/history`);
export const listUnknown = () => apiList<UnknownDevice>(`${P}/unknown-devices`);
export const listAudit = () => apiList<DistAudit>(`${P}/audit`);
export const getDistSettings = () => apiCall<DistSettings>(`${P}/settings`);
export const saveDistSettings = (body: DistSettings) => apiCall<DistSettings>(`${P}/settings`, { method: "PUT", body });
export const getVersionJson = () => apiCall<VersionJson>(`${P}/version-json`);

function apiBase(): string {
  const explicit = (import.meta.env.VITE_API_BASE as string | undefined)?.trim();
  if (explicit) return explicit.replace(/\/+$/, "");
  const port = (import.meta.env.VITE_API_PORT as string | undefined)?.trim() || "8091";
  return `${window.location.protocol}//${window.location.hostname}:${port}`;
}

export const updateSourceUrl = () => `${apiBase()}/dl/`;

export async function uploadPackage(file: File): Promise<PackageUploadResult> {
  const fd = new FormData();
  fd.append("file", file);
  const token = getToken();
  const res = await fetch(`${apiBase()}${P}/packages`, {
    method: "POST", headers: token ? { Authorization: `Bearer ${token}` } : {}, body: fd,
  });
  if (res.status === 401) clearToken();
  const text = await res.text();
  let json: any = {};
  try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }
  if (!res.ok || json?.success === false) {
    throw new ApiCallError((json as ApiError).error ?? {
      code: `HTTP-${res.status}`, httpStatus: res.status, severity: "Error", message: text || res.statusText,
    });
  }
  return (json as ApiSuccess<PackageUploadResult>).data;
}
