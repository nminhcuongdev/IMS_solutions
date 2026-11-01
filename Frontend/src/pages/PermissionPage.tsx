import { useCallback, useEffect, useMemo, useState } from "react";
import {
  listPermissions, savePermissions,
  type PermissionItem, type PermissionRow,
} from "@/api/permissions";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { SortTh } from "@/lib/SortableTh";
import { useLocalSort } from "@/lib/useLocalSort";
import { FUNCTION_LABEL_KEY, groupLabelKey } from "@/i18n/labelKeys";
import { errorText } from "@/lib/errorText";

type RoleKey = "general" | "worker";

const ACTION_LABEL: Record<string, [string, string]> = {
  view:   ["permActionView", "Open"],
  new:    ["btnNew", "New"],
  edit:   ["btnEdit", "Edit"],
  delete: ["btnDelete", "Delete"],
  import: ["btnImport", "Import"],
  export: ["btnExport", "Export"],
  label:  ["permActionLabel", "Label"],
  reset:  ["rsdReset", "Reset"],
};

function actionLabel(action: string, t: (k: string, f: string) => string): string {
  const hit = ACTION_LABEL[action];
  return hit ? t(hit[0], hit[1]) : action;
}

export function PermissionPage() {
  const t = useT();
  const toast = useToast();
  const [rows, setRows] = useState<PermissionRow[]>([]);
  const [draft, setDraft] = useState<Record<string, { general: boolean; worker: boolean }>>({});
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [savedAt, setSavedAt] = useState<number | null>(null);

  const load = useCallback(async () => {
    setLoading(true); setErr(null);
    try {
      const data = await listPermissions();
      setRows(data);
      setDraft(Object.fromEntries(data.map(r => [r.permissionCode, { general: r.general, worker: r.worker }])));
    } catch (e: any) { setErr(errorText(t, e)); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { load(); }, [load]);

  const toggle = (code: string, role: RoleKey) => {
    setSavedAt(null);
    setDraft(d => ({ ...d, [code]: { ...d[code], [role]: !d[code][role] } }));
  };

  const changed = useMemo(() => {
    const items: PermissionItem[] = [];
    for (const r of rows) {
      const d = draft[r.permissionCode];
      if (!d) continue;
      if (d.general !== r.general) items.push({ roleCode: "General", permissionCode: r.permissionCode, isAllowed: d.general });
      if (d.worker !== r.worker) items.push({ roleCode: "Worker", permissionCode: r.permissionCode, isAllowed: d.worker });
    }
    return items;
  }, [rows, draft]);

  const { sorted, sort, onSort } = useLocalSort(rows, r => r.permissionCode, {
    tab: r => (r.largeCode ? t(groupLabelKey(r.largeCode), r.largeName || r.largeCode) : ""),
    function: r => t(FUNCTION_LABEL_KEY[r.functionCode] ?? r.functionCode, r.functionName),
    detail: r => actionLabel(r.action, t),
    general: r => draft[r.permissionCode]?.general ?? false,
    worker: r => draft[r.permissionCode]?.worker ?? false,
  });

  const onSave = async () => {
    if (changed.length === 0) return;
    const n = changed.length;
    setSaving(true); setErr(null);
    try {
      const data = await savePermissions(changed);
      setRows(data);
      setDraft(Object.fromEntries(data.map(r => [r.permissionCode, { general: r.general, worker: r.worker }])));
      setSavedAt(Date.now());
      toast.ok(`${t("toastSaved", "Saved")}: ${n}`);
    } catch (e: any) {
      setDraft(Object.fromEntries(rows.map(r => [r.permissionCode, { general: r.general, worker: r.worker }])));
      setErr(errorText(t, e));
      toast.err(`${t("permSaveFailed", "Could not save — changes were reverted.")} ${errorText(t, e)}`);
    }
    finally { setSaving(false); }
  };

  const pending = useMemo(() => {
    const set = new Set<string>();
    for (const i of changed) set.add(`${i.permissionCode}|${i.roleCode}`);
    return set;
  }, [changed]);

  useEffect(() => {
    if (changed.length === 0) return;
    const warn = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = ""; };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [changed.length]);

  return (
    <div className="pg on" id="pg-m_permission">
      <div className="search-box">
        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}
        {savedAt && !err && (
          <div className="search-row text-emerald-600 text-[12px]">{t("permSaved", "Permissions saved.")}</div>
        )}
        <div className="search-btns">
          <button className="btn btn-primary" onClick={onSave} disabled={saving || changed.length === 0}>
            {saving ? t("saving", "Saving…")
              : changed.length > 0 ? `${t("modalSave", "Save")} (${changed.length})`
              : t("modalSave", "Save")}
          </button>
        </div>
      </div>

      <div className="tbl-wrap">
        <table className="tbl" id="tbl-m_permission">
          <thead><tr>
            <SortTh field="tab" sort={sort} onSort={onSort}>{t("thTab", "Tab")}</SortTh>
            <SortTh field="function" sort={sort} onSort={onSort}>{t("saFunction", "Function")}</SortTh>
            <SortTh field="detail" sort={sort} onSort={onSort}>
              {t("thFunctionDetail", "Function Detail")}
            </SortTh>
            <SortTh field="general" sort={sort} onSort={onSort}>{t("permGeneral", "General")}</SortTh>
            <SortTh field="worker" sort={sort} onSort={onSort}>{t("permWorker", "Worker")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={5} />}
            {!loading && rows.length === 0 && (
              <tr className="no-results-row"><td colSpan={5} className="text-center">{t("noResults", "No matching records found.")}</td></tr>
            )}
            {!loading && sorted.map(r => {
              const d = draft[r.permissionCode];
              return (
                <tr key={r.permissionCode}>
                  <td className="text-left">{r.largeCode ? t(groupLabelKey(r.largeCode), r.largeName || r.largeCode) : "—"}</td>
                  <td className="text-left">{t(FUNCTION_LABEL_KEY[r.functionCode] ?? r.functionCode, r.functionName)}</td>
                  <td className="text-left">{actionLabel(r.action, t)}</td>
                  <td><PermToggle on={d?.general ?? false} pending={pending.has(`${r.permissionCode}|General`)}
                    onClick={() => toggle(r.permissionCode, "general")} /></td>
                  <td><PermToggle on={d?.worker ?? false} pending={pending.has(`${r.permissionCode}|Worker`)}
                    onClick={() => toggle(r.permissionCode, "worker")} /></td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function PermToggle({ on, pending, onClick }: { on: boolean; pending?: boolean; onClick: () => void }) {
  const t = useT();
  return (
    <button
      className={"perm-toggle" + (on ? "" : " is-disable") + (pending ? " is-pending" : "")}
      title={pending ? t("permUnsaved", "Not saved yet") : undefined}
      onClick={onClick}>
      {on ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}
    </button>
  );
}
