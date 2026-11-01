import { ApiCallError } from "@/api/types";


export const QR_MAX_LENGTH = 250;

export interface ProductLabelQrInput {
  gtin?: string | null;
  lotNo?: string | null;
  qty: number;
  productionDate?: string | null;
  expiryDate?: string | null;
  itemCode: string;
  itemName?: string | null;
  locationCode?: string | null;
  locationName?: string | null;
  userCode?: string | null;
  printedAt?: Date;
}

const p2 = (n: number) => String(n).padStart(2, "0");

export function qrTimestamp(d: Date): string {
  return `${p2(d.getFullYear() % 100)}${p2(d.getMonth() + 1)}${p2(d.getDate())}` +
    `${p2(d.getHours())}${p2(d.getMinutes())}${p2(d.getSeconds())}`;
}

export function qrDate(v: string | null | undefined): string | null {
  if (!v) return null;
  const m = /^(\d{4})[-/]?(\d{2})[-/]?(\d{2})/.exec(v.trim());
  return m ? `${m[1].slice(2)}${m[2]}${m[3]}` : null;
}

const enc = (v: string | number) => encodeURIComponent(String(v));

export function buildLocationLabelQr(locationCode: string, locationName?: string | null): string {
  const full = `?lc_cd=${enc(locationCode)}` + (locationName ? `&lc_name=${enc(locationName)}` : "");
  return full.length <= QR_MAX_LENGTH ? full : `?lc_cd=${enc(locationCode)}`;
}

const DROP_ORDER = ["lc_name", "item_name", "user_cd", "print_at", "11", "13"];

function composeQr(path: string, attrs: [string, string | number | null | undefined][]): string {
  const present = attrs.filter(([, v]) => v !== null && v !== undefined && v !== "");
  const build = (skip: Set<string>) =>
    path + "?" + present
      .filter(([k]) => !skip.has(k))
      .map(([k, v]) => `${k}=${enc(v!)}`)
      .join("&");

  const skip = new Set<string>();
  let out = build(skip);
  for (const key of DROP_ORDER) {
    if (out.length <= QR_MAX_LENGTH) return out;
    skip.add(key);
    out = build(skip);
  }
  if (out.length <= QR_MAX_LENGTH) return out;
  throw new ApiCallError({
    code: "VAL-005", httpStatus: 400, severity: "Error",
    message: `QR ${out.length} > ${QR_MAX_LENGTH}`,
  });
}

const keyPath = (gtin?: string | null, lotNo?: string | null) =>
  (gtin ? `/01/${enc(gtin)}` : "") + (lotNo ? `/10/${enc(lotNo)}` : "");

export function buildProductLabelQr(i: ProductLabelQrInput): string {
  return composeQr(keyPath(i.gtin, i.lotNo), [
    ["11", i.productionDate],
    ["17", i.expiryDate],
    ["30", i.qty],
    ["item_cd", i.itemCode],
    ["item_name", i.itemName],
    ["print_at", qrTimestamp(i.printedAt ?? new Date())],
    ["lc_cd", i.locationCode],
    ["lc_name", i.locationName],
    ["user_cd", i.userCode],
  ]);
}

export interface ShippingLabelQrInput {
  gtin?: string | null;
  lotNo?: string | null;
  qty: number;
  itemCode: string;
  itemName?: string | null;
  printedAt?: Date;
}

export function buildShippingLabelQr(i: ShippingLabelQrInput): string {
  const stamp = qrTimestamp(i.printedAt ?? new Date());
  return composeQr(keyPath(i.gtin, i.lotNo), [
    ["13", stamp.slice(0, 6)],
    ["37", i.qty],
    ["item_cd", i.itemCode],
    ["item_name", i.itemName],
    ["print_at", stamp],
  ]);
}
