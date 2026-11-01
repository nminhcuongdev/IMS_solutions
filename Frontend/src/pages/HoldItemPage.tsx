import { useCallback, useMemo, useState } from "react";
import {
  exportStockCsv, holdStock, listStock, releaseHold,
  type StockKey, type StockQuery, type StockRow,
} from "@/api/inventory";
import { listItemGroups } from "@/api/itemGroups";
import { CsvButton, SearchButton } from "@/lib/Buttons";
import { listReasons } from "@/api/reasons";
import { Field, ModalShell } from "@/lib/ModalShell";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { codeName, RefSelect, useRefOptions } from "@/lib/RefSelect";
import { useRowSelect } from "@/lib/rowSelect";
import { SortTh, useSort } from "@/lib/SortableTh";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useUrlNumber, useUrlPatch, useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";


const COLS = 10;

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

const keyOf = (r: { itemCode: string; serialNo: string }) => `${r.itemCode}/${r.serialNo}`;
const parseKey = (k: string): StockKey => {
  const i = k.indexOf("/");
  return { itemCode: k.slice(0, i), serialNo: k.slice(i + 1) };
};

export function HoldItemPage() {
  const t = useT();
  const toast = useToast();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);

  const [category, setCategory] = useUrlString("cat");
  const [lot] = useUrlString("lot");
  const [hold, setHold] = useUrlString("hold");
  const [itemCode] = useUrlString("ic");
  const [itemName] = useUrlString("in");
  const [icDraft, setIcDraft] = useState(itemCode);
  const [inDraft, setInDraft] = useState(itemName);
  const [lotDraft, setLotDraft] = useState(lot);

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState(false);
  const [busy, setBusy] = useState(false);

  const { sort, toggleSort } = useSort("item_code");

  const filters: StockQuery = useMemo(() => ({
    category: category || undefined,
    lot: lot || undefined,
    hold: hold || undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    sort: sort.value,
  }), [itemCode, itemName, category, lot, hold, sort.value]);

  const { data, loading, error: err, reload, refresh } = useFetch(
    qs("stockHold", { ...filters, page, pageSize }),
    () => listStock({ ...filters, page, pageSize }));
  const rows: StockRow[] = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => {
    invalidate("stockHold"); invalidate("stock");
    await reload();
  }, [reload]);

  const toggle = (c: string) => setSelected(p => {
    const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s;
  });
  const toggleAll = () => setSelected(p =>
    p.size === rows.length ? new Set() : new Set(rows.map(keyOf)));
  const rowProps = useRowSelect(toggle, () => { });

  const one = selected.size === 1
    ? rows.find(r => selected.has(keyOf(r))) ?? null
    : null;

  const report = (updated: number, skipped: string[]) => {
    if (skipped.length) toast.err(`${t("toastSkipped", "Skipped")}: ${skipped.join(", ")}`);
    toast.ok(`${t("toastUpdated", "Updated")}: ${updated}`);
  };

  const onRelease = async () => {
    if (selected.size === 0 || busy) return;
    setBusy(true);
    try {
      const res = await releaseHold([...selected].map(parseKey));
      setSelected(new Set());
      await load();
      report(res.updated, res.skipped);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
    finally { setBusy(false); }
  };

  const search = () => {
    patch({ page: null, ic: icDraft || null, "in": inDraft || null, lot: lotDraft || null }); refresh();
  };
  const onEnter = (e: { key: string }) => { if (e.key === "Enter") search(); };

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
    <div className="pg on" id="pg-inv_hold">
      <div className="search-box">
        <div className="search-row">
          <label>{t("thItemCode", "Item Code")}</label>
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
          <label className="ml-3.5">{t("thHold", "Hold")}</label>
          <select className="w-32.5" value={hold}
            onChange={e => { setPage(1); setHold(e.target.value); }}>
            <option value="">{t("holdFilterAll", "All")}</option>
            <option value="hold">{t("holdOn", "On Hold")}</option>
            <option value="notHold">{t("holdFilterNotHold", "Not on Hold")}</option>
          </select>
        </div>

        {err && <div className="search-row text-rose-600 text-[12px]">{err}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
          <button className="btn btn-special" disabled={selected.size !== 1 || busy}
            title={t("holdOneRowNote", "Select exactly one row to hold.")}
            onClick={() => setModal(true)}>
            <svg className="ic" viewBox="0 0 24 24"><path d="M12 2a5 5 0 0 0-5 5v3H6a1 1 0 0 0-1 1v9a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-9a1 1 0 0 0-1-1h-1V7a5 5 0 0 0-5-5zm3 8H9V7a3 3 0 0 1 6 0v3z" /></svg>
            {" "}<span>{t("btnHold", "Hold")}</span>
          </button>
          <button className="btn btn-edit" disabled={selected.size === 0 || busy}
            title={t("holdSkipNote", "Rows already in the requested state are skipped and reported back.")}
            onClick={onRelease}>
            <svg className="ic" viewBox="0 0 24 24"><path d="M17 8V7a5 5 0 0 0-9.9-1h2.1a3 3 0 0 1 5.8 1v1H6a1 1 0 0 0-1 1v9a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-9a1 1 0 0 0-1-1h-1z" /></svg>
            {" "}<span>{t("btnReleaseHold", "Release Hold")}</span>
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
            <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
            <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
            <SortTh field="category" sort={sort} onSort={toggleSort}>{t("thCategory", "Category")}</SortTh>
            <SortTh field="lot_no" sort={sort} onSort={toggleSort}>{t("thLot", "Lot")}</SortTh>
            <SortTh field="stock_qty" sort={sort} onSort={toggleSort}>{t("thStockQty", "Stock")}</SortTh>
            <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
            <SortTh field="hold" sort={sort} onSort={toggleSort}>{t("thHold", "Hold")}</SortTh>
            <SortTh field="hold_reason" sort={sort} onSort={toggleSort}>{t("thHoldReason", "Hold Reason")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={COLS} />}
            {!loading && rows.length === 0 && (
              <tr className="no-results-row">
                <td colSpan={COLS} className="text-center">{t("noResults", "No matching records found.")}</td>
              </tr>
            )}
            {!loading && rows.map((r, i) => {
              const k = keyOf(r);
              return (
                <tr key={k} data-row-code={k} {...rowProps(k)}>
                  <td className="chk-col">
                    <input type="checkbox" className="row-chk"
                      checked={selected.has(k)} onChange={() => toggle(k)} />
                  </td>
                  <td>{(page - 1) * pageSize + i + 1}</td>
                  <td>{r.itemCode}</td>
                  <td className="text-left">{dash(r.itemName)}</td>
                  <td>{dash(r.category)}</td>
                  <td>{dash(r.lotNo)}</td>
                  <td className="text-right">{r.stockQty}</td>
                  <td>{dash(r.unit)}</td>
                  <td className="text-left">
                    <span className={"badge " + (r.isOnHold ? "badge-ng" : "badge-ok")}>
                      {r.isOnHold ? t("holdOn", "On Hold") : t("holdNormal", "Normal")}
                    </span>
                  </td>
                  <td className="text-left">{dash(r.holdReason)}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })}
        onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />

      {modal && one && (
        <HoldModal
          row={one}
          onClose={() => setModal(false)}
          onSubmit={async reasonCode => {
            setBusy(true);
            try {
              const res = await holdStock([...selected].map(parseKey), reasonCode);
              setModal(false); setSelected(new Set());
              await load();
              report(res.updated, res.skipped);
            } catch (ex: any) { toast.err(errorText(t, ex)); }
            finally { setBusy(false); }
          }} />
      )}
    </div>
  );
}


function HoldModal({ row, onClose, onSubmit }: {
  row: StockRow; onClose: () => void; onSubmit: (reasonCode: string) => void;
}) {
  const t = useT();
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);

  const reasons = useRefOptions(
    () => listReasons({ pageSize: 200, category: "Defect" }),
    r => ({ value: r.reasonCode, label: codeName(r.reasonCode, r.reasonName) }));

  return (
    <ModalShell title={t("btnHold", "Hold")} onClose={onClose} saving={saving}
      onSubmit={e => { e.preventDefault(); setSaving(true); onSubmit(reason); }}>
      <Field label={t("thItemCode", "Item Code")}>
        <input type="text" value={row.itemCode} readOnly />
      </Field>
      <Field label={t("thItemName", "Item Name")}>
        <input type="text" value={row.itemName ?? ""} readOnly />
      </Field>
      <Field label={t("thLot", "Lot")}>
        <input type="text" value={row.lotNo ?? ""} readOnly />
      </Field>
      <Field label={t("thHoldReason", "Hold Reason")}>
        <RefSelect value={reason} options={reasons} onChange={setReason} required />
      </Field>
    </ModalShell>
  );
}
