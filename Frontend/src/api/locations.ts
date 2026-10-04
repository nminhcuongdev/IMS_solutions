import { apiCall, apiDownload, apiList } from "./client";
import type { ApiListSuccess } from "./types";

export type LocationType = "Large" | "Middle" | "Small";

export interface Location {
  locationCode: string;
  locationType: LocationType;
  largeCode: string; middleCode: string | null; smallCode: string | null;
  locationName: string;
  zoneCode: string | null;
  parentLocationCode: string | null;
  isActive: boolean;
  remark1: string | null; remark2: string | null; remark3: string | null;
  remark4: string | null; remark5: string | null;
  createdAt: string; updatedAt: string | null;
}

export interface LocationWrite {
  locationType: LocationType;
  inputCode: string;
  locationName: string;
  parentLocationCode?: string | null;
  zoneCode?: string | null;
  isActive?: boolean;
  remark1?: string | null; remark2?: string | null; remark3?: string | null;
  remark4?: string | null; remark5?: string | null;
}

const P = "/masters/locations";
export const listLocations = (q: { page?: number; pageSize?: number; q?: string; sort?: string; isActive?: boolean; type?: LocationType } = {}): Promise<ApiListSuccess<Location>> =>
  apiList<Location>(P, { query: q as any });
export const getLocation = (code: string) => apiCall<Location>(`${P}/${encodeURIComponent(code)}`);
export const createLocation = (body: LocationWrite) => apiCall<Location>(P, { method: "POST", body });
export const updateLocation = (code: string, body: LocationWrite) =>
  apiCall<Location>(`${P}/${encodeURIComponent(code)}`, { method: "PUT", body });
export const deleteLocation = (code: string) => apiCall<void>(`${P}/${encodeURIComponent(code)}`, { method: "DELETE" });
export const exportLocationsCsv = () => apiDownload(`${P}/export.csv`);
