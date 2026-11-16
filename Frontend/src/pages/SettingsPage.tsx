import { useScreenCan } from "@/auth/PermissionContext";
import { useEffect, useState } from "react";
import { getSettings, saveSettings, type AppSettings } from "@/api/settings";
import { mlGetDriverStatus } from "@/lib/mlEngine";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { errorText } from "@/lib/errorText";

export function SettingsPage() { const can = useScreenCan();
  const t = useT();
  const toast = useToast();
  const [form, setForm] = useState<AppSettings | null>(null);
  const [drivers, setDrivers] = useState<string[]>([]);
  const [driversLive, setDriversLive] = useState<boolean | null>(null);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [loaded, setLoaded] = useState<AppSettings | null>(null);

  const checkEngine = async () => {
    setDriversLive(null);
    const r = await mlGetDriverStatus();
    setDrivers(r.names); setDriversLive(r.live);
  };

  useEffect(() => {
    getSettings().then(f => { setForm(f); setLoaded(f); }).catch(e => setErr(errorText(t, e)));
    checkEngine();
  }, []);

  const set = <K extends keyof AppSettings>(k: K, v: AppSettings[K]) =>
    setForm(f => (f ? { ...f, [k]: v } : f));

  const onSave = async () => {
    if (!form || !loaded) return;
    setSaving(true); setErr(null); setSaved(false);
    try { const body = driversLive ? form : { ...form, masterPrinterDefault: loaded.masterPrinterDefault, schedulePrinterDefault: loaded.schedulePrinterDefault }; const result = await saveSettings(body); setForm(result); setLoaded(result); setSaved(true); toast.ok(t("toastSaved", "Saved")); }
    catch (e: any) { setErr(errorText(t, e)); toast.err(errorText(t, e)); }
    finally { setSaving(false); }
  };

  if (!form) {
    return <div className="page-info">{err ? <span className="text-rose-600">{err}</span> : t("loading", "Loading...")}</div>;
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
            live={driversLive} onRefresh={checkEngine}
            onChange={v => set("masterPrinterDefault", v)} />

          <label className="ml-3.5">{t("settingsSchedulePrinterDefault", "Schedule Label Default Printer")}</label>
          <PrinterSelect value={form.schedulePrinterDefault} drivers={drivers}
            live={driversLive} onRefresh={checkEngine}
            onChange={v => set("schedulePrinterDefault", v)} />
        </div>
        {driversLive === false && (
          <div className="search-row mt-1">
            <span className="text-[11px] text-slate-500">
              {t("settingsEngineOffline", "Cannot connect to the label print engine. Start it on this PC and refresh.")}
            </span>
          </div>
        )}

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}
        {saved && !err && <div className="search-row text-emerald-600 text-[12px]">{t("settingsSaved", "Settings saved")}</div>}

        <div className="search-btns">
          {can("Save") && <button className="btn btn-primary" onClick={onSave} disabled={saving}>
            {saving ? t("saving", "Saving…") : t("modalSave", "Save")}
          </button>}
        </div>
      </div>
    </div>
  );
}

function PrinterSelect({ value, drivers, live, onChange, onRefresh }: {
  value: string | null; drivers: string[]; live: boolean | null;
  onChange: (v: string) => void; onRefresh: () => void;
}) {
  const t = useT();
  const missing = !!value && !drivers.includes(value);
  return <div className="flex items-center gap-2">
    <select className="w-55" value={value ?? ""} disabled={live !== true} onChange={e => onChange(e.target.value)}>
      {live !== true ? <option value={value ?? ""}>{live === null ? t("loading", "Loading...") : value || "-"}</option> : <>
        {missing && <option value={value!}>{value} {t("printerNotFound", "(not found)")}</option>}
        <option value="">-</option>
        {drivers.map(d => <option key={d} value={d}>{d}</option>)}
      </>}
    </select>
    <button type="button" className="btn btn-outline" onClick={onRefresh} aria-label={t("btnRefresh", "Refresh")}>{t("btnRefresh", "Refresh")}</button>
  </div>;
}
