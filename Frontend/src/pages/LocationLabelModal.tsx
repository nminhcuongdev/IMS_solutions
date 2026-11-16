import { useState } from "react";
import type { Location } from "@/api/locations";
import { useT } from "@/i18n/LanguageContext";
import { mlPreviewLabel, mlPrintLabel } from "@/lib/mlEngine";
import { printErrorText } from "@/lib/printErrors";
import { buildLocationLabelQr } from "@/lib/qrPayload";
import { useLanPrinters } from "@/lib/useLanPrinters";
import { LanPrinterField } from "@/pages/LanPrinterField";

export function LocationLabelModal({
  rows, onClose, onPrinted,
}: {
  rows: Location[];
  onClose: () => void;
  onPrinted: (labels: number) => void;
}) {
  const t = useT();
  const { printers, deviceCode, setDeviceCode, setting, error: printersError, engineOnline, recheck } = useLanPrinters();
  const [count, setCount] = useState("1");
  const [index, setIndex] = useState(0);
  const [preview, setPreview] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const copies = Number.parseInt(count, 10);
  const countOk = Number.isInteger(copies) && copies >= 1;
  const current = rows[Math.min(index, rows.length - 1)];

  const fieldsOf = (r: Location, n: number) => ({
    Quantiy: n,
    LocationCodeL: r.largeCode,
    LocationCodeM: r.middleCode ?? "",
    LocationCodeS: r.smallCode ?? "",
    LocationName: r.locationName,
    Remark1: r.remark1 ?? "", Remark2: r.remark2 ?? "", Remark3: r.remark3 ?? "",
    Remark4: r.remark4 ?? "", Remark5: r.remark5 ?? "",
    QR: buildLocationLabelQr(r.locationCode, r.locationName),
  });

  const onPreview = async () => {
    if (!current) return;
    setBusy(true); setErr(null);
    try {
      setPreview(await mlPreviewLabel({ layoutFile: LAYOUT, setting, fields: fieldsOf(current, 1) }));
    } catch (e) { setErr(printErrorText(e, t)); } finally { setBusy(false); }
  };

  const onPrint = async () => {
    if (!countOk) { setErr(t("alertInvalidQty", "Please enter a valid quantity.")); return; }
    setBusy(true); setErr(null);
    let done = 0;
    try {
      for (const r of rows) {
        await mlPrintLabel({ layoutFile: LAYOUT, setting, fields: fieldsOf(r, copies) });
        done++;
      }
      onPrinted(done * copies);
    } catch (e: any) {
      setErr(`${printErrorText(e, t)} — ${t("mlPrintResult", "Print result")} OK: ${done}/${rows.length}`);
    } finally { setBusy(false); }
  };

  return (
    <div className="modal-overlay flex">
      <div className="modal-box w-242">
        <div className="modal-head">
          <span>{t("issueLabelTitle", "Issue Label")}</span>
          <span className="modal-close" onClick={onClose}>&times;</span>
        </div>
        <div className="modal-body">
          <div className="mfg">
            <label>{t("thSelectedCount", "Selected")}</label>
            <input className="bg-slate-50 text-slate-600" type="text" readOnly value={rows.length} />
          </div>

          <LanPrinterField printers={printers} deviceCode={deviceCode} onChange={setDeviceCode} engineOnline={engineOnline} onRecheck={recheck} />

          <div className="mfg">
            <label>{t("thLabelCount", "Label Print Count")}</label>
            <input type="number" min={1} value={count} onChange={e => setCount(e.target.value)} />
          </div>

          {current && (
            <div className="text-[12px] text-slate-600 mt-1.5">
              {current.locationCode} · {current.locationName}
              {setting && <> · <code>{setting}</code></>}
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
          {preview && rows.length > 1 && (
            <>
              <button className="btn btn-outline" disabled={index <= 0 || busy}
                onClick={() => { setIndex(i => i - 1); setPreview(null); }}>&lt;</button>
              <span className="self-center text-[13px] text-slate-600 my-0 mx-1.5">
                {index + 1} / {rows.length}
              </span>
              <button className="btn btn-outline" disabled={index >= rows.length - 1 || busy}
                onClick={() => { setIndex(i => i + 1); setPreview(null); }}>&gt;</button>
            </>
          )}
          <button className="btn btn-cancel" onClick={onClose}>{t("modalCancel", "Cancel")}</button>
          <button className="btn btn-outline" onClick={onPreview} disabled={busy || !setting}>
            {t("btnPreview", "Preview")}
          </button>
          <button className="btn btn-primary" onClick={onPrint} disabled={busy || !setting || !countOk}>
            {t("labelPrint", "Label")}
          </button>
        </div>
      </div>
    </div>
  );
}

const LAYOUT = "Layout_IMS/LocationQR_24dpmm.mllayx";
