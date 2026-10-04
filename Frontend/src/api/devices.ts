import { apiList } from "./client";
import type { ApiListSuccess } from "./types";

export interface Device {
  deviceCode: string;
  deviceName: string;
  deviceType: string;
  model: string | null;
  manufacturer: string | null;
  ipAddress: string | null;
  isActive: boolean;
}

export const listPrinters = (): Promise<ApiListSuccess<Device>> =>
  apiList<Device>("/masters/devices", { query: { deviceType: "Printer", isActive: true, pageSize: 200 } });
