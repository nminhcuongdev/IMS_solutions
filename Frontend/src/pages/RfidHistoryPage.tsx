import { CsvButton, SearchButton } from "@/lib/Buttons";
import { fmtDateTime } from "@/lib/dateFormat";
import { useMemo, useState } from "react";
import {
  exportRfidHistoryCsv, listRfidHistory,
  type RfidHistoryQuery,
} from "@/api/history";
import { currentMonthRange, DateRange, useDateDraft } from "@/lib/DateRange";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { SortTh, useSort } from "@/lib/SortableTh";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { qs, useFetch } from "@/lib/useFetch";
import { useUrlBool, useUrlNumber, useUrlString, useUrlPatch } from "@/lib/useUrlState";
import { errorText } from "@/lib/errorText";

export function RfidHistoryPage() {
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
  const [q] = useUrlString("q");
  const [qDraft, setQDraft] = useState(q);


  const { sort, toggleSort } = useSort("worked_date", "desc");
  const onSort = toggleSort;

  const filters: RfidHistoryQuery = useMemo(() => ({
    dateFrom: dateOn && dateFrom ? dateFrom : undefined,
    dateTo: dateOn && dateTo ? dateTo : undefined,
    q: q || undefined,
    sort: sort.value,
  }), [dateOn, dateFrom, dateTo, q, sort.value]);

  const [exportErr, setExportErr] = useState<string | null>(null);
  const { data, loading, error: loadErr, refresh } = useFetch(
    qs("rfidHistory", { ...filters, page, pageSize }),
    () => listRfidHistory({ ...filters, page, pageSize }));
  const err = loadErr ?? exportErr;
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

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
      const { blob, filename } = await exportRfidHistoryCsv(filters);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (e: any) { setExportErr(errorText(t, e)); toast.err(errorText(t, e)); }
  };

  return (
    <div className="pg on" id="pg-rfid_gate_history">
      <div className="search-box">
        <div className="search-row">
          <DateRange {...dates.props} />
          <input className="w-70 ml-3.5" type="text" placeholder={t("searchRfidHistory", "Item, EPC, gate...")}
            value={qDraft}
            onChange={e => setQDraft(e.target.value)}
            onKeyDown={e => { if (e.key === "Enter") { patch({ ...dates.patch(), page: null, q: qDraft || null }); refresh(); } }} />
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={() => { patch({ ...dates.patch(), page: null, q: qDraft || null }); refresh(); }} />
          <CsvButton onClick={onExport} disabled={!canExport} title={exportHint} />
        </div>
      </div>

      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <SortTh field="no" sort={sort} onSort={onSort}>{t("thNo", "No")}</SortTh>
            <SortTh field="worked_date" sort={sort} onSort={onSort}>{t("thWorkedDate", "Worked Date")}</SortTh>
            <SortTh field="item_code" sort={sort} onSort={onSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={onSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="gate_code" sort={sort} onSort={onSort}>{t("thGate", "Gate")}</SortTh>
            <SortTh field="serial_no" sort={sort} onSort={onSort}>{t("thEpc", "EPC")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={6} />}
            {!loading && rows.length === 0 && (
              <tr className="no-results-row"><td colSpan={6} className="text-center">{t("noResults", "No matching records found.")}</td></tr>
            )}
            {!loading && rows.map(r => (
              <tr key={r.no}>
                <td>{r.no}</td>
                <td>{fmtDateTime(r.workedDate, true)}</td>
                <td>{r.itemCode ?? "—"}</td>
                <td className="text-left">{r.itemName ?? "—"}</td>
                <td>{r.gateCode}</td>
                <td>{r.serialNo ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })} onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
    </div>
  );
}
