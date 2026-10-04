import { listDefaultValues, type DefaultValue } from "@/api/defaultValues";
import { useFetch } from "./useFetch";

const PACKING_BADGE_CLASSES = ["badge-info", "badge-warn", "badge-cancel", "badge-ok", "badge-ng"];

export const statusBadge = (isActive: boolean) => (isActive ? "badge-ok" : "badge-ng");
export const holdBadge = (isOnHold: boolean) => (isOnHold ? "badge-ng" : "badge-ok");

export const roleBadge = (role: string | null | undefined) =>
  role === "Admin" ? "badge-ok" : role === "Worker" ? "badge-warn" : "badge-info";

export function usePackingTypes(): DefaultValue[] {
  const { data } = useFetch("defaultValues:PackingType", () => listDefaultValues("PackingType"));
  return data ?? [];
}

export function packingBadge(code: string, list: DefaultValue[]): string {
  const idx = Math.max(0, list.findIndex(p => p.valueCode === code));
  return PACKING_BADGE_CLASSES[idx % PACKING_BADGE_CLASSES.length];
}
