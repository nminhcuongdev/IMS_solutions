import { useCallback, useMemo, useState } from "react";
import {
  createFactory, deleteFactory, exportFactoriesCsv, listFactories, updateFactory,
  type Factory, type FactoryWrite,
} from "@/api/factories";
import { CommonToolbar } from "@/lib/CommonToolbar";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { Field, ModalShell } from "@/lib/ModalShell";
import { SortTh, useSort } from "@/lib/SortableTh";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { useUrlNumber, useUrlSearch, useUrlPatch } from "@/lib/useUrlState";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav } from "@/lib/useShortcuts";
import { useRowSelect } from "@/lib/rowSelect";
import { useAuth } from "@/auth/AuthContext";
import { errorText } from "@/lib/errorText";

type ModalMode = "new" | "edit" | "copy" | null;

export function FactoryPage() {
  const t = useT();
  const toast = useToast();
  const ask = useConfirm();
  const { flash, flashCls } = useRowFlash();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);
  const { q, draft: qDraft, setDraft: setQDraft } = useUrlSearch();
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState<ModalMode>(null);
  const [target, setTarget] = useState<Factory | null>(null);

  const { sort, toggleSort } = useSort("factory_code");
  const onSort = toggleSort;

  const { data, loading, error: err, reload } = useFetch(
    qs("factories", { page, pageSize, q, sort: sort.value }),
    () => listFactories({ page, pageSize, q, sort: sort.value }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("factories"); await reload(); }, [reload]);

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => r.factoryCode === [...selected][0]) ?? null : null,
    [selected, rows]);
  const toggle = (c: string) => setSelected(p => { const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s; });
  const toggleAll = () => setSelected(p => p.size === rows.length ? new Set() : new Set(rows.map(r => r.factoryCode)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(r => r.factoryCode), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);

  return (
    <div className="pg on">
      <CommonToolbar fn="m_factory"
        searchDraft={qDraft} onSearchDraftChange={setQDraft}
        onSearch={() => setPage(1)}
        onNew={() => { setModal("new"); setTarget(null); }}
        onEdit={() => selectedRow && (setModal("edit"), setTarget(selectedRow))}
        onCopy={() => selectedRow && (setModal("copy"), setTarget(selectedRow))}
        onDelete={async () => {
          if (selected.size === 0) return toast.err(t("alertSelectDelete", "Please select at least one row to delete."));
          if (!await ask({
            message: t("confirmDelete", "Delete the selected row(s)?"),
            detail: `${t("thSelectedCount", "Selected")}: ${selected.size}`,
          })) return;
          const n = selected.size;
    try {
      for (const c of selected) await deleteFactory(c);
      setSelected(new Set()); await load();
      toast.ok(`${t("toastDeleted", "Deleted")}: ${n}`);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        onExport={async () => {
          try { const { blob, filename } = await exportFactoriesCsv();
            const u = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = u; a.download = filename; a.click(); URL.revokeObjectURL(u);
          } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        disableEdit={!selectedRow} disableCopy={!selectedRow} disableDelete={selected.size === 0}
        extraKeys={rowNav} />
      {err && <div className="page-info text-rose-600">{err}</div>}
      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <th className="chk-col">
              <input type="checkbox" className="row-chk"
                checked={selected.size > 0 && selected.size === rows.length}
                ref={el => { if (el) el.indeterminate = selected.size > 0 && selected.size < rows.length; }}
                onChange={toggleAll} />
            </th>
            <SortTh field="factory_code" sort={sort} onSort={onSort}>{t("thFactoryCode", "Factory Code")}</SortTh>
            <SortTh field="factory_name" sort={sort} onSort={onSort}>{t("thFactoryName", "Factory Name")}</SortTh>
            <SortTh field="country" sort={sort} onSort={onSort}>{t("thCountry", "Country")}</SortTh>
            <SortTh field="address" sort={sort} onSort={onSort}>{t("thAddress", "Address")}</SortTh>
            <SortTh field="is_default" sort={sort} onSort={onSort}>{t("thDefault", "Default")}</SortTh>
            <SortTh field="is_active" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="remark_1" sort={sort} onSort={onSort}>{t("thRemark1", "Remark 1")}</SortTh>
            <SortTh field="remark_2" sort={sort} onSort={onSort}>{t("thRemark2", "Remark 2")}</SortTh>
            <SortTh field="remark_3" sort={sort} onSort={onSort}>{t("thRemark3", "Remark 3")}</SortTh>
            <SortTh field="remark_4" sort={sort} onSort={onSort}>{t("thRemark4", "Remark 4")}</SortTh>
            <SortTh field="remark_5" sort={sort} onSort={onSort}>{t("thRemark5", "Remark 5")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={12} />}
            {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={12} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
            {!loading && rows.map(r => (
              <tr key={r.factoryCode} data-row-code={r.factoryCode} {...rowProps(r.factoryCode)} className={[flashCls(r.factoryCode), r.factoryCode === cursorCode ? "is-cursor" : ""].filter(Boolean).join(" ") || undefined}>
                <td className="chk-col"><input type="checkbox" className="row-chk"
                  checked={selected.has(r.factoryCode)} onChange={() => toggle(r.factoryCode)} /></td>
                <td>{r.factoryCode}</td>
                <td className="text-left">{r.factoryName}</td>
                <td className="text-left">{r.country ?? "—"}</td>
                <td className="text-left">{r.address ?? "—"}</td>
                <td>{r.isDefault ? "✓" : "—"}</td>
                <td className="text-left"><span className={"badge " + (r.isActive ? "badge-ok" : "badge-ng")}>{r.isActive ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}</span></td>
                <td className="text-left">{r.remark1 ?? "—"}</td>
                <td className="text-left">{r.remark2 ?? "—"}</td>
                <td className="text-left">{r.remark3 ?? "—"}</td>
                <td className="text-left">{r.remark4 ?? "—"}</td>
                <td className="text-left">{r.remark5 ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <PageFooter total={total} page={page} pageSize={pageSize} onGoPage={p => setPage(p, { push: true })} onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
      {modal && <FactoryModal mode={modal} source={target}
        onClose={() => setModal(null)} onSaved={async (savedCode) => {
          setModal(null); setSelected(new Set()); await load();
          flash(savedCode);
          toast.ok(t("toastSaved", "Saved"));
        }} />}
    </div>
  );
}

function FactoryModal({ mode, source, onClose, onSaved }: {
  mode: "new" | "edit" | "copy"; source: Factory | null;
  onClose: () => void; onSaved: (savedCode: string) => void;
}) {
  const t = useT();
  const { user } = useAuth();
  const canSetDefault = !!user?.isSuperAdmin;
  const isEdit = mode === "edit";
  const [form, setForm] = useState<FactoryWrite>({
    factoryCode: isEdit ? source!.factoryCode : "",
    factoryName: mode === "new" ? "" : source?.factoryName ?? "",
    country: source?.country ?? "", address: source?.address ?? "",
    isActive: source?.isActive ?? true, isDefault: source?.isDefault ?? false,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "",
    remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "",
    remark5: source?.remark5 ?? "",
  });
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  return (
    <ModalShell title={t("mFactory", "Factory Master")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          if (isEdit) await updateFactory(source!.factoryCode, form); else await createFactory(form);
          onSaved(form.factoryCode);
        }
        catch (ex: any) { setErr(errorText(t, ex)); } finally { setSaving(false); }
      }}>
      <Field label={t("thFactoryCode", "Factory Code")}>
        <input maxLength={20} required value={form.factoryCode} readOnly={isEdit}
          onChange={e => setForm(f => ({ ...f, factoryCode: e.target.value }))}
          style={isEdit ? { background: "#f7fafc", color: "#718096" } : undefined} />
      </Field>
      <Field label={t("thFactoryName", "Factory Name")}>
        <input maxLength={100} required value={form.factoryName}
          onChange={e => setForm(f => ({ ...f, factoryName: e.target.value }))} />
      </Field>
      <Field label={t("thCountry", "Country")}>
        <input maxLength={50} value={form.country ?? ""} onChange={e => setForm(f => ({ ...f, country: e.target.value }))} />
      </Field>
      <Field label={t("thAddress", "Address")}>
        <input maxLength={500} value={form.address ?? ""} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} />
      </Field>
      <Field label={t("saDefaultFactory", "Default Factory")}>
        <select value={form.isDefault ? "1" : "0"} disabled={!canSetDefault}
          title={canSetDefault ? undefined : t("hintSuperAdminOnly", "Super Admin only")}
          style={canSetDefault ? undefined : { background: "#f7fafc", color: "#718096" }}
          onChange={e => setForm(f => ({ ...f, isDefault: e.target.value === "1" }))}>
          <option value="0">{t("thNo", "No")}</option><option value="1">{t("thYes", "Yes")}</option>
        </select>
      </Field>
      <Field label={t("thStatus", "Status")}>
        <select value={form.isActive ? "1" : "0"}
          onChange={e => setForm(f => ({ ...f, isActive: e.target.value === "1" }))}>
          <option value="1">{t("statusEnable", "Enable")}</option><option value="0">{t("statusDisable", "Disable")}</option>
        </select>
      </Field>
      {(["remark1", "remark2", "remark3", "remark4", "remark5"] as const).map((k, i) => (
        <Field key={k} label={t(`thRemark${i + 1}`, `Remark ${i + 1}`)}>
          <input maxLength={200} value={(form[k] ?? "") as string}
            onChange={e => setForm(f => ({ ...f, [k]: e.target.value }))} />
        </Field>
      ))}
    </ModalShell>
  );
}
