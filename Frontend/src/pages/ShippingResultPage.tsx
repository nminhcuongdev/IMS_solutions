import { listPrintHistory, type PrintHistoryRow } from "@/api/history";
import { ReprintModal } from "@/pages/ReprintModal";
import { fmtDate, fmtDateTime } from "@/lib/dateFormat";
import { useMemo, useState } from "react";
import {
  exportShippingResultCsv, listShippingResults,
  type ShippingPlanRow, type ShippingResultQuery, type ShippingResultRow, type ShippingStatusKey,
} from "@/api/shipping";
import { ShippingLabelModal } from "@/pages/ShippingLabelModal";
import { CsvButton, PrintButton, SearchButton } from "@/lib/Buttons";
import { defaultDateRange, DateRange, useDateDraft } from "@/lib/DateRange";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { rowClickProps } from "@/lib/rowSelect";
import { SortTh, useSort } from "@/lib/SortableTh";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { qs, useFetch } from "@/lib/useFetch";
import { useUrlBool, useUrlNumber, useUrlPatch, useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";

const COLS = 16;

const STATUS_FALLBACK: Record<ShippingStatusKey, string> = {
  statusNotShipped: "Not Shipped",
  statusPartialShip: "Partial",
  statusShipped: "Shipped",
  statusCancelled: "Cancelled",
};

const STATUS_BADGE: Record<ShippingStatusKey, string> = {
  statusNotShipped: "badge-info",
  statusPartialShip: "badge-warn",
  statusShipped: "badge-ok",
  statusCancelled: "badge-cancel",
};

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

export function ShippingResultPage() {
  const t = useT();
  const toast = useToast();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);

  const [defaultRange] = useState(defaultDateRange);
  const [dateOn] = useUrlBool("dates", true);
  const [dateFrom] = useUrlString("from", defaultRange.from);
  const [dateTo] = useUrlString("to", defaultRange.to);
  const dates = useDateDraft(dateOn, dateFrom, dateTo, defaultRange);
  const [presence, setPresence] = useUrlString("so", "all");
  const [itemCode] = useUrlString("ic");
  const [itemName] = useUrlString("in");
  const [q] = useUrlString("q");
  const [icDraft, setIcDraft] = useState(itemCode);
  const [inDraft, setInDraft] = useState(itemName);
  const [qDraft, setQDraft] = useState(q);

  const [selected, setSelected] = useState<Set<number>>(new Set());
  const [recorded, setRecorded] = useState<PrintHistoryRow | null>(null);
  const [reprintRows, setReprintRows] = useState<ShippingPlanRow[] | null>(null);
  const toggle = (no: number) => setSelected(prev => {
    const s = new Set(prev);
    s.has(no) ? s.delete(no) : s.add(no);
    return s;
  });

  const { sort, toggleSort } = useSort("created_at", "desc");

  const filters: ShippingResultQuery = useMemo(() => ({
    dateFrom: dateOn && dateFrom ? dateFrom : undefined,
    dateTo: dateOn && dateTo ? dateTo : undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    slipNo: q || undefined,
    slipNoPresence: presence !== "all" ? presence : undefined,
    sort: sort.value,
  }), [dateOn, dateFrom, dateTo, itemCode, itemName, q, presence, sort.value]);

  const { data, loading, error: err, refresh } = useFetch(
    qs("shippingResult", { ...filters, page, pageSize }),
    () => listShippingResults({ ...filters, page, pageSize }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;
  const allChecked = rows.length > 0 && rows.every(r => selected.has(r.no));
  const someChecked = rows.some(r => selected.has(r.no));
  const toggleAll = () => setSelected(allChecked ? new Set() : new Set(rows.map(r => r.no)));

  const openReprint = async () => {
    const picked = rows.filter(r => selected.has(r.no));
    if (picked.length === 0) {
      toast.err(t("alertSelectAtLeastOne", "Please select at least one row."));
      return;
    }
    if (picked.length === 1) {
      const one = picked[0];
      const doc = one.slipNo ?? "";
      const found = doc ? await listPrintHistory({ category: "Shipping", q: doc, pageSize: 50 }).then(r => r.data, () => []) : [];
      const original = found.filter(h => h.docNo === doc && h.itemCode === one.itemCode && h.canReprint)
        .sort((a, b) => a.no - b.no)[0];
      if (original) { setRecorded(original); return; }
    }
    setReprintRows(picked.map(asLabelRow));
  };

  const onExport = async () => {
    try {
      const { blob, filename } = await exportShippingResultCsv(filters);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
  };

  const search = () => {
    patch({ ...dates.patch(), page: null, ic: icDraft || null, "in": inDraft || null, q: qDraft || null }); refresh();
  };
  const onEnter = (e: { key: string }) => { if (e.key === "Enter") search(); };

  return (
    <div className="pg on" id="pg-shp_result">
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
          <label className="ml-3.5">{t("thSlipNo", "SO No")}</label>
          <input className="w-40" type="text" placeholder="SO-..." value={qDraft}
            onChange={e => setQDraft(e.target.value)} onKeyDown={onEnter} />
          <label className="ml-3.5">{t("soNoPresence", "SO No Presence")}</label>
          <select className="w-35" value={presence}
            onChange={e => { setPage(1); setPresence(e.target.value); }}>
            <option value="all">{t("presenceAll", "All")}</option>
            <option value="with">{t("presenceWithSo", "With SO No")}</option>
            <option value="without">{t("presenceWithoutSo", "Without SO No")}</option>
          </select>
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
          <PrintButton action="Reprint" onClick={openReprint}>{t("btnReprint", "Reprint")}</PrintButton>
          <CsvButton onClick={onExport} />
        </div>
      </div>

      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <th className="chk-col">
              <input type="checkbox" className="row-chk" checked={allChecked}
                ref={el => { if (el) el.indeterminate = someChecked && !allChecked; }}
                onChange={toggleAll} />
            </th>
            <SortTh field="no" sort={sort} onSort={toggleSort}>{t("thNo", "No")}</SortTh>
            <SortTh field="created_at" sort={sort} onSort={toggleSort}>{t("thDateTime", "Date/Time")}</SortTh>
            <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="qty" sort={sort} onSort={toggleSort}>{t("thActualQty", "Qty")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="so_no" sort={sort} onSort={toggleSort}>{t("thSlipNo", "SO No")}</SortTh>
            <SortTh field="serial_no" sort={sort} onSort={toggleSort}>{t("thSerialNo", "Serial No")}</SortTh>
            <SortTh field="scheduled_date" sort={sort} onSort={toggleSort}>{t("thSchedDate", "Scheduled Date")}</SortTh>
            <SortTh field="invoice_no" sort={sort} onSort={toggleSort}>{t("thInvoiceNo", "Invoice No")}</SortTh>
            <SortTh field="location_code" sort={sort} onSort={toggleSort}>{t("thLocation", "Location")}</SortTh>
            <SortTh field="worker" sort={sort} onSort={toggleSort}>{t("thWorker", "Worker")}</SortTh>
            <SortTh field="status" sort={sort} onSort={toggleSort}>{t("thResultStatus", "Status")}</SortTh>
            <SortTh field="shortage_reason" sort={sort} onSort={toggleSort}>{t("thResultShortageReason", "Shortage Reason")}</SortTh>
            <SortTh field="expiry_date" sort={sort} onSort={toggleSort}>{t("thExpirationDate", "Expiration Date")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={COLS} />}
            {!loading && rows.length === 0 && (
              <tr className="no-results-row">
                <td colSpan={COLS} className="text-center">{t("noResults", "No matching records found.")}</td>
              </tr>
            )}
            {!loading && rows.map((r, i) => (
              <tr key={r.no} {...rowClickProps(() => toggle(r.no))}>
                <td className="chk-col">
                  <input type="checkbox" className="row-chk"
                    checked={selected.has(r.no)} onChange={() => toggle(r.no)} />
                </td>
                <td>{(page - 1) * pageSize + i + 1}</td>
                <td>{fmtDateTime(r.createdAt)}</td>
                <td>{r.itemCode}</td>
                <td className="text-left">{dash(r.itemName)}</td>
                <td className="text-right">{r.qty}</td>
                <td>{dash(r.unit)}</td>
                <td>{dash(r.slipNo)}</td>
                <td>{dash(r.serialNo)}</td>
                <td>{fmtDate(r.scheduledDate)}</td>
                <td>{dash(r.invoiceNo)}</td>
                <td>{dash(r.locationCode)}</td>
                <td className="text-left">{dash(r.worker)}</td>
                <td className="text-left">
                  <span className={"badge " + STATUS_BADGE[r.status]}>
                    {t(r.status, STATUS_FALLBACK[r.status])}
                  </span>
                </td>
                <td className="text-left">{dash(r.shortageReason)}</td>
                <td>{fmtDate(r.expiryDate)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })}
        onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />

      {recorded && (
        <ReprintModal row={recorded} onClose={() => setRecorded(null)}
          onPrinted={count => {
            setRecorded(null);
            setSelected(new Set());
            toast.ok(`${t("labelIssueDone", "Labels issued")}: ${count}`);
          }} />
      )}
      {reprintRows && (
        <ShippingLabelModal
          rows={reprintRows}
          reprint
          onClose={() => setReprintRows(null)}
          onPrinted={labels => {
            setReprintRows(null);
            setSelected(new Set());
            toast.ok(`${t("labelIssueDone", "Labels issued")}: ${labels}`);
          }} />
      )}
    </div>
  );
}

function asLabelRow(r: ShippingResultRow): ShippingPlanRow {
  return {
    slipNo: r.slipNo ?? "",
    serialNo: r.serialNo ?? "",
    customerCode: r.customerCode,
    customerName: r.customerName,
    itemCode: r.itemCode,
    itemName: r.itemName,
    plannedQty: r.qty,
    unit: r.unit,
    actualQty: r.qty,
    packingTypeCode: null,
    packingQty: null,
    scheduledDate: r.scheduledDate,
    gateCode: null,
    invoiceNo: r.invoiceNo,
    status: r.status,
    shortageReason: r.shortageReason,
    labelIssued: true,
    createdAt: r.createdAt,
    createdBy: r.worker,
  };
}

