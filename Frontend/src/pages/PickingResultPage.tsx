import { fmtDate, fmtDateTime } from "@/lib/dateFormat";
import { useMemo, useState } from "react";
import {
  exportPickingResultCsv, listPickingResults, type PickingResultQuery,
} from "@/api/picking";
import { CsvButton, SearchButton } from "@/lib/Buttons";
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

const COLS = 12;

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

export function PickingResultPage() {
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
  const [itemCode] = useUrlString("ic");
  const [itemName] = useUrlString("in");
  const [q] = useUrlString("q");
  const [icDraft, setIcDraft] = useState(itemCode);
  const [inDraft, setInDraft] = useState(itemName);
  const [qDraft, setQDraft] = useState(q);

  const [selected, setSelected] = useState<Set<number>>(new Set());
  const toggle = (no: number) => setSelected(prev => {
    const s = new Set(prev);
    s.has(no) ? s.delete(no) : s.add(no);
    return s;
  });

  const { sort, toggleSort } = useSort("created_at", "desc");

  const filters: PickingResultQuery = useMemo(() => ({
    dateFrom: dateOn && dateFrom ? dateFrom : undefined,
    dateTo: dateOn && dateTo ? dateTo : undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    soNo: q || undefined,
    sort: sort.value,
  }), [dateOn, dateFrom, dateTo, itemCode, itemName, q, sort.value]);

  const { data, loading, error: err, refresh } = useFetch(
    qs("pickingResult", { ...filters, page, pageSize }),
    () => listPickingResults({ ...filters, page, pageSize }));
  const rows = data?.data ?? [];
  const allChecked = rows.length > 0 && rows.every(r => selected.has(r.no));
  const someChecked = rows.some(r => selected.has(r.no));
  const toggleAll = () => setSelected(allChecked ? new Set() : new Set(rows.map(r => r.no)));
  const total = data?.meta.total ?? 0;

  const onExport = async () => {
    try {
      const { blob, filename } = await exportPickingResultCsv(filters);
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
    <div className="pg on" id="pg-pick_result">
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
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
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
            <SortTh field="qty" sort={sort} onSort={toggleSort}>{t("thQty", "Qty")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="so_no" sort={sort} onSort={toggleSort}>{t("thSlipNo", "SO No")}</SortTh>
            <SortTh field="serial_no" sort={sort} onSort={toggleSort}>{t("thSerialNo", "Serial No")}</SortTh>
            <SortTh field="scheduled_date" sort={sort} onSort={toggleSort}>{t("thSchedDate", "Scheduled Date")}</SortTh>
            <SortTh field="location_code" sort={sort} onSort={toggleSort}>{t("thLocation", "Location")}</SortTh>
            <SortTh field="worker" sort={sort} onSort={toggleSort}>{t("thWorker", "Worker")}</SortTh>
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
                <td>{r.soNo}</td>
                <td>{r.serialNo}</td>
                <td>{fmtDate(r.scheduledDate)}</td>
                <td>{r.locationCode}</td>
                <td className="text-left">{dash(r.worker)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })}
        onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
    </div>
  );
}

