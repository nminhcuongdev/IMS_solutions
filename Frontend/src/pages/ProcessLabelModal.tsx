import { useEffect, useState } from "react";
import type { Process } from "@/api/processes";
import { getGeneral } from "@/api/superAdmin";
import { useT } from "@/i18n/LanguageContext";
import { mlPreviewLabel, mlPrintLabel } from "@/lib/mlEngine";
import { printErrorText } from "@/lib/printErrors";
import { useLanPrinters } from "@/lib/useLanPrinters";
import { LanPrinterField } from "./LanPrinterField";

export function ProcessLabelModal({ rows, onClose, onPrinted }: { rows: Process[]; onClose: () => void; onPrinted: (n: number) => void }) {
  const t = useT();
  const { printers, deviceCode, setDeviceCode, setting, error: printerError, engineOnline, recheck } = useLanPrinters();
  const [layout, setLayout] = useState("Layout_IMS/Process.mllayx");
  const [count, setCount] = useState(1);
  const [index, setIndex] = useState(0);
  const [preview, setPreview] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  useEffect(() => { getGeneral().then(items => {
    const name = items.find(x => x.configCode === "layout_file_m_process")?.configValue?.trim();
    if (name) setLayout(name.includes("/") ? name : `Layout_IMS/${name}`);
  }).catch(() => {}); }, []);
  const fields = (row: Process, quantity: number) => ({
    ProcessCode: row.processCode,
    ProcessName: row.processName,
    Quantity: quantity,
    QR: `?pr_cd=${encodeURIComponent(row.processCode)}&pr_name=${encodeURIComponent(row.processName)}`,
  });
  const current = rows[index];
  const showPreview = async () => {
    if (!current) return;
    setBusy(true); setError(null);
    try { setPreview(await mlPreviewLabel({ layoutFile: layout, setting, fields: fields(current, 1) })); }
    catch (e) { setError(printErrorText(e, t)); }
    finally { setBusy(false); }
  };
  const print = async () => {
    if (!Number.isInteger(count) || count < 1) return setError(t("alertInvalidQty", "Please enter a valid quantity."));
    setBusy(true); setError(null);
    let done = 0;
    try {
      for (const row of rows) { await mlPrintLabel({ layoutFile: layout, setting, fields: fields(row, count) }); done++; }
      onPrinted(done * count);
    } catch (e) { setError(printErrorText(e, t)); }
    finally { setBusy(false); }
  };
  return <div className="modal-overlay"><div className="modal-box" style={{ width: 968 }}>
    <div className="modal-head"><span>{t("issueLabelTitle", "Issue Label")}</span><span className="modal-close" onClick={onClose}>&times;</span></div>
    <div className="modal-body">
      <div className="mfg"><label>{t("thSelectedCount", "Selected")}</label><input readOnly value={rows.length} /></div>
      <LanPrinterField printers={printers} deviceCode={deviceCode} onChange={setDeviceCode} engineOnline={engineOnline} onRecheck={recheck} />
      <div className="mfg"><label>{t("thLabelCount", "Label Print Count")}</label><input type="number" min={1} value={count} onChange={e => setCount(Number(e.target.value))} /></div>
      {current && <div>{current.processCode} · {current.processName}</div>}
      {(error || printerError) && <div className="page-info" style={{ color: "#DC3545" }}>{error || printerError}</div>}
      {preview && <div style={{ textAlign: "center" }}><img src={preview} alt={t("btnPreview", "Preview")} style={{ maxWidth: "100%" }} /></div>}
    </div>
    <div className="modal-actions">
      {preview && rows.length > 1 && <><button className="btn btn-outline" disabled={index === 0} onClick={() => { setIndex(index - 1); setPreview(null); }}>&lt;</button><span>{index + 1} / {rows.length}</span><button className="btn btn-outline" disabled={index === rows.length - 1} onClick={() => { setIndex(index + 1); setPreview(null); }}>&gt;</button></>}
      <button className="btn btn-cancel" onClick={onClose}>{t("modalCancel", "Cancel")}</button>
      <button className="btn btn-outline" onClick={showPreview} disabled={busy || !setting}>{t("btnPreview", "Preview")}</button>
      <button className="btn btn-primary" onClick={print} disabled={busy || !setting || count < 1}>{t("labelPrint", "Label")}</button>
    </div>
  </div></div>;
}
