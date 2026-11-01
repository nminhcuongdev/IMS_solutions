import { CsvButton, ImportButton, SearchButton } from "@/lib/Buttons";
import { useCallback, useState } from "react";
import {
  exportLanguageDataCsv, listLanguageData, saveLanguageData,
  type LanguageItem, type LanguageRow,
} from "@/api/languageData";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { SortTh, useSort } from "@/lib/SortableTh";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useUrlNumber, useUrlSearch, useUrlString, useUrlPatch } from "@/lib/useUrlState";
import { errorText } from "@/lib/errorText";

type LangCol = "en" | "ja" | "vi";
const LANG_COLS: { key: LangCol; labelKey: string; label: string }[] = [
  { key: "en", labelKey: "thEnglish", label: "English" },
  { key: "ja", labelKey: "thJapanese", label: "Japanese" },
  { key: "vi", labelKey: "thVietnamese", label: "Vietnamese" },
];

export function LanguageDataPage() {
  const t = useT();
  const toast = useToast();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);
  const { q, draft: qDraft, setDraft: setQDraft } = useUrlSearch();
  const [device, setDevice] = useUrlString("device");
  const [edits, setEdits] = useState<Record<string, string>>({});
  const [saving, setSaving] = useState(false);
  const [saveErr, setSaveErr] = useState<string | null>(null);

  const { sort, toggleSort } = useSort("label_key");
  const onSort = toggleSort;

  const { data, loading, error: loadErr, reload } = useFetch(
    qs("languageData", { page, pageSize, q, device, sort: sort.value }),
    () => listLanguageData({ page, pageSize, q, deviceScope: device || undefined, sort: sort.value }));
  const err = loadErr ?? saveErr;
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("languageData"); await reload(); }, [reload]);

  const cellKey = (r: LanguageRow, lang: LangCol) => `${r.labelKey}|${r.deviceScope ?? ""}|${lang}`;
  const valueOf = (r: LanguageRow, lang: LangCol) => edits[cellKey(r, lang)] ?? r[lang] ?? "";

  const onEdit = (r: LanguageRow, lang: LangCol, v: string) => {
    const k = cellKey(r, lang);
    setEdits(e => {
      const next = { ...e };
      if (v === (r[lang] ?? "")) delete next[k];
      else next[k] = v;
      return next;
    });
  };

  const onSave = async () => {
    const items: LanguageItem[] = Object.entries(edits).map(([k, text]) => {
      const [labelKey, scope, lang] = k.split("|");
      return { labelKey, languageCode: lang as LangCol, labelText: text, deviceScope: scope || null };
    });
    if (items.length === 0) return;
    setSaving(true); setSaveErr(null);
    try {
      await saveLanguageData(items);
      setEdits({});
      await load();
      toast.ok(t("toastSavedCount", "Saved {0} cell(s)").replace("{0}", String(items.length)));
    } catch (e: any) { setSaveErr(errorText(t, e)); toast.err(errorText(t, e)); }
    finally { setSaving(false); }
  };

  const onExport = async () => {
    try {
      const { blob, filename } = await exportLanguageDataCsv({ q, deviceScope: device || undefined, sort: sort.value });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (e: any) { setSaveErr(errorText(t, e)); }
  };

  const editCount = Object.keys(edits).length;

  return (
    <div className="pg on" id="pg-lang_data">
      <div className="search-box">
        <div className="search-row">
          <input className="w-70" type="text" placeholder={t("searchAnyText", "Search any text...")}
            value={qDraft}
            onChange={e => setQDraft(e.target.value)}
            onKeyDown={e => { if (e.key === "Enter") setPage(1); }} />
          <label className="ml-2">{t("saDeviceCol", "Device")}</label>
          <select className="w-27.5" value={device}
            onChange={e => { setPage(1); setDevice(e.target.value); }}>
            <option value="">{t("deviceAll", "All")}</option>
            <option value="PC">PC</option>
            <option value="HT">HT</option>
          </select>
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={() => setPage(1)} />
          <button className="btn btn-outline" onClick={onSave} disabled={saving || editCount === 0}>
            {saving ? t("saving", "Saving…")
              : editCount > 0 ? `${t("modalSave", "Save")} (${editCount})`
              : t("modalSave", "Save")}
          </button>
          <ImportButton disabled
            title={t("wipCsvImport", "CSV Import has no API endpoint yet.")} />
          <CsvButton onClick={onExport} />
        </div>
      </div>

      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <SortTh field="device_scope" sort={sort} onSort={onSort}>{t("saDeviceCol", "Device")}</SortTh>
            <SortTh field="label_key" sort={sort} onSort={onSort}>{t("thLanguageId", "Language ID")}</SortTh>
            {LANG_COLS.map(c => (
              <SortTh key={c.key} field={c.key} sort={sort} onSort={onSort}>{t(c.labelKey, c.label)}</SortTh>
            ))}
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={5} />}
            {!loading && rows.length === 0 && (
              <tr className="no-results-row"><td colSpan={5} className="text-center">{t("noResults", "No matching records found.")}</td></tr>
            )}
            {!loading && rows.map(r => (
              <tr key={`${r.labelKey}|${r.deviceScope ?? ""}`}>
                <td>{r.deviceScope ?? "Both"}</td>
                <td>{r.labelKey}</td>
                {LANG_COLS.map(c => (
                  <td key={c.key} className="text-left">
                    <input type="text" value={valueOf(r, c.key)}
                      onChange={e => onEdit(r, c.key, e.target.value)}
                      className={`w-full rounded-[3px] border px-1 py-0.5 focus:outline-none ${
                        edits[cellKey(r, c.key)] !== undefined
                          ? "border-brand-500 bg-brand-50"
                          : "border-transparent bg-transparent focus:border-slate-300 focus:bg-white"}`} />
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })} onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
    </div>
  );
}
