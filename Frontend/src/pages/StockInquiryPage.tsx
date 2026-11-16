import { useScreenCan } from "@/auth/PermissionContext";
import { fmtDate, fmtDateTime } from "@/lib/dateFormat";
import { useCallback, useMemo, useState } from "react";
import {
  adjustStock, disposeStock, exportStockCsv, listStock, listStockGrouped,
  type AggAxis, type StockQuery, type StockRow,
} from "@/api/inventory";
import { listItemGroups } from "@/api/itemGroups";
import { listLocations } from "@/api/locations";
import { BTN_ICON } from "@/layout/icons";
import { CsvButton, SearchButton } from "@/lib/Buttons";
import { TrashIcon } from "@/lib/CommonToolbar";
import { listReasons } from "@/api/reasons";
import { Field, ModalShell } from "@/lib/ModalShell";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { codeName, RefSelect, useRefOptions } from "@/lib/RefSelect";
import { useRowSelect } from "@/lib/rowSelect";
import { SortTh, useSort, type SortState } from "@/lib/SortableTh";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav, useShortcuts } from "@/lib/useShortcuts";
import { useUrlNumber, useUrlPatch, useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";


const COLS = 16;

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

const keyOf = (r: { itemCode: string; serialNo: string }) => `${r.itemCode}/${r.serialNo}`;

function expiryClass(days: number | null): string {
  if (days === null) return "";
  if (days < 0) return "text-red";
  if (days <= 30) return "text-orange";
  return "";
}

function overShortClass(v: number | null): string {
  if (v === null || v === 0) return "";
  return v < 0 ? "text-red" : "text-green";
}

function stockLevelClass(r: StockRow): string {
  if (r.reorderPoint !== null && r.stockQty < r.reorderPoint) return "text-red";
  if (r.surplusPoint !== null && r.stockQty >= r.surplusPoint) return "text-blue";
  return "";
}

type Modal = "adjust" | "disposal" | null;

export function StockInquiryPage() { const can = useScreenCan();
  const t = useT();
  const toast = useToast();
  const { flash, flashCls } = useRowFlash();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);

  const [warehouse, setWarehouse] = useUrlString("wh");
  const [category, setCategory] = useUrlString("cat");
  const [lot] = useUrlString("lot");
  const [expiration, setExpiration] = useUrlString("exp");
  const [stockLevel, setStockLevel] = useUrlString("lvl");
  const [agg, setAgg] = useUrlString("agg", "lot");
  const [itemCode] = useUrlString("ic");
  const [itemName] = useUrlString("in");
  const [icDraft, setIcDraft] = useState(itemCode);
  const [inDraft, setInDraft] = useState(itemName);
  const [lotDraft, setLotDraft] = useState(lot);

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState<Modal>(null);

  const { sort, toggleSort } = useSort("item_code");

  const filters: StockQuery = useMemo(() => ({
    warehouse: warehouse || undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    category: category || undefined,
    lot: lot || undefined,
    expiration: expiration || undefined,
    stockLevel: stockLevel || undefined,
    sort: sort.value,
  }), [warehouse, itemCode, itemName, category, lot, expiration, stockLevel, sort.value]);

  const axis = (agg === "item" || agg === "location" ? agg : "lot") as AggAxis;

  const { data, loading, error: err, reload, refresh } = useFetch<{ axis: AggAxis; res: any }>(
    qs("stock", { ...filters, page, pageSize, agg: axis }),
    async () => ({
      axis,
      res: axis === "lot"
        ? await listStock({ ...filters, page, pageSize })
        : await listStockGrouped(axis, { ...filters, page, pageSize }),
    }));

  const search = () => {
    patch({ page: null, ic: icDraft || null, "in": inDraft || null, lot: lotDraft || null }); refresh();
  };
  const onEnter = (e: { key: string }) => { if (e.key === "Enter") search(); };

  const { data: whData } = useFetch("stockWarehouses",
    () => listLocations({ type: "Large", isActive: true, pageSize: 200, sort: "location_code" }));
  const warehouses = whData?.data ?? [];

  const shownAxis: AggAxis = data?.axis ?? axis;
  const showGrouped = shownAxis !== "lot";
  const total = data?.res.meta.total ?? 0;
  const rows = (data?.res.data ?? []) as any[];
  const flat: StockRow[] = showGrouped ? [] : (rows as StockRow[]);

  const load = useCallback(async () => { invalidate("stock"); await reload(); }, [reload]);

  const toggle = (c: string) => setSelected(p => {
    const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s;
  });
  const toggleAll = () => setSelected(p =>
    p.size === flat.length ? new Set() : new Set(flat.map(keyOf)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: flat.map(keyOf), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);
  useShortcuts(rowNav);

  const target = flat.find(r => selected.has(keyOf(r))) ?? null;

  const itemGroups = useRefOptions(
    () => listItemGroups({ pageSize: 200 }),
    g => ({ value: g.itemGroupCode, label: g.itemGroupName }));

  const onExport = async () => {
    try {
      const { blob, filename } = await exportStockCsv(filters);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
  };

  return (
    <div className="pg on" id="pg-inv">
      <div className="search-box">
        <div className="search-row">
          <label>{t("warehouse", "Inventory")}</label>
          <select className="w-32.5" value={warehouse}
            onChange={e => { setPage(1); setWarehouse(e.target.value); }}>
            <option value="">{t("warehouseAll", "All")}</option>
            {warehouses.map(w => <option key={w.locationCode} value={w.locationCode}>{w.locationCode}</option>)}
          </select>
          <label className="ml-3.5">{t("thItemCode", "Item Code")}</label>
          <input className="w-35" type="text" value={icDraft}
            onChange={e => setIcDraft(e.target.value)} onKeyDown={onEnter} />
          <label className="ml-2">{t("thItemName", "Item Name")}</label>
          <input className="w-40" type="text" value={inDraft}
            onChange={e => setInDraft(e.target.value)} onKeyDown={onEnter} />
          <label className="ml-3.5">{t("category", "Category")}</label>
          <select className="w-30" value={category}
            onChange={e => { setPage(1); setCategory(e.target.value); }}>
            <option value="">{t("categoryAll", "All")}</option>
            {itemGroups.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select>
          <label className="ml-3.5">{t("lot", "Lot")}</label>
          <input className="w-32.5" type="text" value={lotDraft} placeholder="LOT260818"
            onChange={e => setLotDraft(e.target.value)} onKeyDown={onEnter} />
          <label className="ml-3.5">{t("aggAxisLabel", "Agg")}</label>
          <select className="w-35" value={axis}
            onChange={e => { setPage(1); setSelected(new Set()); setAgg(e.target.value); }}>
            <option value="lot">{t("aggLot", "By Lot")}</option>
            <option value="item">{t("aggItem", "By Item")}</option>
            <option value="location">{t("aggLocation", "By Location")}</option>
          </select>
          <label className="ml-3.5">{t("expirationStatus", "Expiration")}</label>
          <select className="w-37.5" value={expiration}
            onChange={e => { setPage(1); setExpiration(e.target.value); }}>
            <option value="">{t("expFilterAll", "All")}</option>
            <option value="expired">{t("expStatusExpired", "Expired")}</option>
            <option value="expiringSoon">{t("expStatusExpiringSoon", "Expiring Soon")}</option>
          </select>
          <label className="ml-3.5">{t("stockLevel", "Stock Level")}</label>
          <select className="w-35" value={stockLevel}
            onChange={e => { setPage(1); setStockLevel(e.target.value); }}>
            <option value="">{t("stockLevelAll", "All")}</option>
            <option value="low">{t("stockLevelLow", "Low")}</option>
            <option value="overflow">{t("stockLevelOverflow", "Overflow")}</option>
          </select>
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
          {can("Adjust") && <button className="btn btn-edit" disabled={showGrouped || !target}
            onClick={() => setModal("adjust")}>
            <img className="icn-img" src={BTN_ICON.edit} alt="" /> <span>{t("invAdjust", "Adjust")}</span>
          </button>}
          {can("Disposal") && <button className="btn btn-delete" disabled={showGrouped || !target}
            onClick={() => setModal("disposal")}>
            <TrashIcon /> <span>{t("actionDisposal", "Disposal")}</span>
          </button>}
          <CsvButton onClick={onExport} />
        </div>
      </div>

      <div className="tbl-wrap">
        {showGrouped ? (
          <GroupedTable axis={shownAxis} rows={rows} loading={loading} page={page} pageSize={pageSize}
            sort={sort} onSort={toggleSort} />
        ) : (
          <table className="tbl">
            <thead><tr>
              <th className="chk-col">
                <input type="checkbox" className="row-chk"
                  checked={selected.size > 0 && selected.size === flat.length}
                  ref={el => { if (el) el.indeterminate = selected.size > 0 && selected.size < flat.length; }}
                  onChange={toggleAll} />
              </th>
              <SortTh field="no" sort={sort} onSort={toggleSort}>{t("thNo", "No")}</SortTh>
              <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
              <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
              <SortTh field="category" sort={sort} onSort={toggleSort}>{t("thCategory", "Category")}</SortTh>
              <SortTh field="warehouse" sort={sort} onSort={toggleSort}>{t("thWarehouse", "WH")}</SortTh>
              <SortTh field="location_code" sort={sort} onSort={toggleSort}>{t("thLocation", "Location")}</SortTh>
              <SortTh field="lot_no" sort={sort} onSort={toggleSort}>{t("thLot", "Lot")}</SortTh>
              <SortTh field="hold" sort={sort} onSort={toggleSort}>{t("thHold", "Hold")}</SortTh>
              <SortTh field="stock_qty" sort={sort} onSort={toggleSort}>{t("thStockQty", "Stock")}</SortTh>
              <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
              <SortTh field="reorder_point" sort={sort} onSort={toggleSort}>{t("thReorderPt", "Reorder")}</SortTh>
              <SortTh field="over_short" sort={sort} onSort={toggleSort}>{t("thOverShort", "+/-")}</SortTh>
              <SortTh field="updated_at" sort={sort} onSort={toggleSort}>{t("thUpdated", "Updated")}</SortTh>
              <SortTh field="expiry_date" sort={sort} onSort={toggleSort}>{t("thExpirationDate", "Expiration Date")}</SortTh>
              <SortTh field="remaining_days" sort={sort} onSort={toggleSort}>{t("thRemainingDays", "Remaining Days")}</SortTh>
            </tr></thead>
            <tbody>
              {loading && <TableSkeleton cols={COLS} />}
              {!loading && flat.length === 0 && (
                <tr className="no-results-row">
                  <td colSpan={COLS} className="text-center">{t("noResults", "No matching records found.")}</td>
                </tr>
              )}
              {!loading && flat.map((r, i) => {
                const k = keyOf(r);
                return (
                  <tr key={k} data-row-code={k} {...rowProps(k)}
                    className={[flashCls(k), k === cursorCode ? "is-cursor" : ""]
                      .filter(Boolean).join(" ") || undefined}>
                    <td className="chk-col">
                      <input type="checkbox" className="row-chk"
                        checked={selected.has(k)} onChange={() => toggle(k)} />
                    </td>
                    <td>{(page - 1) * pageSize + i + 1}</td>
                    <td>{r.itemCode}</td>
                    <td className="text-left">{dash(r.itemName)}</td>
                    <td>{dash(r.category)}</td>
                    <td>{dash(r.warehouse)}</td>
                    <td>{dash(r.locationCode)}</td>
                    <td>{dash(r.lotNo)}</td>
                    <td className="text-left">
                      <span className={"badge " + (r.isOnHold ? "badge-ng" : "badge-ok")}>
                        {r.isOnHold ? t("holdOn", "On Hold") : t("holdNormal", "Normal")}
                      </span>
                    </td>
                    <td className={"text-right " + stockLevelClass(r)}>{r.stockQty}</td>
                    <td>{dash(r.unit)}</td>
                    <td className="text-right">{dash(r.reorderPoint)}</td>
                    <td className={"text-right " + overShortClass(r.overShort)}>
                      {r.overShort === null ? "—" : (r.overShort > 0 ? `+${r.overShort}` : r.overShort)}
                    </td>
                    <td>{fmtDateTime(r.updatedAt)}</td>
                    <td>{fmtDate(r.expiryDate)}</td>
                    <td className={"text-right " + expiryClass(r.remainingDays)}>
                      {dash(r.remainingDays)}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })}
        onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />

      {modal === "adjust" && target && (
        <AdjustModal row={target} onClose={() => setModal(null)}
          onSaved={async () => {
            setModal(null); await load(); flash(keyOf(target));
            toast.ok(t("toastSaved", "Saved"));
          }} />
      )}

      {modal === "disposal" && target && (
        <DisposalModal row={target} onClose={() => setModal(null)}
          onSaved={async () => {
            setModal(null); await load(); flash(keyOf(target));
            toast.ok(t("toastSaved", "Saved"));
          }} />
      )}
    </div>
  );
}


function GroupedTable({ axis, rows, loading, page, pageSize, sort, onSort }: {
  axis: AggAxis; rows: any[]; loading: boolean; page: number; pageSize: number;
  sort: SortState; onSort: (field: string) => void;
}) {
  const t = useT();
  const byItem = axis === "item";
  const cols = byItem ? 8 : 6;

  return (
    <table className="tbl">
      <thead><tr>
        <th className="chk-col"></th>
        <SortTh field="no" sort={sort} onSort={onSort}>{t("thNo", "No")}</SortTh>
        {byItem ? (
          <>
            <SortTh field="item_code" sort={sort} onSort={onSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={onSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="category" sort={sort} onSort={onSort}>{t("thCategory", "Category")}</SortTh>
            <SortTh field="stock_qty" sort={sort} onSort={onSort}>{t("thStockQty", "Stock")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={onSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="lot_count" sort={sort} onSort={onSort}>{t("aggLotCount", "Lots")}</SortTh>
          </>
        ) : (
          <>
            <SortTh field="location_code" sort={sort} onSort={onSort}>{t("thLocation", "Location")}</SortTh>
            <SortTh field="warehouse" sort={sort} onSort={onSort}>{t("thWarehouse", "WH")}</SortTh>
            <SortTh field="item_count" sort={sort} onSort={onSort}>{t("aggItemCount", "Items")}</SortTh>
            <SortTh field="stock_qty" sort={sort} onSort={onSort}>{t("thStockQty", "Stock")}</SortTh>
          </>
        )}
      </tr></thead>
      <tbody>
        {loading && <TableSkeleton cols={cols} />}
        {!loading && rows.length === 0 && (
          <tr className="no-results-row">
            <td colSpan={cols} className="text-center">{t("noResults", "No matching records found.")}</td>
          </tr>
        )}
        {!loading && rows.map((r, i) => (
          <tr key={r.key}>
            <td className="chk-col"></td>
            <td>{(page - 1) * pageSize + i + 1}</td>
            {byItem ? (
              <>
                <td>{r.key}</td>
                <td className="text-left">{dash(r.name)}</td>
                <td className="text-left">{dash(r.category)}</td>
                <td className="text-right">{r.stockQty}</td>
                <td>{dash(r.unit)}</td>
                <td className="text-right">{r.lotCount}</td>
              </>
            ) : (
              <>
                <td>{r.key}</td>
                <td>{dash(r.warehouse)}</td>
                <td className="text-right">{r.itemCount}</td>
                <td className="text-right">{r.stockQty}</td>
              </>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  );
}


function AdjustModal({ row, onClose, onSaved }: {
  row: StockRow; onClose: () => void; onSaved: () => void;
}) {
  const t = useT();
  const [qty, setQty] = useState(String(row.stockQty));
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const reasons = useRefOptions(
    () => listReasons({ pageSize: 200, category: "Adjustment" }),
    r => ({ value: r.reasonCode, label: codeName(r.reasonCode, r.reasonName) }));

  return (
    <ModalShell title={t("invAdjust", "Adjust")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          await adjustStock({
            itemCode: row.itemCode, serialNo: row.serialNo,
            stockQty: Number(qty), reasonCode: reason,
          });
          onSaved();
        } catch (ex: any) { setErr(errorText(t, ex)); }
        finally { setSaving(false); }
      }}>
      <Field label={t("thItemCode", "Item Code")}>
        <input type="text" value={row.itemCode} readOnly />
      </Field>
      <Field label={t("thItemName", "Item Name")}>
        <input type="text" value={row.itemName ?? ""} readOnly />
      </Field>
      <Field label={t("thLot", "Lot")}>
        <input type="text" value={row.lotNo ?? ""} readOnly />
      </Field>
      <Field label={t("thStockQty", "Stock Qty")}>
        <input type="number" min={0} value={qty} onChange={e => setQty(e.target.value)} required />
      </Field>
      <Field label={t("thAdjustReason", "Adjustment Reason")}>
        <RefSelect value={reason} options={reasons} onChange={setReason} required />
      </Field>
    </ModalShell>
  );
}


function DisposalModal({ row, onClose, onSaved }: {
  row: StockRow; onClose: () => void; onSaved: () => void;
}) {
  const t = useT();
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const reasons = useRefOptions(
    () => listReasons({ pageSize: 200, category: "Defect" }),
    r => ({ value: r.reasonCode, label: codeName(r.reasonCode, r.reasonName) }));

  return (
    <ModalShell title={t("actionDisposal", "Disposal")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          await disposeStock({ itemCode: row.itemCode, serialNo: row.serialNo, reasonCode: reason });
          onSaved();
        } catch (ex: any) { setErr(errorText(t, ex)); }
        finally { setSaving(false); }
      }}>
      <Field label={t("thItemCode", "Item Code")}>
        <input type="text" value={row.itemCode} readOnly />
      </Field>
      <Field label={t("thItemName", "Item Name")}>
        <input type="text" value={row.itemName ?? ""} readOnly />
      </Field>
      <Field label={t("thLot", "Lot")}>
        <input type="text" value={row.lotNo ?? ""} readOnly />
      </Field>
      <Field label={t("thStockQty", "Stock Qty")}>
        <input type="text" value={String(row.stockQty)} readOnly />
      </Field>
      <Field label={t("thDisposalReason", "Disposal Reason")}>
        <RefSelect value={reason} options={reasons} onChange={setReason} required />
      </Field>
    </ModalShell>
  );
}
