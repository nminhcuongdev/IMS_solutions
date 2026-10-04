import { useEffect, useMemo, useState } from "react";
import { fillBold, fillText } from "@/lib/fillLabel";
import { createPrintHistory } from "@/api/history";
import { getItem, type Item } from "@/api/items";
import type { ReceivingPlanRow } from "@/api/receiving";
import { useAuth } from "@/auth/AuthContext";
import { useT } from "@/i18n/LanguageContext";
import { mlPreviewLabel, mlPrintLabel } from "@/lib/mlEngine";
import { printErrorText } from "@/lib/printErrors";
import { buildProductLabelQr, qrDate } from "@/lib/qrPayload";
import { useLanPrinters } from "@/lib/useLanPrinters";
import { LanPrinterField } from "@/pages/LanPrinterField";

export function ReceivingLabelModal({
  rows, reprint = false, onClose, onPrinted,
}: {
  rows: ReceivingPlanRow[];
  reprint?: boolean;
  onClose: () => void;
  onPrinted: (labels: number) => void;
}) {
  const t = useT();
  const { user } = useAuth();
  const { printers, deviceCode, setDeviceCode, setting, error: printersError } = useLanPrinters();
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

  const plans = useMemo(() => rows.map(r => planOf(r, items[r.itemCode])), [rows, items]);
  const printable = reprint ? plans : plans.filter(p => !p.row.labelIssued);
  const issued = reprint ? [] : plans.filter(p => p.row.labelIssued);
  const totalLabels = printable.reduce((n, p) => n + p.labelCount, 0);
  const current = plans[Math.min(index, plans.length - 1)];

  const fieldsOf = (p: LabelPlan) => ({
    Quantiy: p.labelCount,
    PONo: p.row.poNo,
    SupplierCode: p.row.supplierCode ?? "",
    SupplierName: p.row.supplierName ?? "",
    "Item Code": p.row.itemCode,
    "Item Name": p.row.itemName ?? p.row.itemCode,
    ItemCategory: "",
    Unit: p.row.unit ?? "",
    ItemQty: p.expectedQty,
    FactoryCode: "",
    FactoryName: "",
    Address: "",
    Lot: p.row.lotNo ?? "",
    ScheduledDate: p.row.scheduledDate ?? "",
    ReprintFlag: reprint ? "R" : "",
    QR: buildProductLabelQr({
      gtin: p.item?.barcode,
      lotNo: p.row.lotNo,
      qty: p.expectedQty,
      productionDate: qrDate(p.row.scheduledDate),
      itemCode: p.row.itemCode,
      itemName: p.row.itemName,
      userCode: user?.userCode,
    }),
  });

  const onPreview = async () => {
    if (!current) return;
    setBusy(true); setErr(null);
    try {
      setPreview(await mlPreviewLabel({ layoutFile: LAYOUT, setting, fields: fieldsOf(current) }));
    } catch (e) { setErr(printErrorText(e, t)); } finally { setBusy(false); }
  };

  const onPrint = async () => {
    setBusy(true); setErr(null);
    let done = 0;
    try {
      for (const p of printable) {
        await mlPrintLabel({ layoutFile: LAYOUT, setting, fields: fieldsOf(p) });
        await createPrintHistory({
          category: "Receiving", docNo: p.row.poNo, itemCode: p.row.itemCode,
          qty: p.expectedQty, uomCode: p.row.unit, packageCount: p.labelCount,
          spec: deviceCode, scheduledDate: p.row.scheduledDate, reprint,
        });
        done += p.labelCount;
      }
      onPrinted(done);
    } catch (e: any) {
      setErr(`${printErrorText(e, t)} — ${fillText(t("labelPrintedPartial", "Printed {0} of {1} labels."), done, totalLabels)}`);
    } finally { setBusy(false); }
  };

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
              <Ro label={t("thPoNo", "PO No")} value={current.row.poNo} />
              <Ro label={t("thItemCode", "Item Code")} value={current.row.itemCode} />
              <Ro label={t("thItemName", "Item Name")} value={current.row.itemName ?? ""} />
              <Ro label={t("thPlanned", "Planned")}
                value={`${current.row.plannedQty} ${current.row.unit ?? ""}`.trim()} />
              <Ro label={t("thPackingQty", "Packing Qty")} value={String(current.labelCount)} />
              <Ro label={t("thExpectedQty", "Expected")}
                value={`${current.expectedQty} ${current.row.unit ?? ""}`.trim()} />
              <Ro label={t("thLabelCount", "Label Print Count")} value={String(current.labelCount)} />
            </>
          )}

          <LanPrinterField printers={printers} deviceCode={deviceCode} onChange={setDeviceCode} />

          <div className="text-[12px] text-slate-600 mt-1.5">
            {fillBold(t("labelSummaryTotal", "Rows: {0} · Total labels: {1}"), printable.length, totalLabels)}
            {setting && <> · <code>{setting}</code></>}
          </div>

          {issued.length > 0 && (
            <div className="text-[12px] text-amber-700 mt-1.5">
              {t("labelAlreadyIssuedSkip", "Already issued, skipped (use Reprint): {0}")
                .replace("{0}", issued.map(p => `${p.row.poNo} / ${p.row.itemCode}`).join(", "))}
            </div>
          )}

          {(err ?? printersError) && (
            <div className="text-rose-600 text-[12px] mt-2">{err ?? printersError}</div>
          )}

          {preview && (
            <div className="mt-3 text-center">
              <img className="max-w-full shadow-[0_2px_10px_rgba(0,0,0,.18)]" src={preview} alt="Label preview" />
            </div>
          )}
        </div>
        <div className="modal-actions">
          {plans.length > 1 && (
            <>
              <button className="btn btn-outline" disabled={index <= 0}
                onClick={() => { setIndex(i => i - 1); setPreview(null); }}>&lt;</button>
              <span className="self-center text-[13px] text-slate-600">
                {index + 1} / {plans.length}
              </span>
              <button className="btn btn-outline" disabled={index >= plans.length - 1}
                onClick={() => { setIndex(i => i + 1); setPreview(null); }}>&gt;</button>
            </>
          )}
          <button className="btn btn-outline" onClick={onClose}>{t("modalClose", "Close")}</button>
          <button className="btn btn-outline" onClick={onPreview} disabled={busy || !setting}>
            {t("btnPreview", "Preview")}
          </button>
          <button className="btn btn-primary" onClick={onPrint}
            disabled={busy || !setting || printable.length === 0}>
            {busy ? "Đang in…" : t("btnPrint", "Print")}
          </button>
        </div>
      </div>
    </div>
  );
}

const LAYOUT = "Layout_IMS/ReceivingQR_24dpmm.mllayx";

interface LabelPlan {
  row: ReceivingPlanRow;
  item?: Item;
  expectedQty: number;
  labelCount: number;
}

function planOf(row: ReceivingPlanRow, item?: Item): LabelPlan {
  const pack = row.packingQty && row.packingQty > 0 ? Math.round(row.packingQty) : 0;
  if (pack <= 0) return { row, item, expectedQty: row.plannedQty, labelCount: 1 };
  return { row, item, expectedQty: row.plannedQty / pack, labelCount: pack };
}

function Ro({ label, value }: { label: string; value: string }) {
  return (
    <div className="mfg">
      <label>{label}</label>
      <input className="bg-slate-50 text-slate-600" type="text" readOnly value={value} />
    </div>
  );
}
