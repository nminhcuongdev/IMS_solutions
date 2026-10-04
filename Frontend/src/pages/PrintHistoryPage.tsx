import { fmtDate, fmtDateTime } from "@/lib/dateFormat";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  exportPrintHistoryCsv, listPrintHistory,
  type PrintHistoryQuery, type PrintHistoryRow,
} from "@/api/history";
import { CsvButton, PrintButton, SearchButton } from "@/lib/Buttons";
import { currentMonthRange, DateRange, useDateDraft } from "@/lib/DateRange";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { SortTh, useSort } from "@/lib/SortableTh";
import { ReprintModal } from "@/pages/ReprintModal";
import { useT } from "@/i18n/LanguageContext";
import { rowClickProps } from "@/lib/rowSelect";
import { useToast } from "@/lib/Toast";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { useDebounced } from "@/lib/useDebounced";
import { qs, useFetch } from "@/lib/useFetch";
import { useUrlBool, useUrlNumber, useUrlString, useUrlPatch } from "@/lib/useUrlState";
import { Spacer, useVirtualRows } from "@/lib/VirtualRows";

const CATEGORIES = [
  "Receiving", "ReceivingReprint", "Returns", "ReturnsReprint",
  "Shipping", "ShippingReprint", "Inventory",
];

const ROW_H = 37;
const CHUNK = 100;
const COLS = 14;

const STATUS_PRINTED = 19;
const STATUS_REPRINTED = 20;

const humanize = (v: string) => v.replace(/([a-z])([A-Z])/g, "$1 $2");

const catLabel = (v: string, t: (k: string, f: string) => string) =>
  t("printCat" + v, humanize(v));

export function PrintHistoryPage() {
  const t = useT();
  const toast = useToast();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);

  const [defaultRange] = useState(currentMonthRange);
  const [dateOn] = useUrlBool("dates", true);
  const [dateFrom] = useUrlString("from", defaultRange.from);
  const [dateTo] = useUrlString("to", defaultRange.to);
  const dates = useDateDraft(dateOn, dateFrom, dateTo, defaultRange);
  const [category, setCategory] = useUrlString("cat");
  const [status, setStatus] = useUrlString("status");
  const [q] = useUrlString("q");
  const [item] = useUrlString("item");
  const [qDraft, setQDraft] = useState(q);
  const [itemDraft, setItemDraft] = useState(item);
  const itemSettled = useDebounced(itemDraft);
  useEffect(() => {
    if (itemSettled !== item) patch({ page: null, item: itemSettled || null });
  }, [itemSettled]);

  const [selected, setSelected] = useState<number | null>(null);
  const toggle = (no: number) => setSelected(s => (s === no ? null : no));
  const [reprint, setReprint] = useState<PrintHistoryRow | null>(null);

  const { sort, toggleSort } = useSort("worked_date", "desc");
  const onSort = toggleSort;

  const filters: PrintHistoryQuery = useMemo(() => ({
    dateFrom: dateOn && dateFrom ? dateFrom : undefined,
    dateTo: dateOn && dateTo ? dateTo : undefined,
    category: category || undefined,
    status: status ? Number(status) : undefined,
    item: item || undefined,
    q: q || undefined,
    sort: sort.value,
  }), [dateOn, dateFrom, dateTo, category, status, item, q, sort.value]);

  const [virtualMode, setVirtualMode] = useUrlBool("scroll", false);

  const [exportErr, setExportErr] = useState<string | null>(null);
  const { data, loading, error: loadErr, refresh } = useFetch(
    qs("printHistory", { ...filters, page, pageSize }),
    () => listPrintHistory({ ...filters, page, pageSize }));
  const err = loadErr ?? exportErr;
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const fetchChunk = useCallback(
    async (offset: number, count: number) => {
      const r = await listPrintHistory({ ...filters, page: offset / CHUNK + 1, pageSize: count });
      return r.data;
    }, [filters]);

  const vr = useVirtualRows<PrintHistoryRow>({
    total, rowHeight: ROW_H, chunkSize: CHUNK, fetchChunk,
    resetKey: qs("printHistory", { ...filters }),
  });

  const selectedRow = (virtualMode
    ? vr.items.find(r => r && r.no === selected)
    : rows.find(r => r.no === selected)) ?? null;

  const rangeDays = dateOn && dateFrom && dateTo
    ? Math.round((new Date(dateTo).getTime() - new Date(dateFrom).getTime()) / 86400000) + 1
    : 0;
  const canExport = rangeDays > 0 && rangeDays <= 92;
  const exportHint = !dateOn || !dateFrom || !dateTo
    ? t("exportPickRange", "Select a Worked Date range before exporting CSV.")
    : rangeDays > 92
      ? t("exportMaxDays", "Date range is limited to {0} days (currently {1}).")
          .replace("{0}", "92").replace("{1}", String(rangeDays))
      : t("exportDays", "Export {0} day(s)").replace("{0}", String(rangeDays));

  const onExport = async () => {
    try {
      const { blob, filename } = await exportPrintHistoryCsv(filters);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (e: any) { setExportErr(e?.message ?? String(e)); toast.err(e?.message ?? String(e)); }
  };

  return (
    <div className="pg on" id="pg-print_history">
      <div className="search-box">
        <div className="search-row">
          <DateRange {...dates.props} />
          <input className="w-50 ml-3.5" type="text" placeholder={t("searchItemCodeName", "Item Code or Name...")}
            value={itemDraft}
            onChange={e => setItemDraft(e.target.value)} />
          <label className="ml-3.5">{t("thPrintCategory", "Print Category")}</label>
          <select className="w-42.5" value={category}
            onChange={e => { setPage(1); setCategory(e.target.value); }}>
            <option value="">{t("statusAll", "All")}</option>
            {CATEGORIES.map(c => <option key={c} value={c}>{catLabel(c, t)}</option>)}
          </select>
          <label className="ml-3.5">{t("thStatus", "Status")}</label>
          <select className="w-35" value={status}
            onChange={e => { setPage(1); setStatus(e.target.value); }}>
            <option value="">{t("statusAll", "All")}</option>
            <option value={STATUS_PRINTED}>{t("statusPrinted", "Printed")}</option>
            <option value={STATUS_REPRINTED}>{t("statusReprinted", "Reprinted")}</option>
          </select>
          <input className="w-60 ml-3.5" type="text" placeholder={t("searchPrintHistory", "Doc No, item, printer, worker...")}
            value={qDraft}
            onChange={e => setQDraft(e.target.value)}
            onKeyDown={e => { if (e.key === "Enter") { patch({ ...dates.patch(), page: null, item: itemDraft || null, q: qDraft || null }); refresh(); } }} />
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={() => { patch({ ...dates.patch(), page: null, item: itemDraft || null, q: qDraft || null }); refresh(); }} />
          <PrintButton disabled={!selectedRow}
            onClick={() => selectedRow && setReprint(selectedRow)}>{t("btnReprint", "Reprint")}</PrintButton>
          <CsvButton onClick={onExport} disabled={!canExport} title={exportHint} />
          <button className={"btn " + (virtualMode ? "btn-primary" : "btn-outline")}
            onClick={() => setVirtualMode(!virtualMode)}
            title={t("scrollModeHint", "Scroll the whole table without paging")}>
            {virtualMode ? t("scrollModePaged", "Paged view") : t("scrollModeAll", "Scroll all")}
          </button>
        </div>
      </div>

      <div className={"tbl-wrap" + (virtualMode ? " is-virtual" : "")}
        ref={virtualMode ? vr.scrollRef : undefined}>
        <table className="tbl">
          <thead><tr>
            <th className="chk-col"></th>
            <SortTh field="no" sort={sort} onSort={onSort}>{t("thNo", "No")}</SortTh>
            <SortTh field="worked_date" sort={sort} onSort={onSort}>{t("thWorkedDate", "Worked Date")}</SortTh>
            <SortTh field="category" sort={sort} onSort={onSort}>{t("thPrintCategory", "Print Category")}</SortTh>
            <SortTh field="doc_no" sort={sort} onSort={onSort}>{t("colDocNo", "Doc No.")}</SortTh>
            <SortTh field="scheduled_date" sort={sort} onSort={onSort}>{t("thSchedDate", "Scheduled Date")}</SortTh>
            <th>{t("thScheduleSerialNo", "Schedule Serial No")}</th>
            <SortTh field="item_code" sort={sort} onSort={onSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={onSort}>{t("thItemName", "Item Name")}</SortTh>
            <th>{t("thLot", "Lot")}</th>
            <SortTh field="qty" sort={sort} onSort={onSort}>{t("thQty", "Qty")}</SortTh>
            <SortTh field="uom_code" sort={sort} onSort={onSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="created_by" sort={sort} onSort={onSort}>{t("thWorker", "Worker")}</SortTh>
            <SortTh field="status" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={COLS} />}
            {!loading && total === 0 && (
              <tr className="no-results-row"><td colSpan={COLS} className="text-center">{t("noResults", "No matching records found.")}</td></tr>
            )}

            {!loading && virtualMode && total > 0 && (
              <>
                <Spacer height={vr.padTop} cols={COLS} />
                {vr.items.map((r, i) => r
                  ? <PrintRow key={r.no} r={r} selected={selected === r.no}
                      onSelect={() => toggle(r.no)} t={t} />
                  : <PendingRow key={`p${vr.start + i}`} />)}
                <Spacer height={vr.padBottom} cols={COLS} />
              </>
            )}

            {!loading && !virtualMode && rows.map(r => (
              <PrintRow key={r.no} r={r} selected={selected === r.no}
                onSelect={() => toggle(r.no)} t={t} />
            ))}
          </tbody>
        </table>
      </div>

      {virtualMode
        ? (
          <div className="pg-footer">
            <div className="pg-pagesize">
              <span>{t("pgTotal", "Total:")} {total.toLocaleString()}</span>
              <span className="ml-3.5 text-slate-500">
                {t("scrollRowRange", "Rows")} {(vr.start + 1).toLocaleString()}–
                {Math.min(total, vr.start + vr.items.length).toLocaleString()}
                {vr.pendingChunks > 0 && ` · ${t("loading", "Loading…")}`}
              </span>
            </div>
            <div className="pg-footer-spacer"></div>
          </div>
        )
        : (
          <PageFooter total={total} page={page} pageSize={pageSize}
            onGoPage={p => setPage(p, { push: true })} onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
        )}

      {reprint && <ReprintModal row={reprint} onClose={() => setReprint(null)}
        onPrinted={count => {
          setReprint(null);
          toast.ok(`${t("btnReprint", "Reprint")}: ${reprint.docNo ?? reprint.itemCode ?? reprint.no} × ${count}`);
          refresh();
        }} />}
    </div>
  );
}

function PrintRow({ r, selected, onSelect, t }: {
  r: PrintHistoryRow; selected: boolean; onSelect: () => void;
  t: (k: string, f: string) => string;
}) {
  return (
    <tr style={{ height: ROW_H }} {...rowClickProps(onSelect)}>
      <td className="chk-col">
        <input type="checkbox" className="row-chk"
          checked={selected} onChange={onSelect} />
      </td>
      <td>{r.no}</td>
      <td>{fmtDateTime(r.workedDate)}</td>
      <td className="text-left">{catLabel(r.category, t)}</td>
      <td>{r.docNo ?? "—"}</td>
      <td>{fmtDate(r.scheduledDate)}</td>
      <td>—</td>
      <td>{r.itemCode ?? "—"}</td>
      <td className="text-left">{r.itemName ?? "—"}</td>
      <td>—</td>
      <td className="text-right">{r.qty ?? "—"}</td>
      <td>{r.uomCode ?? "—"}</td>
      <td className="text-left">{r.workerName ?? r.createdBy ?? "—"}</td>
      <td>
        <span className={"badge " + (r.status === STATUS_REPRINTED ? "badge-info" : "badge-ok")}>
          {r.statusName ?? r.status}
        </span>
      </td>
    </tr>
  );
}

function PendingRow() {
  return (
    <tr aria-hidden="true" style={{ height: ROW_H }}>
      {Array.from({ length: COLS }, (_, c) => (
        <td key={c}><span className="sk" style={{ width: `${55 + (c * 17) % 35}%` }} /></td>
      ))}
    </tr>
  );
}
