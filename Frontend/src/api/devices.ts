import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export const DEVICE_TYPES = [
  "TabletAndroid", "TabletWindows", "Handheld", "RfidHandheld",
  "RfidGate", "Printer", "WeighingScale", "Other",
] as const;

export interface Device {
  deviceCode: string;
  deviceName: string;
  deviceType: string;
  model: string | null;
  manufacturer: string | null;
  spec?: string | null;
  serialNo?: string | null;
  ipAddress: string | null;
  locationCode?: string | null;
  isActive: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
  createdAt?: string; updatedAt?: string | null;
}

export interface DeviceWrite {
  deviceCode: string; deviceName: string; deviceType: string;
  model?: string | null; manufacturer?: string | null; spec?: string | null;
  serialNo?: string | null; ipAddress?: string | null; locationCode?: string | null;
  isActive?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}

export interface DeviceListQuery { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean; deviceType?: string; updateStatus?: string; }

const P = "/masters/devices";
export const listDevices = (q: DeviceListQuery = {}): Promise<ApiListSuccess<Device>> =>
  apiList<Device>(P, { query: q as any });
export const createDevice = (body: DeviceWrite) => apiCall<Device>(P, { method: "POST", body });
export const updateDevice = (code: string, body: DeviceWrite) =>
  apiCall<Device>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteDevice = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportDevicesCsv = () => apiDownload(`${P}/export.csv`);

export const listPrinters = (): Promise<ApiListSuccess<Device>> =>
  apiList<Device>(P, { query: { deviceType: "Printer", isActive: true, pageSize: 200 } });
