import { listDefaultValues, type DefaultValue } from "@/api/defaultValues";
import { useFetch } from "./useFetch";

const PACKING_BADGE_CLASSES = ["badge-info", "badge-warn", "badge-cancel", "badge-ok", "badge-ng"];

export const statusBadge = (isActive: boolean) => (isActive ? "badge-ok" : "badge-ng");
export const htScanBadge = (required: boolean) => (required ? "badge-info" : "badge-cancel");
export const holdBadge = (isOnHold: boolean) => (isOnHold ? "badge-ng" : "badge-ok");

export const roleBadge = (role: string | null | undefined) =>
  role === "Admin" ? "badge-ok" : role === "Worker" ? "badge-warn" : "badge-info";

const DIST_STATUS_BADGE: Record<string, string> = {
  latest: "badge-ok", downloaded: "badge-info", waiting: "badge-warn",
  attention: "badge-cancel", unconfirmed: "badge-cancel", notTarget: "badge-info",
};
const DIST_STATUS_SYMBOL: Record<string, string> = {
  latest: "○", downloaded: "△", waiting: "△", attention: "×", unconfirmed: "×", notTarget: "—",
};
export const distStatusBadge = (status: string) => DIST_STATUS_BADGE[status] ?? "badge-info";
export const distStatusSymbol = (status: string) => DIST_STATUS_SYMBOL[status] ?? "—";

const RULE_STATE_BADGE: Record<string, string> = {
  Scheduled: "badge-info", Active: "badge-ok", Stopped: "badge-cancel", Superseded: "badge-cancel", Error: "badge-ng",
};
export const ruleStateBadge = (state: string) => RULE_STATE_BADGE[state] ?? "badge-info";

export function usePackingTypes(): DefaultValue[] {
  const { data } = useFetch("defaultValues:PackingType", () => listDefaultValues("PackingType"));
  return data ?? [];
}

export function packingBadge(code: string, list: DefaultValue[]): string {
  const idx = Math.max(0, list.findIndex(p => p.valueCode === code));
  return PACKING_BADGE_CLASSES[idx % PACKING_BADGE_CLASSES.length];
}
