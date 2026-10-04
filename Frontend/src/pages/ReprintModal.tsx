import { useState } from "react";
import { BRAND } from "@/brand";
import { reprintPrintHistory, type PrintHistoryRow } from "@/api/history";
import { useAuth } from "@/auth/AuthContext";
import { mlPreviewLabel } from "@/lib/mlEngine";
import { printErrorText } from "@/lib/printErrors";
import { buildProductLabelQr, qrDate } from "@/lib/qrPayload";
import { useLanPrinters } from "@/lib/useLanPrinters";
import { useT } from "@/i18n/LanguageContext";
import { LanPrinterField } from "@/pages/LanPrinterField";

const LAYOUTS: Record<string, { file: string; qtyField: "ItemQty" | "TotalQty" }> = {
  Receiving: { file: "Layout_IMS/ReceivingQR_24dpmm.mllayx", qtyField: "ItemQty" },
  Returns:   { file: "Layout_IMS/ReturnsQR_24dpmm.mllayx",   qtyField: "ItemQty" },
  Shipping:  { file: "Layout_IMS/ShippingQR_24dpmm.mllayx",  qtyField: "TotalQty" },
};

function baseCategory(category: string) {
  return category.replace(/Reprint$/, "");
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
  const [preview, setPreview] = useState<string | null>(null);
  const [busy, setBusy] = useState<"preview" | "print" | null>(null);
  const [err, setErr] = useState<string | null>(null);

  const layout = LAYOUTS[baseCategory(row.category)];

  const labelFields = (l: NonNullable<typeof layout>): Record<string, string> => ({
    PONo: row.docNo ?? "",
    SupplierCode: "",
    SupplierName: "",
    "Item Code": row.itemCode ?? "",
    "Item Name": row.itemName ?? row.itemCode ?? "",
    Unit: row.uomCode ?? "",
    Lot: "",
    FactoryCode: "FAC-01",
    FactoryName: BRAND.company,
    Address: "",
    ScheduledDate: row.scheduledDate ?? "",
    ReprintFlag: "REPRINT",
    [l.qtyField]: String(row.qty ?? 0),
    QR: buildProductLabelQr({
      qty: row.qty ?? 0,
      productionDate: qrDate(row.scheduledDate),
      itemCode: row.itemCode ?? "",
      itemName: row.itemName,
      userCode: user?.userCode,
    }),
  });

  const noLayout = () => setErr(`Chưa có layout cho phân loại "${row.category}".`);

  const onPreview = async () => {
    if (!layout) { noLayout(); return; }
    setBusy("preview"); setErr(null);
    try {
      setPreview(await mlPreviewLabel({
        layoutFile: layout.file, setting: lan.setting, fields: { ...labelFields(layout), Quantiy: count },
      }));
    } catch (e) {
      setErr(printErrorText(e, t));
    } finally { setBusy(null); }
  };

  const onPrint = async () => {
    if (!layout) { noLayout(); return; }
    setBusy("print"); setErr(null);
    try {
      await reprintPrintHistory(row.no, { deviceCode: lan.deviceCode, count, fields: labelFields(layout) });
      onPrinted(count);
    } catch (e: any) {
      setErr(e?.message ?? String(e));
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

          <LanPrinterField printers={lan.printers} deviceCode={lan.deviceCode} onChange={lan.setDeviceCode} />
          <div className="mfg">
            <label>{t("thLabelCount", "Label Print Count")}</label>
            <input type="number" min={1} value={count}
              onChange={e => setCount(Math.max(1, Number(e.target.value) || 1))} />
          </div>

          {error && <div className="text-rose-600 text-[12px] mt-2">{error}</div>}

          {preview && (
            <div className="mt-3 text-center">
              <img className="max-w-full shadow-[0_2px_10px_rgba(0,0,0,.18)]" src={preview} alt="Label preview" />
            </div>
          )}
        </div>
        <div className="modal-actions">
          <button className="btn btn-outline" onClick={onClose}>{t("modalClose", "Close")}</button>
          <button className="btn btn-outline" onClick={onPreview} disabled={busy !== null || !lan.setting}>
            {busy === "preview" ? "Đang tạo…" : "Preview"}
          </button>
          <button className="btn btn-primary" onClick={onPrint} disabled={busy !== null || !lan.setting}>
            {busy === "print" ? "Đang in…" : t("btnPrint", "Print")}
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
