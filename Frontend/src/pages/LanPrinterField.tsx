import type { Device } from "@/api/devices";
import { useT } from "@/i18n/LanguageContext";

export function LanPrinterField({
  printers, deviceCode, onChange,
}: {
  printers: Device[] | null;
  deviceCode: string;
  onChange: (deviceCode: string) => void;
}) {
  const t = useT();
  return (
    <>
      <div className="mfg">
        <label>{t("deviceTypePrinter", "Printer")}</label>
        <select value={deviceCode} onChange={e => onChange(e.target.value)} disabled={printers === null}>
          {printers === null
            ? <option value="">…</option>
            : printers.map(p => (
              <option key={p.deviceCode} value={p.deviceCode}>
                {p.deviceName} ({p.ipAddress})
              </option>
            ))}
        </select>
      </div>
      {printers?.length === 0 && (
        <div className="text-[11px] text-slate-500 mt-1.5">
          Device Master chưa có máy in nào khai địa chỉ IP.
        </div>
      )}
    </>
  );
}
