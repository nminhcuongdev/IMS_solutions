import { useEffect, useState } from "react";
import { getReprintData, reprintPrintHistory, type PrintHistoryRow, type PrintSource } from "@/api/history";
import { useAuth } from "@/auth/AuthContext";
import { mlPreviewLabel, mlPrintLabel } from "@/lib/mlEngine";
import { printErrorText } from "@/lib/printErrors";
import { buildProductLabelQr, buildShippingLabelQr, qrDate } from "@/lib/qrPayload";
import { useLanPrinters } from "@/lib/useLanPrinters";
import { useT } from "@/i18n/LanguageContext";
import { LanPrinterField } from "@/pages/LanPrinterField";
import { errorText } from "@/lib/errorText";
import { fillText } from "@/lib/fillLabel";

function baseCategory(category: string) {
  return category.replace(/Reprint$/, "");
}

function rebuild(category: string, s: PrintSource, userCode?: string): Record<string, string> {
  const shipping = category === "Shipping";
  const qty = String(s.qty ?? 0);
  return {
    PONo: s.docNo,
    SupplierCode: s.partnerCode ?? "",
    SupplierName: s.partnerName ?? "",
    "Item Code": s.itemCode,
    "Item Name": s.itemName ?? s.itemCode,
    Unit: s.uomCode ?? "",
    Lot: s.lotNo ?? "",
    FactoryCode: s.factoryCode ?? "",
    FactoryName: s.factoryName ?? "",
    Address: s.address ?? "",
    ScheduledDate: s.scheduledDate ?? "",
    ReprintFlag: "REPRINT",
    [shipping ? "TotalQty" : "ItemQty"]: qty,
    QR: shipping
      ? buildShippingLabelQr({ gtin: s.barcode, lotNo: s.lotNo, qty: s.qty ?? 0, itemCode: s.itemCode, itemName: s.itemName })
      : buildProductLabelQr({
        gtin: s.barcode, lotNo: s.lotNo, qty: s.qty ?? 0,
        productionDate: qrDate(s.productionDate), expiryDate: qrDate(s.expiryDate),
        itemCode: s.itemCode, itemName: s.itemName, userCode,
      }),
  };
}

export function ReprintModal({ row, onClose, onPrinted }: {
  row: PrintHistoryRow;
  onClose: () => void;
  onPrinted: (count: number) => void;
}) {
  const t = useT();
  const { user } = useAuth();
  const lan = useLanPrinters(row.spec);
  const [count, setCount] = useState(1);
  const [layout, setLayout] = useState<string | null>(null);
  const [fields, setFields] = useState<Record<string, string> | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [busy, setBusy] = useState<"preview" | "print" | null>(null);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    getReprintData(row.no).then(d => {
      if (!alive) return;
      setLayout(d.layoutFile);
      try {
        setFields(d.fields ?? (d.source ? rebuild(baseCategory(row.category), d.source, user?.userCode) : null));
      } catch (e) { setErr(errorText(t, e)); }
    }, e => { if (alive) setErr(errorText(t, e)); });
    return () => { alive = false; };
  }, [row.no]);

  const ready = !!layout && !!fields;
  const noLayout = () => setErr(fillText(t("labelNoLayoutForCategory", "No label layout is defined for category \"{0}\"."), row.category));

  const onPreview = async () => {
    if (!ready) { noLayout(); return; }
    setBusy("preview"); setErr(null);
    try {
      setPreview(await mlPreviewLabel({ layoutFile: layout!, setting: lan.setting, fields: { ...fields!, Quantiy: count } }));
    } catch (e) {
      setErr(printErrorText(e, t));
    } finally { setBusy(null); }
  };

  const onPrint = async () => {
    if (!ready) { noLayout(); return; }
    setBusy("print"); setErr(null);
    try {
      await mlPrintLabel({ layoutFile: layout!, setting: lan.setting, fields: { ...fields!, Quantiy: count } });
    } catch (e) {
      setErr(printErrorText(e, t));
      setBusy(null);
      return;
    }
    try {
      await reprintPrintHistory(row.no, { deviceCode: lan.deviceCode, count, fields: fields!, clientPrinted: true });
      onPrinted(count);
    } catch (e) {
      setErr(errorText(t, e));
      setBusy(null);
    }
  };

  const error = err ?? lan.error;

  return (
    <div className="modal-overlay flex">
      <div className="modal-box w-242">
        <div className="modal-head">
          <span>{t("btnReprint", "Reprint")}</span>
          <span className="modal-close" onClick={onClose}>&times;</span>
        </div>
        <div className="modal-body">
          <ReadOnlyField label={t("colDocNo", "Doc No.")} value={row.docNo ?? ""} />
          <ReadOnlyField label={t("thItemCode", "Item Code")} value={row.itemCode ?? ""} />
          <ReadOnlyField label={t("thItemName", "Item Name")} value={row.itemName ?? ""} />
          <ReadOnlyField label={t("thLot", "Lot")} value={fields?.Lot ?? ""} />
          <ReadOnlyField label={t("thQty", "Qty")} value={fields?.ItemQty ?? fields?.TotalQty ?? String(row.qty ?? "")} />

          <LanPrinterField printers={lan.printers} deviceCode={lan.deviceCode} onChange={lan.setDeviceCode} engineOnline={lan.engineOnline} onRecheck={lan.recheck} />
          <div className="mfg">
            <label>{t("thLabelCount", "Label Print Count")}</label>
            <input type="number" min={1} value={count}
              onChange={e => setCount(Math.max(1, Number(e.target.value) || 1))} />
          </div>

          {error && <div className="text-rose-600 text-[12px] mt-2">{error}</div>}

          {preview && (
            <div className="mt-3 text-center">
              <img className="max-w-full shadow-[0_2px_10px_rgba(0,0,0,.18)]" src={preview} alt={t("btnPreview", "Preview")} />
            </div>
          )}
        </div>
        <div className="modal-actions">
          <button className="btn btn-outline" onClick={onClose}>{t("modalClose", "Close")}</button>
          <button className="btn btn-outline" onClick={onPreview} disabled={busy !== null || !lan.setting || !ready}>
            {busy === "preview" ? t("creating", "Creating...") : t("btnPreview", "Preview")}
          </button>
          <button className="btn btn-primary" onClick={onPrint} disabled={busy !== null || !lan.setting || !ready}>
            {busy === "print" ? t("printing", "Printing...") : t("btnPrint", "Print")}
          </button>
        </div>
      </div>
    </div>
  );
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div className="mfg">
      <label>{label}</label>
      <input className="bg-slate-50 text-slate-600" type="text" readOnly value={value} />
    </div>
  );
}
