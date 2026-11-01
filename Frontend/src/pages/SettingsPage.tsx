import { useEffect, useState } from "react";
import { getSettings, saveSettings, type AppSettings } from "@/api/settings";
import { mlGetDriverNamesOrFallback } from "@/lib/mlEngine";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { errorText } from "@/lib/errorText";

export function SettingsPage() {
  const t = useT();
  const toast = useToast();
  const [form, setForm] = useState<AppSettings | null>(null);
  const [drivers, setDrivers] = useState<string[]>([]);
  const [driversLive, setDriversLive] = useState<boolean | null>(null);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    getSettings().then(setForm).catch(e => setErr(errorText(t, e)));
    mlGetDriverNamesOrFallback().then(r => { setDrivers(r.names); setDriversLive(r.live); });
  }, []);

  const set = <K extends keyof AppSettings>(k: K, v: AppSettings[K]) =>
    setForm(f => (f ? { ...f, [k]: v } : f));

  const onSave = async () => {
    if (!form) return;
    setSaving(true); setErr(null); setSaved(false);
    try { setForm(await saveSettings(form)); setSaved(true); toast.ok(t("toastSaved", "Saved")); }
    catch (e: any) { setErr(errorText(t, e)); toast.err(errorText(t, e)); }
    finally { setSaving(false); }
  };

  if (!form) {
    return <div className="page-info">{err ? <span className="text-rose-600">{err}</span> : "Loading…"}</div>;
  }

  return (
    <div className="pg on" id="pg-settings">
      <div className="search-box">
        <div className="search-row">
          <label>{t("settingsInvAggDefault", "Inventory Aggregation Default")}</label>
          <select className="w-40" value={form.invAggDefault}
            onChange={e => set("invAggDefault", e.target.value)}>
            <option value="lot">{t("aggLot", "By Lot")}</option>
            <option value="item">{t("aggItem", "By Item")}</option>
            <option value="location">{t("aggLocation", "By Location")}</option>
          </select>
        </div>

        <div className="search-row mt-2.5">
          <label>{t("settingsMasterPrinterDefault", "Master Label Default Printer")}</label>
          <PrinterSelect value={form.masterPrinterDefault} drivers={drivers}
            loading={driversLive === null}
            onChange={v => set("masterPrinterDefault", v)} />

          <label className="ml-3.5">{t("settingsSchedulePrinterDefault", "Schedule Label Default Printer")}</label>
          <PrinterSelect value={form.schedulePrinterDefault} drivers={drivers}
            loading={driversLive === null}
            onChange={v => set("schedulePrinterDefault", v)} />
        </div>

        {driversLive === false && (
          <div className="search-row mt-1">
            <span className="text-[11px] text-slate-500">
              Label Engine không phản hồi tại localhost:8080 — đang hiển thị danh sách driver mẫu.
              Engine phải cài trên chính máy đang mở trình duyệt.
            </span>
          </div>
        )}

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}
        {saved && !err && <div className="search-row text-emerald-600 text-[12px]">{t("settingsSaved", "Settings saved")}</div>}

        <div className="search-btns">
          <button className="btn btn-primary" onClick={onSave} disabled={saving}>
            {saving ? t("saving", "Saving…") : t("modalSave", "Save")}
          </button>
        </div>
      </div>
    </div>
  );
}

function PrinterSelect({ value, drivers, loading, onChange }: {
  value: string | null; drivers: string[]; loading: boolean; onChange: (v: string) => void;
}) {
  const options = value && !drivers.includes(value) ? [value, ...drivers] : drivers;
  if (loading) {
    return <select className="w-55" disabled><option>Loading printer drivers…</option></select>;
  }
  return (
    <select className="w-55" value={value ?? ""} onChange={e => onChange(e.target.value)}>
      <option value="">—</option>
      {options.map(d => <option key={d} value={d}>{d}</option>)}
    </select>
  );
}
