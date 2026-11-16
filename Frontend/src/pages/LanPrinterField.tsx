import type { PrinterOption } from "@/lib/useLanPrinters";
import { useT } from "@/i18n/LanguageContext";

export function LanPrinterField({ printers, deviceCode, onChange, engineOnline, onRecheck }: {
  printers: PrinterOption[] | null; deviceCode: string; onChange: (value: string) => void;
  engineOnline: boolean | null; onRecheck: () => void;
}) {
  const t = useT();
  const current = printers?.find(p => p.code === deviceCode);
  return <div className="mfg"><label>{t("deviceTypePrinter", "Printer")}</label>
    <div className="flex items-center gap-2"><select className="flex-1" value={deviceCode} onChange={e => onChange(e.target.value)} disabled={!printers || engineOnline !== true}>
      {engineOnline !== true || printers === null ? <option value={deviceCode}>{current?.label ?? (deviceCode.replace(/^DRV:/, "") || "-")}</option> : (["lan", "driver"] as const).map(kind => <optgroup key={kind} label={kind === "lan" ? t("printerGroupLan", "LAN printers") : t("printerGroupDriver", "Printers installed on this PC")}>
        {printers?.filter(p => p.kind === kind).map(p => <option key={p.code} value={p.code}>{p.label}</option>)}
      </optgroup>)}
    </select><button type="button" className="btn btn-outline" onClick={onRecheck}>{t("btnRefresh", "Refresh")}</button></div>
    {engineOnline === false && <div className="mt-1 text-xs text-rose-600">PRN-003 - {t("PRN-003", "Cannot connect to the label print engine. Check that it is running on this PC.")}</div>}
    {engineOnline === true && printers?.length === 0 && <div className="mt-1 text-xs text-slate-500">{t("printerNoneFound", "No printer found. Register a LAN printer or install a label printer driver.")}</div>}
  </div>;
}
