import { useEffect, useState } from "react";
import { fillBold, fillText } from "@/lib/fillLabel";
import { createPrintHistory } from "@/api/history";
import { getItem, type Item } from "@/api/items";
import type { ReturnPlanRow } from "@/api/returns";
import { useAuth } from "@/auth/AuthContext";
import { useT } from "@/i18n/LanguageContext";
import { mlPreviewLabel, mlPrintLabel } from "@/lib/mlEngine";
import { printErrorText } from "@/lib/printErrors";
import { buildProductLabelQr, qrDate } from "@/lib/qrPayload";
import { useLanPrinters } from "@/lib/useLanPrinters";
import { LanPrinterField } from "@/pages/LanPrinterField";

export function ReturnLabelModal({
  rows, onClose, onPrinted,
}: {
  rows: ReturnPlanRow[];
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

  const printable = rows.filter(r => !r.labelIssued);
  const issued = rows.filter(r => r.labelIssued);
  const current = rows[Math.min(index, rows.length - 1)];

  const fieldsOf = (row: ReturnPlanRow) => ({
    Quantiy: 1,
    PONo: row.returnNo,
    SupplierCode: row.returnedByCode ?? "",
    SupplierName: [row.returnedByName, row.returnedByCode && `(${row.returnedByCode})`]
      .filter(Boolean).join(" "),
    "Item Code": row.itemCode,
    "Item Name": row.itemName ?? row.itemCode,
    ItemCategory: "",
    Unit: row.unit ?? "",
    ItemQty: row.expectedQty,
    FactoryCode: "",
    FactoryName: "",
    Address: "",
    Lot: row.lotNo ?? "",
    ScheduledDate: row.returnDate ?? "",
    ReprintFlag: row.labelIssued ? "REPRINT" : "",
    QR: buildProductLabelQr({
      gtin: items[row.itemCode]?.barcode,
      lotNo: row.lotNo,
      qty: row.expectedQty,
      productionDate: qrDate(row.returnDate),
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
          category: "Returns", docNo: row.returnNo, itemCode: row.itemCode,
          qty: row.expectedQty, uomCode: row.unit, packageCount: 1,
          spec: lan.deviceCode, scheduledDate: row.returnDate,
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
          <span>{t("labelPrint", "Label")}</span>
          <span className="modal-close" onClick={onClose}>&times;</span>
        </div>
        <div className="modal-body">
          {current && (
            <>
              <Ro label={t("thReturnNo", "Return No")} value={current.returnNo} />
              <Ro label={t("thItemCode", "Item Code")} value={current.itemCode} />
              <Ro label={t("thItemName", "Item Name")} value={current.itemName ?? ""} />
              <Ro label={t("thExpectedQty", "Expected")}
                value={`${current.expectedQty} ${current.unit ?? ""}`.trim()} />
              <Ro label={t("thLotNo", "Lot No")} value={current.lotNo ?? ""} />
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
                .replace("{0}", issued.map(r => `${r.returnNo} / ${r.itemCode}`).join(", "))}
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

const LAYOUT = "Layout_IMS/ReturnsQR_24dpmm.mllayx";

function Ro({ label, value }: { label: string; value: string }) {
  return (
    <div className="mfg">
      <label>{label}</label>
      <input className="bg-slate-50 text-slate-600" type="text" readOnly value={value} />
    </div>
  );
}
