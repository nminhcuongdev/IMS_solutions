import { fmtDate, fmtDateTime } from "@/lib/dateFormat";
import { useCallback, useMemo, useState } from "react";
import {
  changePickingStatus, createPickingPlan, exportPickingScheduleCsv,
  listPickingSchedule,
  type PickingPlanWrite,
  type PickingScheduleQuery, type PickingStatusKey,
} from "@/api/picking";
import { listItems } from "@/api/items";
import { listLocations } from "@/api/locations";
import { CsvButton, ImportButton, NewButton, SearchButton, StatusChangeButton } from "@/lib/Buttons";
import { ScheduleImportModal } from "@/pages/ScheduleImportModal";
import { StatusChangeModal } from "@/pages/StatusChangeModal";
import { defaultDateRange, DateRange, useDateDraft } from "@/lib/DateRange";
import { Field, ModalShell } from "@/lib/ModalShell";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { codeName, RefSelect, useRefOptions } from "@/lib/RefSelect";
import { useRowSelect } from "@/lib/rowSelect";
import { SortTh, useSort } from "@/lib/SortableTh";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav, useShortcuts } from "@/lib/useShortcuts";
import { useUrlBool, useUrlNumber, useUrlPatch, useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";


const COLS = 15;

const STATUS_KEYS: PickingStatusKey[] = ["statusNotPicked", "statusPicked", "statusCancelled"];

const STATUS_FALLBACK: Record<PickingStatusKey, string> = {
  statusNotPicked: "Not Picked",
  statusPicked: "Picked",
  statusCancelled: "Cancelled",
};

const STATUS_BADGE: Record<PickingStatusKey, string> = {
  statusNotPicked: "badge-info",
  statusPicked: "badge-ok",
  statusCancelled: "badge-cancel",
};

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

type Modal = "new" | "status" | "import" | null;

export function PickingSchedulePage() {
  const t = useT();
  const toast = useToast();
  const { flash, flashCls } = useRowFlash();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);

  const [defaultRange] = useState(defaultDateRange);
  const [dateOn] = useUrlBool("dates", true);
  const [dateFrom] = useUrlString("from", defaultRange.from);
  const [dateTo] = useUrlString("to", defaultRange.to);
  const dates = useDateDraft(dateOn, dateFrom, dateTo, defaultRange);
  const [status, setStatus] = useUrlString("status");
  const [itemCode] = useUrlString("ic");
  const [itemName] = useUrlString("in");
  const [icDraft, setIcDraft] = useState(itemCode);
  const [inDraft, setInDraft] = useState(itemName);

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState<Modal>(null);

  const { sort, toggleSort } = useSort("scheduled_date");

  const filters: PickingScheduleQuery = useMemo(() => ({
    dateFrom: dateOn && dateFrom ? dateFrom : undefined,
    dateTo: dateOn && dateTo ? dateTo : undefined,
    status: status || undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    sort: sort.value,
  }), [dateOn, dateFrom, dateTo, itemCode, itemName, status, sort.value]);

  const { data, loading, error: err, reload, refresh } = useFetch(
    qs("pickingSchedule", { ...filters, page, pageSize }),
    () => listPickingSchedule({ ...filters, page, pageSize }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("pickingSchedule"); await reload(); }, [reload]);

  const toggle = (c: string) => setSelected(p => {
    const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s;
  });
  const toggleAll = () => setSelected(p =>
    p.size === rows.length ? new Set() : new Set(rows.map(rowKey)));

  const selectedRows = rows.filter(r => selected.has(rowKey(r)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(rowKey), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);
  useShortcuts(rowNav);

  const onExport = async () => {
    try {
      const { blob, filename } = await exportPickingScheduleCsv(filters);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (ex: any) { toast.err(ex?.message ?? String(ex)); }
  };

  const search = () => {
    patch({ ...dates.patch(), page: null, ic: icDraft || null, "in": inDraft || null }); refresh();
  };
  const onEnter = (e: { key: string }) => { if (e.key === "Enter") search(); };

  return (
    <div className="pg on" id="pg-pick_import">
      <div className="search-box">
        <div className="search-row">
          <DateRange {...dates.props}
            label={t("thSchedDate", "Scheduled Date")} />
          <label className="ml-3.5">{t("thItemCode", "Item Code")}</label>
          <input className="w-35" type="text" value={icDraft}
            onChange={e => setIcDraft(e.target.value)} onKeyDown={onEnter} />
          <label className="ml-2">{t("thItemName", "Item Name")}</label>
          <input className="w-40" type="text" value={inDraft}
            onChange={e => setInDraft(e.target.value)} onKeyDown={onEnter} />
          <label className="ml-3.5">{t("status", "Status")}</label>
          <select className="w-32.5" value={status}
            onChange={e => { setPage(1); setStatus(e.target.value); }}>
            <option value="">{t("statusAll", "All")}</option>
            {STATUS_KEYS.map(k => (
              <option key={k} value={k}>{t(k, STATUS_FALLBACK[k])}</option>
            ))}
          </select>
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
          <NewButton onClick={() => setModal("new")} />
          <StatusChangeButton onClick={() => selected.size
            ? setModal("status")
            : toast.err(t("alertSelectAtLeastOne", "Please select at least one row."))} />
          <ImportButton onClick={() => setModal("import")} />
          <CsvButton onClick={onExport} />
        </div>
      </div>

      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <th className="chk-col">
              <input type="checkbox" className="row-chk"
                checked={selected.size > 0 && selected.size === rows.length}
                ref={el => { if (el) el.indeterminate = selected.size > 0 && selected.size < rows.length; }}
                onChange={toggleAll} />
            </th>
            <SortTh field="no" sort={sort} onSort={toggleSort}>{t("thNo", "No")}</SortTh>
            <SortTh field="so_no" sort={sort} onSort={toggleSort}>{t("thSlipNo", "SO No")}</SortTh>
            <SortTh field="so_branch_no" sort={sort} onSort={toggleSort}>{t("thSerialNo", "Serial No")}</SortTh>
            <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="location_code" sort={sort} onSort={toggleSort}>{t("thLocation", "Location")}</SortTh>
            <SortTh field="qty" sort={sort} onSort={toggleSort}>{t("thPlannedQty", "Planned")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="picked_qty" sort={sort} onSort={toggleSort}>{t("thActualQty", "Picked")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="scheduled_date" sort={sort} onSort={toggleSort}>{t("thSchedDate", "Scheduled Date")}</SortTh>
            <SortTh field="status" sort={sort} onSort={toggleSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="created_at" sort={sort} onSort={toggleSort}>{t("thRegisteredDate", "Registered Date")}</SortTh>
            <SortTh field="created_by" sort={sort} onSort={toggleSort}>{t("thRegisteredUser", "Registered User")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={COLS} />}
            {!loading && rows.length === 0 && (
              <tr className="no-results-row">
                <td colSpan={COLS} className="text-center">{t("noResults", "No matching records found.")}</td>
              </tr>
            )}
            {!loading && rows.map((r, i) => (
              <tr key={`${r.soNo}/${r.serialNo}`} data-row-code={rowKey(r)} {...rowProps(rowKey(r))}
                className={[flashCls(r.soNo), rowKey(r) === cursorCode ? "is-cursor" : ""]
                  .filter(Boolean).join(" ") || undefined}>
                <td className="chk-col">
                  <input type="checkbox" className="row-chk"
                    checked={selected.has(rowKey(r))} onChange={() => toggle(rowKey(r))} />
                </td>
                <td>{(page - 1) * pageSize + i + 1}</td>
                <td>{r.soNo}</td>
                <td>{r.serialNo}</td>
                <td>{r.itemCode}</td>
                <td className="text-left">{dash(r.itemName)}</td>
                <td>{dash(r.locationCode)}</td>
                <td className="text-right">{r.plannedQty}</td>
                <td>{dash(r.unit)}</td>
                <td className="text-right">{r.pickedQty}</td>
                <td>{dash(r.unit)}</td>
                <td>{fmtDate(r.scheduledDate)}</td>
                <td>
                  <span className={"badge " + STATUS_BADGE[r.status]}>
                    {t(r.status, STATUS_FALLBACK[r.status])}
                  </span>
                </td>
                <td>{fmtDateTime(r.createdAt)}</td>
                <td>{dash(r.createdBy)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })}
        onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />

      {modal === "new" && (
        <NewPickingModal
          onClose={() => setModal(null)}
          onSaved={async savedNo => {
            setModal(null); setSelected(new Set()); await load();
            flash(savedNo); toast.ok(t("toastSaved", "Saved"));
          }} />
      )}

      {modal === "status" && (
        <StatusChangeModal
          count={selectedRows.length}
          done={{ key: "statusPicked", fallback: "Picked" }}
          notDone={{ key: "statusNotPicked", fallback: "Not Picked" }}
          onApply={action => changePickingStatus(
            selectedRows.map(r => ({ soNo: r.soNo, serialNo: r.serialNo })), action)}
          onClose={() => setModal(null)}
          onDone={async ({ updated, skipped }) => {
            setModal(null); setSelected(new Set()); await load();
            if (skipped.length) {
              toast.err(`${t("toastSkipped", "Skipped")}: ${skipped.join(", ")}`);
            }
            toast.ok(`${t("toastUpdated", "Updated")}: ${updated}`);
          }} />
      )}

      {modal === "import" && (
        <ScheduleImportModal kind="picking-schedule"
          onClose={() => setModal(null)}
          onDone={async n => {
            setModal(null);
            await load();
            toast.ok(`${t("IMP-003", "Import complete")}: ${n}`);
          }} />
      )}
    </div>
  );
}


function NewPickingModal({ onClose, onSaved }: {
  onClose: () => void; onSaved: (soNo: string) => void;
}) {
  const t = useT();
  const [form, setForm] = useState<PickingPlanWrite>({
    soNo: "", serialNo: "01", itemCode: "", locationCode: "",
    plannedQty: 1, scheduledDate: new Date().toISOString().slice(0, 10),
  });
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const items = useRefOptions(
    () => listItems({ pageSize: 200 }),
    i => ({ value: i.itemCode, label: codeName(i.itemCode, i.itemName) }));
  const locations = useRefOptions(
    () => listLocations({ pageSize: 200 }),
    l => ({ value: l.locationCode, label: codeName(l.locationCode, l.locationName) }));

  return (
    <ModalShell title={t("newPickingTitle", "New Picking Schedule")} onClose={onClose}
      saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try { await createPickingPlan(form); onSaved(form.soNo.trim()); }
        catch (ex: any) { setErr(ex?.message ?? String(ex)); }
        finally { setSaving(false); }
      }}>
      <Field label={t("thSlipNo", "SO No")}>
        <input type="text" maxLength={30} required value={form.soNo}
          onChange={e => setForm(f => ({ ...f, soNo: e.target.value }))} />
      </Field>
      <Field label={t("thSerialNo", "Serial No")}>
        <input type="text" maxLength={30} value={form.serialNo ?? ""}
          placeholder="01"
          onChange={e => setForm(f => ({ ...f, serialNo: e.target.value }))} />
      </Field>
      <Field label={t("thItemCode", "Item Code")}>
        <RefSelect required value={form.itemCode} options={items}
          onChange={v => setForm(f => ({ ...f, itemCode: v }))} />
      </Field>
      <Field label={t("thLocation", "Location")}>
        <RefSelect value={form.locationCode ?? ""} options={locations}
          onChange={v => setForm(f => ({ ...f, locationCode: v }))} />
      </Field>
      <Field label={t("thPlannedQty", "Planned")}>
        <input type="number" min={1} step="any" required value={form.plannedQty}
          onChange={e => setForm(f => ({ ...f, plannedQty: Number(e.target.value) }))} />
      </Field>
      <Field label={t("thSchedDate", "Scheduled Date")}>
        <input type="date" value={form.scheduledDate ?? ""}
          onChange={e => setForm(f => ({ ...f, scheduledDate: e.target.value }))} />
      </Field>
    </ModalShell>
  );
}

function rowKey(r: { soNo: string; serialNo: string }) {
  return `${r.soNo}/${r.serialNo}`;
}
