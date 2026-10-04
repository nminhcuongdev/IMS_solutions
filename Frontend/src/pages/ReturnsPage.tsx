import { fmtDate, fmtDateTime } from "@/lib/dateFormat";
import { useCallback, useMemo, useState } from "react";
import {
  changeReturnStatus, createReturnPlan, exportReturnsScheduleCsv,
  listReturnsSchedule, registerReturnShortage,
  type ReturnPlanWrite,
  type ReturnScheduleQuery, type ReturnStatusKey,
} from "@/api/returns";
import { listItems } from "@/api/items";
import { listPartners } from "@/api/partners";
import { CsvButton, ImportButton, NewButton, PrintButton, SearchButton, ShortageButton, StatusChangeButton } from "@/lib/Buttons";
import { ScheduleImportModal } from "@/pages/ScheduleImportModal";
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
import { ReturnLabelModal } from "@/pages/ReturnLabelModal";
import { useT } from "@/i18n/LanguageContext";
import { ShortageModal, ShortageReasonCell, useShortageReasons } from "@/pages/ShortageModal";
import { StatusChangeModal } from "@/pages/StatusChangeModal";


const COLS = 17;

const STATUS_KEYS: ReturnStatusKey[] = [
  "statusPendingReturn", "statusPartialReturn", "statusReturned", "statusCancelled",
];

const STATUS_FALLBACK: Record<ReturnStatusKey, string> = {
  statusPendingReturn: "Pending",
  statusPartialReturn: "Partial",
  statusReturned: "Returned",
  statusCancelled: "Cancelled",
};

const STATUS_BADGE: Record<ReturnStatusKey, string> = {
  statusPendingReturn: "badge-info",
  statusPartialReturn: "badge-warn",
  statusReturned: "badge-ok",
  statusCancelled: "badge-cancel",
};

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

type Modal = "new" | "status" | "shortage" | "label" | "import" | null;

export function ReturnsPage() {
  const t = useT();
  const toast = useToast();
  const shortageReasons = useShortageReasons();
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
  const [q] = useUrlString("q");
  const [icDraft, setIcDraft] = useState(itemCode);
  const [inDraft, setInDraft] = useState(itemName);
  const [qDraft, setQDraft] = useState(q);

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState<Modal>(null);

  const { sort, toggleSort } = useSort("scheduled_date");

  const filters: ReturnScheduleQuery = useMemo(() => ({
    dateFrom: dateOn && dateFrom ? dateFrom : undefined,
    dateTo: dateOn && dateTo ? dateTo : undefined,
    status: status || undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    returnNo: q || undefined,
    sort: sort.value,
  }), [dateOn, dateFrom, dateTo, itemCode, itemName, status, q, sort.value]);

  const { data, loading, error: err, reload, refresh } = useFetch(
    qs("returnsSchedule", { ...filters, page, pageSize }),
    () => listReturnsSchedule({ ...filters, page, pageSize }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("returnsSchedule"); await reload(); }, [reload]);

  const labelRows = useMemo(() => rows.filter(r => selected.has(rowKey(r))), [rows, selected]);

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => rowKey(r) === [...selected][0]) ?? null : null,
    [selected, rows]);

  const toggle = (c: string) => setSelected(p => {
    const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s;
  });
  const toggleAll = () => setSelected(p =>
    p.size === rows.length ? new Set() : new Set(rows.map(rowKey)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(rowKey), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);
  useShortcuts(rowNav);

  const onExport = async () => {
    try {
      const { blob, filename } = await exportReturnsScheduleCsv(filters);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (ex: any) { toast.err(ex?.message ?? String(ex)); }
  };

  const search = () => {
    patch({ ...dates.patch(), page: null, ic: icDraft || null, "in": inDraft || null, q: qDraft || null }); refresh();
  };
  const onEnter = (e: { key: string }) => { if (e.key === "Enter") search(); };

  return (
    <div className="pg on" id="pg-returns">
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
          <label className="ml-3.5">{t("thReturnNo", "Return No")}</label>
          <input className="w-40" type="text" placeholder="RTN-..." value={qDraft}
            onChange={e => setQDraft(e.target.value)} onKeyDown={onEnter} />
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
          <PrintButton disabled={labelRows.length === 0} onClick={() => setModal("label")}>
            {t("labelPrint", "Label")}
          </PrintButton>
          <ShortageButton onClick={() => selectedRow
            ? setModal("shortage")
            : toast.err(t("alertSelectOneRow", "Please select exactly one row."))} />
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
            <SortTh field="return_no" sort={sort} onSort={toggleSort}>{t("thReturnNo", "Return No")}</SortTh>
            <SortTh field="return_branch_no" sort={sort} onSort={toggleSort}>{t("thSerialNo", "Serial No")}</SortTh>
            <SortTh field="partner_code" sort={sort} onSort={toggleSort}>{t("thReturnedBy", "Returned By")}</SortTh>
            <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="qty" sort={sort} onSort={toggleSort}>{t("thPlannedQty", "Planned")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="actual_qty" sort={sort} onSort={toggleSort}>{t("thActualQty", "Actual")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="scheduled_date" sort={sort} onSort={toggleSort}>{t("thSchedDate", "Scheduled Date")}</SortTh>
            <SortTh field="invoice_no" sort={sort} onSort={toggleSort}>{t("thInvoiceNo", "Invoice No")}</SortTh>
            <SortTh field="status" sort={sort} onSort={toggleSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="shortage_reason" sort={sort} onSort={toggleSort}>{t("thShortageReason", "Shortage Reason")}</SortTh>
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
              <tr key={rowKey(r)} data-row-code={rowKey(r)} {...rowProps(rowKey(r))}
                className={[flashCls(r.returnNo), rowKey(r) === cursorCode ? "is-cursor" : ""]
                  .filter(Boolean).join(" ") || undefined}>
                <td className="chk-col">
                  <input type="checkbox" className="row-chk"
                    checked={selected.has(rowKey(r))} onChange={() => toggle(rowKey(r))} />
                </td>
                <td>{(page - 1) * pageSize + i + 1}</td>
                <td>{r.returnNo}</td>
                <td>{r.serialNo}</td>
                <td className="text-left">{dash(codeName(r.returnedByCode ?? "", r.returnedByName) || null)}</td>
                <td>{r.itemCode}</td>
                <td className="text-left">{dash(r.itemName)}</td>
                <td className="text-right">{r.expectedQty}</td>
                <td>{dash(r.unit)}</td>
                <td className="text-right">{r.actualQty}</td>
                <td>{dash(r.unit)}</td>
                <td>{fmtDate(r.returnDate)}</td>
                <td>{dash(r.invoiceNo)}</td>
                <td>
                  <span className={"badge " + STATUS_BADGE[r.status]}>
                    {t(r.status, STATUS_FALLBACK[r.status])}
                  </span>
                </td>
                <td className="text-left"><ShortageReasonCell code={r.shortageReason} reasons={shortageReasons} /></td>
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
        <NewReturnModal
          onClose={() => setModal(null)}
          onSaved={async savedNo => {
            setModal(null); setSelected(new Set()); await load();
            flash(savedNo); toast.ok(t("toastSaved", "Saved"));
          }} />
      )}

      {modal === "status" && (
        <StatusChangeModal
          count={labelRows.length}
          done={{ key: "statusReturned", fallback: "Returned" }}
          notDone={{ key: "statusPendingReturn", fallback: "Pending" }}
          onApply={action => changeReturnStatus(
            labelRows.map(r => ({ returnNo: r.returnNo, serialNo: r.serialNo })), action)}
          onClose={() => setModal(null)}
          onDone={async ({ updated, skipped }) => {
            setModal(null); setSelected(new Set()); await load();
            if (skipped.length) {
              toast.err(`${t("toastSkipped", "Skipped")}: ${skipped.join(", ")}`);
            }
            toast.ok(`${t("toastUpdated", "Updated")}: ${updated}`);
          }} />
      )}

      {modal === "shortage" && selectedRow && (
        <ShortageModal
          docLabel={t("thReturnNo", "Return No")}
          docNo={selectedRow.returnNo}
          itemCode={selectedRow.itemCode}
          itemName={selectedRow.itemName}
          planned={selectedRow.expectedQty}
          actual={selectedRow.actualQty}
          onSave={(qty, reason) => registerReturnShortage(selectedRow.returnNo, qty, reason, selectedRow.serialNo)}
          onClose={() => setModal(null)}
          onSaved={async () => {
            setModal(null); await load();
            flash(selectedRow.returnNo); toast.ok(t("toastSaved", "Saved"));
          }} />
      )}
      {modal === "label" && labelRows.length > 0 && (
        <ReturnLabelModal
          rows={labelRows}
          onClose={() => setModal(null)}
          onPrinted={async labels => {
            setModal(null); await load();
            toast.ok(`${t("labelIssueDone", "Labels issued")}: ${labels}`);
          }} />
      )}

      {modal === "import" && (
        <ScheduleImportModal kind="returns-schedule"
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


function NewReturnModal({ onClose, onSaved }: {
  onClose: () => void; onSaved: (returnNo: string) => void;
}) {
  const t = useT();
  const [form, setForm] = useState<ReturnPlanWrite>({
    returnNo: "", serialNo: "01", returnedByCode: "", itemCode: "",
    expectedQty: 1, returnDate: new Date().toISOString().slice(0, 10),
    invoiceNo: "", lotNo: "",
  });
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const partners = useRefOptions(
    () => listPartners({ pageSize: 500 }),
    p => ({ value: p.partnerCode, label: codeName(p.partnerCode, p.partnerName) }));
  const items = useRefOptions(
    () => listItems({ pageSize: 500 }),
    i => ({ value: i.itemCode, label: codeName(i.itemCode, i.itemName) }));

  return (
    <ModalShell title={t("newReturnsTitle", "New Return")} onClose={onClose}
      saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try { await createReturnPlan(form); onSaved(form.returnNo.trim()); }
        catch (ex: any) { setErr(ex?.message ?? String(ex)); }
        finally { setSaving(false); }
      }}>
      <Field label={t("thReturnNo", "Return No")}>
        <input type="text" maxLength={30} required value={form.returnNo}
          onChange={e => setForm(f => ({ ...f, returnNo: e.target.value }))} />
      </Field>
      <Field label={t("thSerialNo", "Serial No")}>
        <input type="text" maxLength={30} value={form.serialNo ?? ""}
          placeholder="01"
          onChange={e => setForm(f => ({ ...f, serialNo: e.target.value }))} />
      </Field>
      <Field label={t("thReturnedBy", "Returned By")}>
        <RefSelect value={form.returnedByCode ?? ""} options={partners}
          onChange={v => setForm(f => ({ ...f, returnedByCode: v }))} />
      </Field>
      <Field label={t("thItemCode", "Item Code")}>
        <RefSelect required value={form.itemCode} options={items}
          onChange={v => setForm(f => ({ ...f, itemCode: v }))} />
      </Field>
      <Field label={t("thExpectedQty", "Expected")}>
        <input type="number" min={1} step="any" required value={form.expectedQty}
          onChange={e => setForm(f => ({ ...f, expectedQty: Number(e.target.value) }))} />
      </Field>
      <Field label={t("thLot", "Lot")}>
        <input type="text" maxLength={20} value={form.lotNo ?? ""}
          onChange={e => setForm(f => ({ ...f, lotNo: e.target.value }))} />
      </Field>
      <Field label={t("thReturnDate", "Return Date")}>
        <input type="date" value={form.returnDate ?? ""}
          onChange={e => setForm(f => ({ ...f, returnDate: e.target.value }))} />
      </Field>
      <Field label={t("thInvoiceNo", "Invoice No")}>
        <input type="text" maxLength={30} value={form.invoiceNo ?? ""}
          onChange={e => setForm(f => ({ ...f, invoiceNo: e.target.value }))} />
      </Field>
    </ModalShell>
  );
}

function rowKey(r: { returnNo: string; serialNo: string }) {
  return `${r.returnNo}/${r.serialNo}`;
}
