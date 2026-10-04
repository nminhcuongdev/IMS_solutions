
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

export function buildProductLabelQr(i: ProductLabelQrInput): string {
  const path =
    (i.gtin ? `/01/${enc(i.gtin)}` : "") +
    (i.lotNo ? `/10/${enc(i.lotNo)}` : "");

  const attrs: [string, string | number | null | undefined][] = [
    ["11", i.productionDate],
    ["17", i.expiryDate],
    ["30", i.qty],
    ["item_cd", i.itemCode],
    ["item_name", i.itemName],
    ["print_at", qrTimestamp(i.printedAt ?? new Date())],
    ["lc_cd", i.locationCode],
    ["lc_name", i.locationName],
    ["user_cd", i.userCode],
  ];

  const build = (skip: Set<string>) =>
    path + "?" + attrs
      .filter(([k, v]) => !skip.has(k) && v !== null && v !== undefined && v !== "")
      .map(([k, v]) => `${k}=${enc(v!)}`)
      .join("&");

  let out = build(new Set());
  if (out.length <= QR_MAX_LENGTH) return out;

  for (const drop of [["lc_name"], ["lc_name", "item_name"]]) {
    out = build(new Set(drop));
    if (out.length <= QR_MAX_LENGTH) return out;
  }
  return out.slice(0, QR_MAX_LENGTH);
}
