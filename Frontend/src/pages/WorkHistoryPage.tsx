import { fmtDateTime } from "@/lib/dateFormat";
import { useCallback, useMemo, useState } from "react";
import {
  exportInventoryHistoryCsv, listInventoryHistory, restoreDisposal,
  type HistoryQuery,
} from "@/api/inventory";
import { CsvButton, SearchButton } from "@/lib/Buttons";
import { defaultDateRange, DateRange, useDateDraft } from "@/lib/DateRange";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { useRowSelect } from "@/lib/rowSelect";
import { SortTh, useSort } from "@/lib/SortableTh";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useUrlBool, useUrlNumber, useUrlPatch, useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";


const COLS = 13;

const ACTIONS = [
  "Receiving", "Shipping", "Picking", "Returns",
  "Adjust", "Hold", "HoldRelease", "Disposal",
] as const;

const ACTION_FALLBACK: Record<string, string> = {
  Receiving: "Receiving", Shipping: "Shipping", Picking: "Picking", Returns: "Returns",
  Adjust: "Adjust", Hold: "Hold", HoldRelease: "Release Hold", Disposal: "Disposal",
};

const ACTION_BADGE: Record<string, string> = {
  Receiving: "badge-info",
  Shipping: "badge-ok",
  Picking: "badge-ok",
  Returns: "badge-info",
  Adjust: "badge-warn",
  Hold: "badge-ng",
  HoldRelease: "badge-ok",
  Disposal: "badge-ng",
};

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

export function WorkHistoryPage() {
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
  const [action, setAction] = useUrlString("action");
  const [itemCode] = useUrlString("ic");
  const [itemName] = useUrlString("in");
  const [q] = useUrlString("q");
  const [icDraft, setIcDraft] = useState(itemCode);
  const [inDraft, setInDraft] = useState(itemName);
  const [qDraft, setQDraft] = useState(q);

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [busy, setBusy] = useState(false);

  const { sort, toggleSort } = useSort("created_at", "desc");

  const filters: HistoryQuery = useMemo(() => ({
    dateFrom: dateOn && dateFrom ? dateFrom : undefined,
    dateTo: dateOn && dateTo ? dateTo : undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    action: action || undefined,
    q: q || undefined,
    sort: sort.value,
  }), [dateOn, dateFrom, dateTo, itemCode, itemName, action, q, sort.value]);

  const { data, loading, error: err, reload, refresh } = useFetch(
    qs("inventoryHistory", { ...filters, page, pageSize }),
    () => listInventoryHistory({ ...filters, page, pageSize }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => {
    invalidate("inventoryHistory");
    invalidate("stock");
    await reload();
  }, [reload]);

  const toggle = (c: string) => setSelected(p => {
    const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s;
  });
  const toggleAll = () => setSelected(p =>
    p.size === rows.length ? new Set() : new Set(rows.map(r => String(r.no))));
  const rowProps = useRowSelect(toggle, () => { });

  const onRestore = async () => {
    if (selected.size === 0 || busy) return;
    setBusy(true);
    try {
      const res = await restoreDisposal([...selected].map(Number));
      setSelected(new Set());
      await load();
      if (res.skipped.length) {
        toast.err(`${t("toastSkipped", "Skipped")}: ${res.skipped.join(", ")}`);
      }
      toast.ok(`${t("toastUpdated", "Updated")}: ${res.updated}`);
    } catch (ex: any) { toast.err(ex?.message ?? String(ex)); }
    finally { setBusy(false); }
  };

  const onExport = async () => {
    try {
      const { blob, filename } = await exportInventoryHistoryCsv(filters);
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
    <div className="pg on" id="pg-inv_history">
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
          <label className="ml-3.5">{t("thAction", "Action")}</label>
          <select className="w-37.5" value={action}
            onChange={e => { setPage(1); setAction(e.target.value); }}>
            <option value="">{t("statusAll", "All")}</option>
            {ACTIONS.map(a => (
              <option key={a} value={a}>{t("action" + a, ACTION_FALLBACK[a])}</option>
            ))}
          </select>
          <input className="w-55 ml-3.5" type="text" placeholder={t("searchWorkHistory", "Item, Location, Worker...")}
            value={qDraft}
            onChange={e => setQDraft(e.target.value)} onKeyDown={onEnter} />
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
          <button className="btn btn-special" disabled={selected.size === 0 || busy}
            title={t("restoreSkipNote", "Only Disposal rows not yet restored are put back.")}
            onClick={onRestore}>
            <svg className="ic" viewBox="0 0 24 24"><path d="M12 5V2L7 7l5 5V9a5 5 0 1 1-5 5H5a7 7 0 1 0 7-9z" /></svg>
            {" "}<span>{t("btnRestoreDisposal", "Restore to Inventory")}</span>
          </button>
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
            <SortTh field="created_at" sort={sort} onSort={toggleSort}>{t("thDateTime", "Date/Time")}</SortTh>
            <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
            <th>{t("thScheduleNo", "Schedule No")}</th>
            <SortTh field="lot_no" sort={sort} onSort={toggleSort}>{t("thLot", "Lot")}</SortTh>
            <SortTh field="action_type" sort={sort} onSort={toggleSort}>{t("thAction", "Action")}</SortTh>
            <SortTh field="qty" sort={sort} onSort={toggleSort}>{t("thQty", "Qty")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="location_code" sort={sort} onSort={toggleSort}>{t("thLocation", "Location")}</SortTh>
            <SortTh field="reason" sort={sort} onSort={toggleSort}>{t("thReason", "Reason")}</SortTh>
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
              <tr key={r.no} data-row-code={String(r.no)} {...rowProps(String(r.no))}>
                <td className="chk-col">
                  <input type="checkbox" className="row-chk"
                    checked={selected.has(String(r.no))} onChange={() => toggle(String(r.no))} />
                </td>
                <td>{(page - 1) * pageSize + i + 1}</td>
                <td>{fmtDateTime(r.createdAt)}</td>
                <td>{r.itemCode}</td>
                <td className="text-left">{dash(r.itemName)}</td>
                <td>—</td>
                <td>{dash(r.lotNo)}</td>
                <td>
                  <span className={"badge " + (ACTION_BADGE[r.actionType] ?? "badge-info")}>
                    {t(r.actionLabelKey, ACTION_FALLBACK[r.actionType] ?? r.actionType)}
                  </span>
                </td>
                <td className="text-right">
                  {r.qty === null ? "—" : (r.qty > 0 ? `+${r.qty}` : r.qty)}
                </td>
                <td>{dash(r.unit)}</td>
                <td>{dash(r.locationCode)}</td>
                <td className="text-left">{dash(r.reasonName)}</td>
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

