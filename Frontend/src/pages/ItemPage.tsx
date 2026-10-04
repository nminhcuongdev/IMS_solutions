import { useCallback, useEffect, useMemo, useState } from "react";
import { packingBadge } from "@/lib/badges";
import {
  createItem, deleteItem, exportItemsCsv, listItems, updateItem,
  type Item, type ItemWrite,
} from "@/api/items";
import { listItemGroups, type ItemGroup } from "@/api/itemGroups";
import { listDefaultValues, type DefaultValue } from "@/api/defaultValues";
import { CommonToolbar } from "@/lib/CommonToolbar";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { Field, ModalShell } from "@/lib/ModalShell";
import { SortTh, useSort } from "@/lib/SortableTh";
import { codeName, RefSelect, useRefOptions } from "@/lib/RefSelect";
import { listLocations } from "@/api/locations";
import { useLang, useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { useUrlNumber, useUrlSearch, useUrlPatch } from "@/lib/useUrlState";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav } from "@/lib/useShortcuts";
import { useRowSelect } from "@/lib/rowSelect";

type ModalMode = "new" | "edit" | "copy" | null;

export function ItemPage() {
  const t = useT();
  const toast = useToast();
  const ask = useConfirm();
  const { flash, flashCls } = useRowFlash();
  const { lang } = useLang();
  const [packings, setPackings] = useState<DefaultValue[]>([]);
  useEffect(() => { listDefaultValues("PackingType").then(setPackings).catch(() => setPackings([])); }, []);

  const [groupNames, setGroupNames] = useState<Map<string, string>>(new Map());
  useEffect(() => {
    listItemGroups({ pageSize: 200 })
      .then(r => setGroupNames(new Map(r.data.map(g => [g.itemGroupCode, g.itemGroupName]))))
      .catch(() => setGroupNames(new Map()));
  }, []);

  const packingName = (code: string | null) => {
    if (!code) return "—";
    const p = packings.find(x => x.valueCode === code);
    if (!p) return code;
    return (lang === "ja" ? p.nameLang2 : lang === "vi" ? p.nameLang3 : p.nameLang1) || p.nameLang1 || code;
  };

  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);
  const { q, draft: qDraft, setDraft: setQDraft } = useUrlSearch();
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState<ModalMode>(null);
  const [target, setTarget] = useState<Item | null>(null);

  const { sort, toggleSort } = useSort("item_code");
  const onSort = toggleSort;

  const { data, loading, error: err, reload } = useFetch(
    qs("items", { page, pageSize, q, sort: sort.value }),
    () => listItems({ page, pageSize, q, sort: sort.value }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("items"); await reload(); }, [reload]);

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => r.itemCode === [...selected][0]) ?? null : null,
    [selected, rows]);
  const toggle = (c: string) => setSelected(p => { const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s; });
  const toggleAll = () => setSelected(p => p.size === rows.length ? new Set() : new Set(rows.map(r => r.itemCode)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(r => r.itemCode), setSelected,
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
      for (const c of selected) await deleteItem(c);
      setSelected(new Set()); await load();
      toast.ok(`${t("toastDeleted", "Deleted")}: ${n}`);
    } catch (ex: any) { toast.err(ex?.message ?? String(ex)); }
        }}
        onExport={async () => {
          try { const { blob, filename } = await exportItemsCsv();
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
            <SortTh field="item_code" sort={sort} onSort={onSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={onSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="item_group_code" sort={sort} onSort={onSort}>{t("thItemCategory", "Item Category")}</SortTh>
            <SortTh field="uom_code" sort={sort} onSort={onSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="packing_type_code" sort={sort} onSort={onSort}>{t("thPackingType", "Packing Type")}</SortTh>
            <SortTh field="package_qty" sort={sort} onSort={onSort}>{t("thItemQty", "Standard Pack Qty")}</SortTh>
            <SortTh field="expiration_period" sort={sort} onSort={onSort}>{t("thExpirationPeriod", "Expiration Period (Days)")}</SortTh>
            <SortTh field="barcode" sort={sort} onSort={onSort}>{t("thEanCode", "EAN Code")}</SortTh>
            <SortTh field="surplus_point" sort={sort} onSort={onSort}>{t("thSurplusPoint", "Surplus Point")}</SortTh>
            <SortTh field="reorder_level_1" sort={sort} onSort={onSort}>{t("thReorderLevel1", "Reorder Level 1")}</SortTh>
            <SortTh field="reorder_level_2" sort={sort} onSort={onSort}>{t("thReorderLevel2", "Reorder Level 2")}</SortTh>
            <SortTh field="default_location_code" sort={sort} onSort={onSort}>{t("thDefaultLocation", "Default Location")}</SortTh>
            <SortTh field="is_active" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="remark_1" sort={sort} onSort={onSort}>{t("thRemark1", "Remark 1")}</SortTh>
            <SortTh field="remark_2" sort={sort} onSort={onSort}>{t("thRemark2", "Remark 2")}</SortTh>
            <SortTh field="remark_3" sort={sort} onSort={onSort}>{t("thRemark3", "Remark 3")}</SortTh>
            <SortTh field="remark_4" sort={sort} onSort={onSort}>{t("thRemark4", "Remark 4")}</SortTh>
            <SortTh field="remark_5" sort={sort} onSort={onSort}>{t("thRemark5", "Remark 5")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={19} />}
            {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={19} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
            {!loading && rows.map(r => (
              <tr key={r.itemCode} data-row-code={r.itemCode} {...rowProps(r.itemCode)} className={[flashCls(r.itemCode), r.itemCode === cursorCode ? "is-cursor" : ""].filter(Boolean).join(" ") || undefined}>
                <td className="chk-col"><input type="checkbox" className="row-chk"
                  checked={selected.has(r.itemCode)} onChange={() => toggle(r.itemCode)} /></td>
                <td>{r.itemCode}</td>
                <td className="text-left">{r.itemName}</td>
                <td className="text-left">{groupNames.get(r.itemGroupCode) ?? r.itemGroupCode}</td>
                <td>{r.uomCode ?? "—"}</td>
                <td className="text-left">{r.packingTypeCode
                  ? <span className={"badge " + packingBadge(r.packingTypeCode, packings)}>{packingName(r.packingTypeCode)}</span>
                  : "—"}</td>
                <td className="text-right">{r.packageQty}</td>
                <td className="text-right">{r.expirationPeriod ?? "—"}</td>
                <td className="text-right">{r.barcode ?? "—"}</td>
                <td className="text-right">{r.surplusPoint ?? "—"}</td>
                <td className="text-right">{r.reorderLevel1 ?? "—"}</td>
                <td className="text-right">{r.reorderLevel2 ?? "—"}</td>
                <td className="text-left">{r.defaultLocationCode ?? "—"}</td>
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
      {modal && <ItemModal mode={modal} source={target}
        onClose={() => setModal(null)} onSaved={async (savedCode) => {
          setModal(null); setSelected(new Set()); await load();
          flash(savedCode);
          toast.ok(t("toastSaved", "Saved"));
        }} />}
    </div>
  );
}

function ItemModal({ mode, source, onClose, onSaved }: {
  mode: "new" | "edit" | "copy"; source: Item | null;
  onClose: () => void; onSaved: (savedCode: string) => void;
}) {
  const t = useT();
  const isEdit = mode === "edit";
  const [form, setForm] = useState<ItemWrite>({
    itemCode: isEdit ? source!.itemCode : "",
    itemName: mode === "new" ? "" : source?.itemName ?? "",
    itemGroupCode: source?.itemGroupCode ?? "",
    uomCode: source?.uomCode ?? "",
    packingTypeCode: source?.packingTypeCode ?? "",
    packageQty: source?.packageQty ?? 1,
    expirationPeriod: source?.expirationPeriod ?? null,
    barcode: source?.barcode ?? "",
    surplusPoint: source?.surplusPoint ?? null,
    reorderLevel1: source?.reorderLevel1 ?? null,
    reorderLevel2: source?.reorderLevel2 ?? null,
    defaultLocationCode: source?.defaultLocationCode ?? "",
    isActive: source?.isActive ?? true,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "",
    remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "",
    remark5: source?.remark5 ?? "",
  });
  const [groups, setGroups] = useState<ItemGroup[]>([]);
  const [packings, setPackings] = useState<DefaultValue[]>([]);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => { listItemGroups({ pageSize: 200 }).then(r => setGroups(r.data)).catch(() => {}); }, []);
  useEffect(() => { listDefaultValues("PackingType").then(setPackings).catch(() => setPackings([])); }, []);

  const locations = useRefOptions(
    () => listLocations({ type: "Small", pageSize: 500 }),
    l => ({ value: l.locationCode, label: codeName(l.locationCode, l.locationName) }));

  const num = (v: string): number | null => v === "" ? null : Number(v);

  return (
    <ModalShell title={t("mItem", "Item Master")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          if (isEdit) await updateItem(source!.itemCode, form); else await createItem(form);
          onSaved(form.itemCode);
        }
        catch (ex: any) { setErr(ex?.message ?? String(ex)); } finally { setSaving(false); }
      }}>
      <Field label={t("thItemCode", "Item Code")}>
        <input maxLength={20} required value={form.itemCode} readOnly={isEdit}
          onChange={e => setForm(f => ({ ...f, itemCode: e.target.value }))}
          style={isEdit ? { background: "#f7fafc", color: "#718096" } : undefined} />
      </Field>
      <Field label={t("thItemName", "Item Name")}>
        <input maxLength={100} required value={form.itemName}
          onChange={e => setForm(f => ({ ...f, itemName: e.target.value }))} />
      </Field>
      <Field label={t("thItemGroupCode", "Item Group")}>
        <RefSelect required value={form.itemGroupCode}
          options={groups.map(g => ({ value: g.itemGroupCode, label: codeName(g.itemGroupCode, g.itemGroupName) }))}
          onChange={v => setForm(f => ({ ...f, itemGroupCode: v }))} />
      </Field>
      <Field label={t("thUnit", "Unit")}>
        <input maxLength={10} placeholder="pcs, set, rolls..." value={form.uomCode ?? ""}
          onChange={e => setForm(f => ({ ...f, uomCode: e.target.value }))} />
      </Field>
      <Field label={t("thPackingType", "Packing Type")}>
        <select value={form.packingTypeCode ?? ""}
          onChange={e => setForm(f => ({ ...f, packingTypeCode: e.target.value || null }))}>
          <option value="">—</option>
          {packings.map(p => <option key={p.valueCode} value={p.valueCode}>{p.nameLang1} ({p.valueCode})</option>)}
        </select>
      </Field>
      <Field label={t("thItemQty", "Package Qty")}>
        <input type="number" min={1} step="any" value={form.packageQty ?? 1}
          onChange={e => setForm(f => ({ ...f, packageQty: Number(e.target.value) }))} />
      </Field>
      <Field label={t("thExpirationPeriod", "Expiration Period")}>
        <input type="number" min={0} value={form.expirationPeriod ?? ""}
          onChange={e => setForm(f => ({ ...f, expirationPeriod: e.target.value === "" ? null : Number(e.target.value) }))} />
      </Field>
      <Field label={t("thEanCode", "EAN Code")}>
        <input maxLength={50} value={form.barcode ?? ""} onChange={e => setForm(f => ({ ...f, barcode: e.target.value }))} />
      </Field>
      <Field label={t("thSurplusPoint", "Surplus Point")}>
        <input type="number" min={0} step="any" value={form.surplusPoint ?? ""}
          onChange={e => setForm(f => ({ ...f, surplusPoint: num(e.target.value) }))} />
      </Field>
      <Field label={t("thReorderLevel1", "Reorder Level 1")}>
        <input type="number" min={0} step="any" value={form.reorderLevel1 ?? ""}
          onChange={e => setForm(f => ({ ...f, reorderLevel1: num(e.target.value) }))} />
      </Field>
      <Field label={t("thReorderLevel2", "Reorder Level 2")}>
        <input type="number" min={0} step="any" value={form.reorderLevel2 ?? ""}
          onChange={e => setForm(f => ({ ...f, reorderLevel2: num(e.target.value) }))} />
      </Field>
      <Field label={t("thDefaultLocation", "Default Location")}>
        <RefSelect value={form.defaultLocationCode ?? ""} options={locations}
          onChange={v => setForm(f => ({ ...f, defaultLocationCode: v || null }))} />
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
