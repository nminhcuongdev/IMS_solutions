import { useEffect, useState } from "react";
import { fillBold, fillText } from "@/lib/fillLabel";
import { createPrintHistory } from "@/api/history";
import { getItem, type Item } from "@/api/items";
import type { ShippingPlanRow } from "@/api/shipping";
import { useAuth } from "@/auth/AuthContext";
import { useT } from "@/i18n/LanguageContext";
import { mlPreviewLabel, mlPrintLabel } from "@/lib/mlEngine";
import { printErrorText } from "@/lib/printErrors";
import { buildProductLabelQr, qrDate } from "@/lib/qrPayload";
import { useLanPrinters } from "@/lib/useLanPrinters";
import { LanPrinterField } from "@/pages/LanPrinterField";

export function ShippingLabelModal({
  rows, reprint = false, onClose, onPrinted,
}: {
  rows: ShippingPlanRow[];
  reprint?: boolean;
  onClose: () => void;
  onPrinted: (labels: number) => void;
}) {
  const t = useT();
  const { user } = useAuth();
  const lan = useLanPrinters();
  const [items, setItems] = useState<Record<string, Item>>({});
  const [index, setIndex] = useState(0);
  const [preview, setPreview] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    const codes = [...new Set(rows.map(r => r.itemCode))];
    let alive = true;
    Promise.all(codes.map(c => getItem(c).then(it => it, () => null)))
      .then(list => {
        if (!alive) return;
        const map: Record<string, Item> = {};
        for (const it of list) if (it) map[it.itemCode] = it;
        setItems(map);
      });
    return () => { alive = false; };
  }, [rows]);

  const printable = reprint ? rows : rows.filter(r => !r.labelIssued);
  const issued = reprint ? [] : rows.filter(r => r.labelIssued);
  const current = rows[Math.min(index, rows.length - 1)];

  const fieldsOf = (row: ShippingPlanRow) => ({
    Quantiy: 1,
    PONo: row.slipNo,
    SupplierCode: row.customerCode ?? "",
    SupplierName: row.customerName ?? "",
    "Item Code": row.itemCode,
    "Item Name": row.itemName ?? row.itemCode,
    ItemCategory: "",
    Unit: row.unit ?? "",
    TotalQty: row.plannedQty,
    FactoryCode: "",
    FactoryName: "",
    Address: "",
    Lot: "",
    ProductionDate: "",
    ScheduledDate: row.scheduledDate ?? "",
    ReprintFlag: reprint ? "R" : "",
    QR: buildProductLabelQr({
      gtin: items[row.itemCode]?.barcode,
      qty: row.plannedQty,
      productionDate: qrDate(row.scheduledDate),
      itemCode: row.itemCode,
      itemName: row.itemName,
      userCode: user?.userCode,
    }),
  });

  const onPreview = async () => {
    if (!current) return;
    setBusy(true); setErr(null);
    try {
      setPreview(await mlPreviewLabel({ layoutFile: LAYOUT, setting: lan.setting, fields: fieldsOf(current) }));
    } catch (e) { setErr(printErrorText(e, t)); } finally { setBusy(false); }
  };

  const onPrint = async () => {
    setBusy(true); setErr(null);
    let done = 0;
    try {
      for (const row of printable) {
        await mlPrintLabel({ layoutFile: LAYOUT, setting: lan.setting, fields: fieldsOf(row) });
        await createPrintHistory({
          category: "Shipping", docNo: row.slipNo, itemCode: row.itemCode,
          qty: row.plannedQty, uomCode: row.unit, packageCount: 1,
          spec: lan.deviceCode, scheduledDate: row.scheduledDate, reprint,
        });
        done += 1;
      }
      onPrinted(done);
    } catch (e: any) {
      setErr(`${printErrorText(e, t)} — ${fillText(t("labelPrintedPartial", "Printed {0} of {1} labels."), done, printable.length)}`);
    } finally { setBusy(false); }
  };

  const error = err ?? lan.error;

  return (
    <div className="modal-overlay flex">
      <div className="modal-box w-242">
        <div className="modal-head">
          <span>{reprint ? t("btnReprint", "Reprint") : t("labelPrint", "Label")}</span>
          <span className="modal-close" onClick={onClose}>&times;</span>
        </div>
        <div className="modal-body">
          {current && (
            <>
              <Ro label={t("thSlipNo", "Slip No")} value={current.slipNo} />
              <Ro label={t("thCustomer", "Customer")}
                value={[current.customerName, current.customerCode].filter(Boolean).join(" / ")} />
              <Ro label={t("thItemCode", "Item Code")} value={current.itemCode} />
              <Ro label={t("thItemName", "Item Name")} value={current.itemName ?? ""} />
              <Ro label={t("thPlannedQty", "Planned")}
                value={`${current.plannedQty} ${current.unit ?? ""}`.trim()} />
              <Ro label={t("thSchedDate", "Scheduled Date")} value={current.scheduledDate ?? ""} />
            </>
          )}

          <LanPrinterField printers={lan.printers} deviceCode={lan.deviceCode} onChange={lan.setDeviceCode} />

          <div className="text-[12px] text-slate-600 mt-1.5">
            {fillBold(t("labelSummaryPerRow", "Rows: {0} · Labels per row: {1}"), printable.length, 1)}
            {lan.setting && <> · <code>{lan.setting}</code></>}
          </div>

          {issued.length > 0 && (
            <div className="text-[12px] text-amber-700 mt-1.5">
              {t("labelAlreadyIssuedSkip", "Already issued, skipped (use Reprint): {0}")
                .replace("{0}", issued.map(r => `${r.slipNo} / ${r.itemCode}`).join(", "))}
            </div>
          )}
          {error && <div className="text-rose-600 text-[12px] mt-2">{error}</div>}

          {preview && (
            <div className="mt-3 text-center">
              <img className="max-w-full shadow-[0_2px_10px_rgba(0,0,0,.18)]" src={preview} alt="Label preview" />
            </div>
          )}
        </div>
        <div className="modal-actions">
          {rows.length > 1 && (
            <>
              <button className="btn btn-outline" disabled={index <= 0}
                onClick={() => { setIndex(i => i - 1); setPreview(null); }}>&lt;</button>
              <span className="self-center text-[13px] text-slate-600">
                {index + 1} / {rows.length}
              </span>
              <button className="btn btn-outline" disabled={index >= rows.length - 1}
                onClick={() => { setIndex(i => i + 1); setPreview(null); }}>&gt;</button>
            </>
          )}
          <button className="btn btn-outline" onClick={onClose}>{t("modalClose", "Close")}</button>
          <button className="btn btn-outline" onClick={onPreview} disabled={busy || !lan.setting}>
            {t("btnPreview", "Preview")}
          </button>
          <button className="btn btn-primary" onClick={onPrint}
            disabled={busy || !lan.setting || printable.length === 0}>
            {busy ? "Đang in…" : t("btnPrint", "Print")}
          </button>
        </div>
      </div>
    </div>
  );
}

const LAYOUT = "Layout_IMS/ShippingQR_24dpmm.mllayx";

function Ro({ label, value }: { label: string; value: string }) {
  return (
    <div className="mfg">
      <label>{label}</label>
      <input className="bg-slate-50 text-slate-600" type="text" readOnly value={value} />
    </div>
  );
}
