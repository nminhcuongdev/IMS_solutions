import { useCallback, useEffect, useMemo, useState } from "react";
import {
  getDbInfo, getDefaultMaster, getGeneral, getVisibility,
  saveDefaultMaster, saveGeneral, saveVisibility,
  type DbInfo, type DefaultMasterRow, type DefaultMasterType,
  type GeneralItem, type VisibilityRow,
} from "@/api/superAdmin";
import { listFactories, type Factory } from "@/api/factories";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { FUNCTION_LABEL_KEY, groupLabelKey } from "@/i18n/labelKeys";
import { SortTh } from "@/lib/SortableTh";
import { useLocalSort } from "@/lib/useLocalSort";


const HT_MODE_KEYS = [
  { key: "ht_label_mode_rcv", labelKey: "htReceiving", en: "Receiving" },
  { key: "ht_label_mode_shp", labelKey: "htShipping", en: "Shipping" },
  { key: "ht_label_mode_rtn", labelKey: "htReturns", en: "Returns" },
];

const LABEL_FILE_KEYS = [
  { key: "layout_file_rcv", labelKey: "saLabelReceiving", en: "Receiving Label" },
  { key: "layout_file_returns", labelKey: "saLabelReturns", en: "Returns Label" },
  { key: "layout_file_shp", labelKey: "saLabelShipping", en: "Shipping Label" },
  { key: "layout_file_m_location", labelKey: "mLocation", en: "Location Master" },
  { key: "layout_file_m_user", labelKey: "mUser", en: "User Master" },
  { key: "layout_file_m_partner", labelKey: "mPartner", en: "Business Partner Master" },
];

const DEF_MASTERS: { type: DefaultMasterType; labelKey: string; en: string }[] = [
  { type: "status", labelKey: "saDefMasterStatusTitle", en: "Master Status" },
  { type: "packing-type", labelKey: "thPackingType", en: "Packing Type" },
  { type: "hold-name", labelKey: "saDefMasterHoldTitle", en: "Hold Name" },
];

export function SuperAdminPage() {
  const t = useT();
  const toast = useToast();
  const [general, setGeneral] = useState<Record<string, string>>({});
  const [visibility, setVisibility] = useState<VisibilityRow[]>([]);
  const [defMasters, setDefMasters] = useState<Record<string, DefaultMasterRow[]>>({});
  const [factories, setFactories] = useState<Factory[]>([]);
  const [dbInfo, setDbInfo] = useState<DbInfo | null>(null);

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const load = useCallback(async () => {
    setLoading(true); setErr(null);
    try {
      const [g, v, db, f, ...defs] = await Promise.all([
        getGeneral(), getVisibility(), getDbInfo(),
        listFactories({ pageSize: 200 }),
        ...DEF_MASTERS.map(d => getDefaultMaster(d.type)),
      ]);
      setGeneral(Object.fromEntries(g.map(i => [i.configCode, i.configValue ?? ""])));
      setVisibility(v);
      setDbInfo(db);
      setFactories(f.data);
      setDefMasters(Object.fromEntries(DEF_MASTERS.map((d, i) => [d.type, defs[i] as DefaultMasterRow[]])));
    } catch (e: any) { setErr(e?.message ?? String(e)); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { load(); }, [load]);

  const setG = (k: string, v: string) => { setSaved(false); setGeneral(p => ({ ...p, [k]: v })); };

  const toggleVis = (targetType: string, targetCode: string) => {
    setSaved(false);
    setVisibility(p => p.map(r =>
      r.targetType === targetType && r.targetCode === targetCode
        ? { ...r, isVisible: !r.isVisible } : r));
  };

  const setDef = (type: string, code: string, field: keyof DefaultMasterRow, value: string) => {
    setSaved(false);
    setDefMasters(p => ({
      ...p,
      [type]: p[type].map(r => (r.code === code ? { ...r, [field]: value } : r)),
    }));
  };

  const onSave = async () => {
    setSaving(true); setErr(null); setSaved(false);
    try {
      const items: GeneralItem[] = Object.entries(general)
        .filter(([k]) => k !== "layout_path")
        .map(([configCode, configValue]) => ({ configCode, configValue, category: null }));
      await saveGeneral(items);

      await saveVisibility(visibility.map(r => ({
        targetType: r.targetType, targetCode: r.targetCode, isVisible: r.isVisible,
      })));

      for (const d of DEF_MASTERS) {
        const rows = defMasters[d.type];
        if (rows?.length) await saveDefaultMaster(d.type, rows);
      }

      setSaved(true);
      await load();
      toast.ok(t("toastSaved", "Saved"));
    } catch (e: any) { setErr(e?.message ?? String(e)); toast.err(e?.message ?? String(e)); }
    finally { setSaving(false); }
  };

  const visByDevice = useMemo(() => {
    const out: Record<string, VisibilityRow[]> = { PC: [], HT: [] };
    for (const r of visibility) (out[r.targetType] ??= []).push(r);
    return out;
  }, [visibility]);

  if (loading) return <div className="page-info">Loading…</div>;

  return (
    <div className="pg on" id="pg-super_admin">
      <div className="search-box">
        <Section labelKey="saFunctionVisibility" en="Function Visibility" first />
        <VisibilityTable rows={[...(visByDevice.PC ?? []), ...(visByDevice.HT ?? [])]}
          onToggle={toggleVis} />

        <Section labelKey="saFactorySettings" en="Factory Settings" />
        <div className="search-row mt-1.5">
          <label>{t("saDefaultFactory", "Default Factory")}</label>
          <select className="w-65" value={general.default_factory ?? ""}
            onChange={e => setG("default_factory", e.target.value)}>
            <option value="">—</option>
            {factories.map(f => (
              <option key={f.factoryCode} value={f.factoryCode}>
                {f.factoryCode} — {f.factoryName}
              </option>
            ))}
          </select>
        </div>

        <Section labelKey="htLabelModeSection" en="HT Label Issue Mode" />
        <div className="search-row mt-1.5">
          {HT_MODE_KEYS.map((m, i) => (
            <span key={m.key}>
              <label style={i > 0 ? { marginLeft: 14 } : undefined}>{t(m.labelKey, m.en)}</label>
              <select className="w-37.5" value={general[m.key] ?? "printNow"}
                onChange={e => setG(m.key, e.target.value)}>
                <option value="printNow">{t("htLabelModePrintNow", "Print Now")}</option>
                <option value="preIssued">{t("htLabelModePreIssued", "Pre-Issued")}</option>
              </select>
            </span>
          ))}
        </div>

        <Section labelKey="htReasonScopeSection" en="HT Shortage/Defect Reason Selection" />
        <div className="search-row mt-1.5">
          <label>{t("htReasonScopeMode", "Reason Selection Mode")}</label>
          <select className="w-45" value={general.ht_reason_scope ?? "byFunction"}
            onChange={e => setG("ht_reason_scope", e.target.value)}>
            <option value="byFunction">{t("htReasonScopeByFunction", "By Function")}</option>
            <option value="byItemGroup">{t("htReasonScopeByItemGroup", "By Item Group")}</option>
          </select>
        </div>

        <Section labelKey="saDbInfo" en="Database Connection Info (Read-only)" />
        <div className="search-row mt-1.5">
          <label>{t("saDbServer", "Server")}</label>
          <ReadOnly value={dbInfo?.server ?? ""} width={220} />
          <label className="ml-3.5">{t("saDbUser", "User Name")}</label>
          <ReadOnly value={dbInfo?.userName ?? ""} width={160} />
          <label className="ml-3.5">{t("saDbTable", "Table")}</label>
          <ReadOnly value={dbInfo?.table ?? ""} width={220} />
        </div>

        <Section labelKey="saDefaultMaster" en="Default Value Master" />
        <div className="search-row mt-1.5 flex-col items-start gap-4.5">
          {DEF_MASTERS.map(d => (
            <div className="w-full max-w-170" key={d.type}>
              <div className="text-[12px] font-bold text-slate-600 mb-1">
                {t(d.labelKey, d.en)}
              </div>
              <DefMasterTable rows={defMasters[d.type] ?? []}
                onChange={(code, f, v) => setDef(d.type, code, f, v)} />
            </div>
          ))}
        </div>

        <Section labelKey="saLayoutPath" en="Layout File Path (Fixed - Label Engine)" />
        <div className="search-row mt-1.5">
          <ReadOnly value={general.layout_path ?? ""} width={480} />
        </div>

        <Section labelKey="saLabelFileNames" en="Label File Names" />
        <div className="search-row mt-1.5 flex-col items-start gap-2">
          {LABEL_FILE_KEYS.map(f => (
            <div key={f.key}>
              <label className="w-42.5 inline-block">{t(f.labelKey, f.en)}</label>
              <input className="w-70" type="text" value={general[f.key] ?? ""}
                onChange={e => setG(f.key, e.target.value)} />
            </div>
          ))}
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}
        {saved && !err && (
          <div className="search-row text-emerald-600 text-[12px]">{t("settingsSaved", "Settings saved")}</div>
        )}

        <div className="search-btns">
          <button className="btn btn-primary" onClick={onSave} disabled={saving}>
            {saving ? t("saving", "Saving…") : t("modalSave", "Save")}
          </button>
        </div>
      </div>
    </div>
  );
}

function VisibilityTable({ rows, onToggle }: {
  rows: VisibilityRow[];
  onToggle: (device: string, code: string) => void;
}) {
  const t = useT();
  const group = (r: VisibilityRow) => (r.largeCode ? t(groupLabelKey(r.largeCode), r.largeName ?? r.largeCode) : "");
  const fn = (r: VisibilityRow) => t(FUNCTION_LABEL_KEY[r.targetCode] ?? r.targetCode, r.functionName);
  const { sorted, sort, onSort } = useLocalSort(rows, r => `${r.targetType}|${r.targetCode}`, {
    device: r => r.targetType,
    group,
    function: fn,
    visibility: r => r.isVisible,
  });
  return (
    <div className="tbl-wrap">
      <table className="tbl">
        <thead><tr>
          <SortTh field="device" sort={sort} onSort={onSort}>{t("saDeviceCol", "Device")}</SortTh>
          <SortTh field="group" sort={sort} onSort={onSort}>{t("saFunction", "Function")}</SortTh>
          <SortTh field="function" sort={sort} onSort={onSort}>{t("saFunction", "Function")}</SortTh>
          <SortTh field="visibility" sort={sort} onSort={onSort}>{t("saVisibility", "Visibility")}</SortTh>
        </tr></thead>
        <tbody>
          {sorted.map(r => (
            <tr key={`${r.targetType}|${r.targetCode}`}>
              <td>{r.targetType}</td>
              <td className="text-left">{group(r) || "—"}</td>
              <td className="text-left">{fn(r)}</td>
              <td>
                <button className={"perm-toggle" + (r.isVisible ? "" : " is-disable")}
                  onClick={() => onToggle(r.targetType, r.targetCode)}>
                  {r.isVisible ? t("saVisibilityShow", "Show") : t("saVisibilityHide", "Hide")}
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

type DefField = "nameLang1" | "nameLang2" | "nameLang3";

function DefMasterTable({ rows, onChange }: {
  rows: DefaultMasterRow[];
  onChange: (code: string, field: DefField, value: string) => void;
}) {
  const t = useT();
  const { sorted, sort, onSort } = useLocalSort(rows, r => r.code, {
    code: r => r.code,
    nameLang1: r => r.nameLang1,
    nameLang2: r => r.nameLang2,
    nameLang3: r => r.nameLang3,
  });
  return (
    <div className="tbl-wrap">
      <table className="tbl">
        <thead><tr>
          <SortTh field="code" sort={sort} onSort={onSort}>{t("saDefCode", "Code")}</SortTh>
          <SortTh field="nameLang1" sort={sort} onSort={onSort}>{t("thEnglish", "English")}</SortTh>
          <SortTh field="nameLang2" sort={sort} onSort={onSort}>{t("thJapanese", "Japanese")}</SortTh>
          <SortTh field="nameLang3" sort={sort} onSort={onSort}>{t("thVietnamese", "Vietnamese")}</SortTh>
        </tr></thead>
        <tbody>
          {sorted.map(r => (
            <tr key={r.code}>
              <td><ReadOnly value={r.code} width={130} /></td>
              {(["nameLang1", "nameLang2", "nameLang3"] as const).map(f => (
                <td key={f} className="text-left">
                  <input className="w-32.5" type="text" value={r[f]}
                    onChange={e => onChange(r.code, f, e.target.value)} />
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function Section({ labelKey, en, first }: { labelKey: string; en: string; first?: boolean }) {
  const t = useT();
  return (
    <div className="search-row" style={first ? undefined : { marginTop: 14 }}>
      <strong>{t(labelKey, en)}</strong>
    </div>
  );
}

function ReadOnly({ value, width }: { value: string; width: number }) {
  return (
    <input type="text" readOnly value={value}
      className="bg-slate-100 text-slate-600" style={{ width }} />
  );
}
