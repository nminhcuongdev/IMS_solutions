import { useCallback, useEffect, useMemo, useState } from "react";
import {
  createLocation, deleteLocation, exportLocationsCsv, listLocations, updateLocation,
  type Location, type LocationWrite, type LocationType,
} from "@/api/locations";
import { CommonToolbar } from "@/lib/CommonToolbar";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { Field, ModalShell } from "@/lib/ModalShell";
import { SortTh, useSort } from "@/lib/SortableTh";
import { codeName, RefSelect, useRefOptions } from "@/lib/RefSelect";
import { listZones } from "@/api/zones";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { useUrlNumber, useUrlSearch, useUrlPatch } from "@/lib/useUrlState";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav } from "@/lib/useShortcuts";
import { useRowSelect } from "@/lib/rowSelect";
import { BTN_ICON } from "@/layout/icons";
import { LocationLabelModal } from "@/pages/LocationLabelModal";
import { errorText } from "@/lib/errorText";

type ModalMode = "new" | "edit" | "copy" | null;

export function LocationPage() {
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
  const [target, setTarget] = useState<Location | null>(null);
  const [labelOpen, setLabelOpen] = useState(false);

  const { sort, toggleSort } = useSort("location_code");
  const onSort = toggleSort;

  const { data, loading, error: err, reload } = useFetch(
    qs("locations", { page, pageSize, q, sort: sort.value }),
    () => listLocations({ page, pageSize, q, sort: sort.value }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("locations"); await reload(); }, [reload]);

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => r.locationCode === [...selected][0]) ?? null : null,
    [selected, rows]);
  const toggle = (c: string) => setSelected(p => { const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s; });
  const toggleAll = () => setSelected(p => p.size === rows.length ? new Set() : new Set(rows.map(r => r.locationCode)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(r => r.locationCode), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);

  return (
    <div className="pg on">
      <CommonToolbar fn="m_location"
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
      for (const c of selected) await deleteLocation(c);
      setSelected(new Set()); await load();
      toast.ok(`${t("toastDeleted", "Deleted")}: ${n}`);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        onExport={async () => {
          try { const { blob, filename } = await exportLocationsCsv();
            const u = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = u; a.download = filename; a.click(); URL.revokeObjectURL(u);
          } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        disableEdit={!selectedRow} disableCopy={!selectedRow} disableDelete={selected.size === 0}
        extraKeys={rowNav}
        labelButton={
          <button className="btn btn-print" disabled={selected.size === 0}
            onClick={() => setLabelOpen(true)}>
            <img className="icn-img" src={BTN_ICON.print} alt="" /> <span>{t("labelPrint", "Label")}</span>
          </button>
        } />
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
            <SortTh field="location_type" sort={sort} onSort={onSort}>{t("thLocType", "Classification")}</SortTh>
            <SortTh field="location_code" sort={sort} onSort={onSort}>{t("thLocCode", "Location Code")}</SortTh>
            <SortTh field="large_code" sort={sort} onSort={onSort}>{t("thLocLarge", "Large")}</SortTh>
            <SortTh field="middle_code" sort={sort} onSort={onSort}>{t("thLocMedium", "Middle")}</SortTh>
            <SortTh field="small_code" sort={sort} onSort={onSort}>{t("thLocSmall", "Small")}</SortTh>
            <SortTh field="location_name" sort={sort} onSort={onSort}>{t("thLocName", "Location Name")}</SortTh>
            <SortTh field="zone_code" sort={sort} onSort={onSort}>{t("thZoneCode", "Zone Code")}</SortTh>
            <SortTh field="is_active" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="remark_1" sort={sort} onSort={onSort}>{t("thRemark1", "Remark 1")}</SortTh>
            <SortTh field="remark_2" sort={sort} onSort={onSort}>{t("thRemark2", "Remark 2")}</SortTh>
            <SortTh field="remark_3" sort={sort} onSort={onSort}>{t("thRemark3", "Remark 3")}</SortTh>
            <SortTh field="remark_4" sort={sort} onSort={onSort}>{t("thRemark4", "Remark 4")}</SortTh>
            <SortTh field="remark_5" sort={sort} onSort={onSort}>{t("thRemark5", "Remark 5")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={14} />}
            {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={14} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
            {!loading && rows.map(r => (
              <tr key={r.locationCode} data-row-code={r.locationCode} {...rowProps(r.locationCode)} className={[flashCls(r.locationCode), r.locationCode === cursorCode ? "is-cursor" : ""].filter(Boolean).join(" ") || undefined}>
                <td className="chk-col"><input type="checkbox" className="row-chk"
                  checked={selected.has(r.locationCode)} onChange={() => toggle(r.locationCode)} /></td>
                <td>{t("locType" + r.locationType, r.locationType)}</td>
                <td>{r.locationCode}</td>
                <td>{r.largeCode}</td>
                <td>{r.middleCode ?? ""}</td>
                <td>{r.smallCode ?? ""}</td>
                <td className="text-left">{r.locationName}</td>
                <td>{r.zoneCode ?? "—"}</td>
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
      {labelOpen && (
        <LocationLabelModal
          rows={rows.filter(r => selected.has(r.locationCode))}
          onClose={() => setLabelOpen(false)}
          onPrinted={n => { setLabelOpen(false); toast.ok(`${t("labelIssueDone", "Labels issued")}: ${n}`); }} />
      )}
      {modal && <LocationModal mode={modal} source={target}
        onClose={() => setModal(null)} onSaved={async (savedCode) => {
          setModal(null); setSelected(new Set()); await load();
          flash(savedCode);
          toast.ok(t("toastSaved", "Saved"));
        }} />}
    </div>
  );
}

function LocationModal({ mode, source, onClose, onSaved }: {
  mode: "new" | "edit" | "copy"; source: Location | null;
  onClose: () => void; onSaved: (savedCode: string) => void;
}) {
  const t = useT();
  const isEdit = mode === "edit";
  const initialType: LocationType = source?.locationType ?? "Large";
  const initialInput = source?.locationType === "Large"
    ? source.largeCode
    : source?.locationType === "Middle"
      ? (source.middleCode ?? "")
      : (source?.smallCode ?? "");

  const [form, setForm] = useState<LocationWrite>({
    locationType: initialType,
    inputCode: mode === "new" ? "" : (mode === "copy" ? "" : initialInput),
    locationName: mode === "new" ? "" : source?.locationName ?? "",
    parentLocationCode: source?.parentLocationCode ?? "",
    zoneCode: source?.zoneCode ?? "",
    isActive: source?.isActive ?? true,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "",
    remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "",
    remark5: source?.remark5 ?? "",
  });
  const [parents, setParents] = useState<Location[]>([]);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const zones = useRefOptions(
    () => listZones({ pageSize: 500 }),
    z => ({ value: z.zoneCode, label: codeName(z.zoneCode, z.zoneName) }));

  const prefix = form.locationType === "Large" || !form.parentLocationCode
    ? "" : form.parentLocationCode + "_";
  const inputBudget = Math.max(1, 20 - prefix.length);
  const fullCode = prefix + form.inputCode;

  useEffect(() => {
    if (form.locationType === "Large") { setParents([]); return; }
    const parentType: LocationType = form.locationType === "Middle" ? "Large" : "Middle";
    listLocations({ type: parentType, pageSize: 200 })
      .then(r => setParents(r.data))
      .catch(() => setParents([]));
  }, [form.locationType]);

  return (
    <ModalShell title={t("mLocation", "Location Master")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          const saved = isEdit ? await updateLocation(source!.locationCode, form) : await createLocation(form);
          onSaved(saved.locationCode);
        }
        catch (ex: any) { setErr(errorText(t, ex)); } finally { setSaving(false); }
      }}>
      <Field label={t("thType", "Type")}>
        <select value={form.locationType} disabled={isEdit}
          onChange={e => setForm(f => ({ ...f, locationType: e.target.value as LocationType, parentLocationCode: "" }))}>
          <option value="Large">Large (大)</option>
          <option value="Middle">Middle (中)</option>
          <option value="Small">Small (小)</option>
        </select>
      </Field>
      {form.locationType !== "Large" && (
        <Field label={`${t("thParent", "Parent")} (${form.locationType === "Middle" ? "Large" : "Middle"})`}>
          <RefSelect required disabled={isEdit} value={form.parentLocationCode ?? ""}
            options={parents.map(p => ({ value: p.locationCode, label: codeName(p.locationCode, p.locationName) }))}
            onChange={v => setForm(f => ({ ...f, parentLocationCode: v }))} />
        </Field>
      )}
      <Field label={t("thLocCode", "Location Code")}>
        <input maxLength={inputBudget} required value={form.inputCode} readOnly={isEdit}
          onChange={e => setForm(f => ({ ...f, inputCode: e.target.value }))}
          style={isEdit ? { background: "#f7fafc", color: "#718096" } : undefined} />
        {!isEdit && (
          <div className="text-[11px] text-slate-500 mt-1">
            {fullCode || "—"} ({fullCode.length}/20)
          </div>
        )}
      </Field>
      <Field label={t("thLocName", "Location Name")}>
        <input maxLength={100} required value={form.locationName}
          onChange={e => setForm(f => ({ ...f, locationName: e.target.value }))} />
      </Field>
      <Field label={t("thZoneCode", "Zone Code")}>
        <RefSelect value={form.zoneCode ?? ""} options={zones}
          onChange={v => setForm(f => ({ ...f, zoneCode: v }))} />
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
