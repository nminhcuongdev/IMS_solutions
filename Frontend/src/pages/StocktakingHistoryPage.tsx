import { CsvButton, SearchButton } from "@/lib/Buttons";
import { useState } from "react";
import {
  exportStocktakingHistoryCsv, listStocktakingHistory, type StocktakingHistoryRow,
} from "@/api/stocktaking";
import { PageFooter } from "@/lib/PageFooter";
import { SortTh, useSort } from "@/lib/SortableTh";
import { PAGE_SIZES } from "@/lib/Pager";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { qs, useFetch } from "@/lib/useFetch";
import { useUrlNumber, useUrlPatch, useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";


const COLS = 11;

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

const stamp = (v: string) =>
  v ? new Date(v).toLocaleString("sv-SE").replace("T", " ").slice(0, 19).replace(/-/g, "/") : "—";

export function StocktakingHistoryPage() {
  const t = useT();
  const toast = useToast();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);
  const { sort, toggleSort } = useSort("stocktaking_date", "desc");
  const [q, setQ] = useUrlString("q");
  const [qDraft, setQDraft] = useState(q);

  const { data, loading, error, refresh } = useFetch(
    qs("stRef", { q, page, pageSize, sort: sort.value }),
    () => listStocktakingHistory({ q: q || undefined, page, pageSize, sort: sort.value }));

  const rows: StocktakingHistoryRow[] = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const search = () => { setPage(1); setQ(qDraft); refresh(); };

  const onExport = async () => {
    try {
      const { blob, filename } = await exportStocktakingHistoryCsv({ q: q || undefined });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
  };

  return (
    <div className="pg on" id="pg-st_ref">
      <div className="search-box">
        <div className="search-row">
          <input className="w-70" type="text"
            placeholder={t("searchStRef", "Stocktaking name, item, status...")}
            value={qDraft} onChange={e => setQDraft(e.target.value)}
            onKeyDown={e => { if (e.key === "Enter") search(); }} />
        </div>

        {error && <div className="search-row text-rose-600 text-[12px]">{error}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
          <CsvButton onClick={onExport} />
        </div>
      </div>

      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <SortTh field="no" sort={sort} onSort={toggleSort}>{t("thNo", "No")}</SortTh>
            <SortTh field="stocktaking_name" sort={sort} onSort={toggleSort}>{t("stName", "Stocktaking Name")}</SortTh>
            <SortTh field="stocktaking_date" sort={sort} onSort={toggleSort}>{t("thStocktakeDate", "Stocktaking Date")}</SortTh>
            <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="lot_no" sort={sort} onSort={toggleSort}>{t("thLot", "Lot")}</SortTh>
            <th>{t("thLocation", "Location")}</th>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="planned_qty" sort={sort} onSort={toggleSort}>{t("slPlannedQty", "Planned")}</SortTh>
            <SortTh field="result_qty" sort={sort} onSort={toggleSort}>{t("thResultQty", "Result")}</SortTh>
            <SortTh field="missing_qty" sort={sort} onSort={toggleSort}>{t("thMissingQty", "Missing")}</SortTh>
            <SortTh field="status" sort={sort} onSort={toggleSort}>{t("thStatus", "Status")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={COLS + 1} />}
            {!loading && rows.length === 0 && (
              <tr className="no-results-row">
                <td colSpan={COLS + 1} className="text-center">
                  {t("noResults", "No matching records found.")}
                </td>
              </tr>
            )}
            {!loading && rows.map((r, i) => (
              <tr key={`${r.itemCode}|${r.lotNo}`}>
                <td>{(page - 1) * pageSize + i + 1}</td>
                <td className="text-left">{r.stocktakingName}</td>
                <td>{stamp(r.stocktakingDate)}</td>
                <td>{r.itemCode}</td>
                <td className="text-left">{dash(r.itemName)}</td>
                <td>{dash(r.lotNo)}</td>
                <td>—</td>
                <td>{dash(r.unit)}</td>
                <td className="text-right">{r.plannedQty}</td>
                <td className="text-right">{r.resultQty}</td>
                <td className={"text-right" + (r.missingQty === 0 ? "" : " text-red")}>{r.missingQty}</td>
                <td>
                  <span className={"badge " + (r.badgeKey === "badgeMatch" ? "badge-ok" : "badge-ng")}>
                    {t(r.badgeKey, r.badgeKey === "badgeMatch" ? "Match" : "Diff")}
                  </span>
                </td>
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
