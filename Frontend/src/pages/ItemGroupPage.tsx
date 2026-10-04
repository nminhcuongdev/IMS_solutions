import { useCallback, useMemo, useState } from "react";
import {
  createItemGroup, deleteItemGroup, exportItemGroupsCsv, listItemGroups, updateItemGroup,
  type ItemGroup, type ItemGroupWrite, ITEM_GROUP_CATEGORY_LABELS,
} from "@/api/itemGroups";
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

type ModalMode = "new" | "edit" | "copy" | null;

function catLabel(cat: number, t: (k: string, f: string) => string): string {
  const hit = ITEM_GROUP_CATEGORY_LABELS[cat];
  return hit ? t(hit[0], hit[1]) : String(cat);
}

export function ItemGroupPage() {
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
  const [target, setTarget] = useState<ItemGroup | null>(null);

  const { sort, toggleSort } = useSort("item_group_code");
  const onSort = toggleSort;

  const { data, loading, error: err, reload } = useFetch(
    qs("itemGroups", { page, pageSize, q, sort: sort.value }),
    () => listItemGroups({ page, pageSize, q, sort: sort.value }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("itemGroups"); await reload(); }, [reload]);

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => r.itemGroupCode === [...selected][0]) ?? null : null,
    [selected, rows]);
  const toggle = (c: string) => setSelected(p => { const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s; });
  const toggleAll = () => setSelected(p => p.size === rows.length ? new Set() : new Set(rows.map(r => r.itemGroupCode)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(r => r.itemGroupCode), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);

  return (
    <div className="pg on">
      <CommonToolbar
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
      for (const c of selected) await deleteItemGroup(c);
      setSelected(new Set()); await load();
      toast.ok(`${t("toastDeleted", "Deleted")}: ${n}`);
    } catch (ex: any) { toast.err(ex?.message ?? String(ex)); }
        }}
        onExport={async () => {
          try { const { blob, filename } = await exportItemGroupsCsv();
            const u = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = u; a.download = filename; a.click(); URL.revokeObjectURL(u);
          } catch (ex: any) { toast.err(ex?.message ?? String(ex)); }
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
            <SortTh field="item_group_code" sort={sort} onSort={onSort}>{t("thItemGroupCode", "Group Code")}</SortTh>
            <SortTh field="item_group_name" sort={sort} onSort={onSort}>{t("thItemGroupName", "Group Name")}</SortTh>
            <SortTh field="category" sort={sort} onSort={onSort}>{t("thCategory", "Category")}</SortTh>
            <SortTh field="is_active" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="remark_1" sort={sort} onSort={onSort}>{t("thRemark1", "Remark 1")}</SortTh>
            <SortTh field="remark_2" sort={sort} onSort={onSort}>{t("thRemark2", "Remark 2")}</SortTh>
            <SortTh field="remark_3" sort={sort} onSort={onSort}>{t("thRemark3", "Remark 3")}</SortTh>
            <SortTh field="remark_4" sort={sort} onSort={onSort}>{t("thRemark4", "Remark 4")}</SortTh>
            <SortTh field="remark_5" sort={sort} onSort={onSort}>{t("thRemark5", "Remark 5")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={10} />}
            {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={10} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
            {!loading && rows.map(r => (
              <tr key={r.itemGroupCode} data-row-code={r.itemGroupCode} {...rowProps(r.itemGroupCode)} className={[flashCls(r.itemGroupCode), r.itemGroupCode === cursorCode ? "is-cursor" : ""].filter(Boolean).join(" ") || undefined}>
                <td className="chk-col"><input type="checkbox" className="row-chk"
                  checked={selected.has(r.itemGroupCode)} onChange={() => toggle(r.itemGroupCode)} /></td>
                <td>{r.itemGroupCode}</td>
                <td className="text-left">{r.itemGroupName}</td>
                <td className="text-left">{catLabel(r.category, t)}</td>
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
      {modal && <ItemGroupModal mode={modal} source={target}
        onClose={() => setModal(null)} onSaved={async (savedCode) => {
          setModal(null); setSelected(new Set()); await load();
          flash(savedCode);
          toast.ok(t("toastSaved", "Saved"));
        }} />}
    </div>
  );
}

function ItemGroupModal({ mode, source, onClose, onSaved }: {
  mode: "new" | "edit" | "copy"; source: ItemGroup | null;
  onClose: () => void; onSaved: (savedCode: string) => void;
}) {
  const t = useT();
  const isEdit = mode === "edit";
  const [form, setForm] = useState<ItemGroupWrite>({
    itemGroupCode: isEdit ? source!.itemGroupCode : "",
    itemGroupName: mode === "new" ? "" : source?.itemGroupName ?? "",
    category: source?.category ?? 1,
    isActive: source?.isActive ?? true,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "",
    remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "",
    remark5: source?.remark5 ?? "",
  });
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  return (
    <ModalShell title={t("mItemGroup", "Item Group Master")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          if (isEdit) await updateItemGroup(source!.itemGroupCode, form); else await createItemGroup(form);
          onSaved(form.itemGroupCode);
        }
        catch (ex: any) { setErr(ex?.message ?? String(ex)); } finally { setSaving(false); }
      }}>
      <Field label={t("thItemGroupCode", "Item Group Code")}>
        <input maxLength={20} required value={form.itemGroupCode} readOnly={isEdit}
          onChange={e => setForm(f => ({ ...f, itemGroupCode: e.target.value }))}
          style={isEdit ? { background: "#f7fafc", color: "#718096" } : undefined} />
      </Field>
      <Field label={t("thItemGroupName", "Item Group Name")}>
        <input maxLength={100} required value={form.itemGroupName}
          onChange={e => setForm(f => ({ ...f, itemGroupName: e.target.value }))} />
      </Field>
      <Field label={t("thCategory", "Category")}>
        <select value={form.category} onChange={e => setForm(f => ({ ...f, category: Number(e.target.value) }))}>
          {Object.keys(ITEM_GROUP_CATEGORY_LABELS).map(k =>
            <option key={k} value={k}>{catLabel(Number(k), t)}</option>)}
        </select>
      </Field>
      <Field label={t("thStatus", "Status")}>
        <select value={form.isActive ? "1" : "0"} onChange={e => setForm(f => ({ ...f, isActive: e.target.value === "1" }))}>
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
