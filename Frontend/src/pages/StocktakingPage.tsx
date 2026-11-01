import { CsvButton, SearchButton } from "@/lib/Buttons";
import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import {
  completeStocktaking, exportStocktakingLabelsCsv, finalizeStocktaking,
  getStocktakingSession, listStocktakingGroups, listStocktakingLabels, startStocktaking,
  ST_COMPLETED, ST_IN_PROGRESS,
  type LabelQuery, type StocktakingGroupRow, type StocktakingLabelRow, type StocktakingSession,
} from "@/api/stocktaking";
import { listLocations } from "@/api/locations";
import { Field, ModalShell } from "@/lib/ModalShell";
import { PageFooter } from "@/lib/PageFooter";
import { SortTh, useSort } from "@/lib/SortableTh";
import { PAGE_SIZES } from "@/lib/Pager";
import { useRefOptions } from "@/lib/RefSelect";
import { useRowSelect } from "@/lib/rowSelect";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useUrlNumber, useUrlPatch, useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";
import type { ApiListSuccess } from "@/api/types";
import { errorText } from "@/lib/errorText";


const MODEL_COLS = 9;
const LABEL_COLS = 12;

const dash = (v: string | number | null | undefined) =>
  v === null || v === undefined || v === "" ? "—" : String(v);

const stamp = (v: string | null) =>
  v ? new Date(v).toLocaleString("sv-SE").replace("T", " ").slice(0, 19).replace(/-/g, "/") : "—";

export function StocktakingPage() {
  const t = useT();
  const toast = useToast();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);
  const { sort, toggleSort } = useSort("no");

  const [view, setView] = useUrlString("view", "model");
  const [locationCode, setLocationCode] = useUrlString("loc");
  const [itemCode, setItemCode] = useUrlString("code");
  const [itemName, setItemName] = useUrlString("name");
  const [notScanned, setNotScanned] = useState(false);
  const [statusDiff, setStatusDiff] = useState(false);

  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [startModal, setStartModal] = useState(false);
  const [finalizeModal, setFinalizeModal] = useState(false);
  const [busy, setBusy] = useState(false);

  const session = useFetch("stSession", getStocktakingSession);
  const s: StocktakingSession | null = session.data ?? null;
  const running = s !== null;

  const filters: LabelQuery = useMemo(() => ({
    locationCode: locationCode || undefined,
    itemCode: itemCode || undefined,
    itemName: itemName || undefined,
    notScanned: notScanned || undefined,
    statusDiff: statusDiff || undefined,
    sort: sort.value,
  }), [locationCode, itemCode, itemName, notScanned, statusDiff, sort.value]);

  const key = qs("stLabels", { ...filters, view, page, pageSize });
  const labels = useFetch(key, (): Promise<ApiListSuccess<StocktakingLabelRow | StocktakingGroupRow>> =>
    view === "model"
      ? listStocktakingGroups({ ...filters, page, pageSize })
      : listStocktakingLabels({ ...filters, page, pageSize }));

  const rows = labels.data?.data ?? [];
  const total = labels.data?.meta.total ?? 0;

  const reloadAll = useCallback(async () => {
    invalidate("stLabels"); invalidate("stSession");
    await Promise.all([session.reload(), labels.reload()]);
  }, [session.reload, labels.reload]);

  useEffect(() => {
    if (s?.status === ST_COMPLETED && view === "model") setView("label");
  }, [s?.status, view, setView]);

  const locations = useRefOptions(
    () => listLocations({ pageSize: 200, type: "Small" }),
    l => ({ value: l.locationCode, label: l.locationCode }));

  const labelRows = view === "label" ? (rows as unknown as StocktakingLabelRow[]) : [];
  const groupRows = view === "model" ? (rows as unknown as StocktakingGroupRow[]) : [];

  const toggle = (c: string) => setSelected(p => {
    const n = new Set(p); n.has(c) ? n.delete(c) : n.add(c); return n;
  });
  const rowProps = useRowSelect(toggle, () => { });

  const selectable = labelRows.filter(r => !r.isScanned).map(r => r.serialNo);
  const toggleAll = () => setSelected(p =>
    selectable.every(x => p.has(x)) && p.size > 0 ? new Set() : new Set(selectable));

  const totals = useMemo(() => {
    const picked = labelRows.filter(r => selected.has(r.serialNo));
    return {
      ok: picked.filter(r => !r.isExpired).length,
      expired: picked.filter(r => r.isExpired).length,
      count: picked.length,
    };
  }, [labelRows, selected]);

  const onStart = async (name: string) => {
    setBusy(true);
    try {
      await startStocktaking(name);
      setStartModal(false);
      await reloadAll();
      toast.ok(t("slStarted", "Stocktaking started."));
    } catch (ex: any) { toast.err(errorText(t, ex)); }
    finally { setBusy(false); }
  };

  const onComplete = async () => {
    setBusy(true);
    try {
      await completeStocktaking();
      setSelected(new Set());
      await reloadAll();
      toast.ok(t("slCompleted", "Stocktaking closed for counting."));
    } catch (ex: any) { toast.err(errorText(t, ex)); }
    finally { setBusy(false); }
  };

  const onFinalize = async () => {
    setBusy(true);
    try {
      await finalizeStocktaking([...selected]);
      setFinalizeModal(false);
      setSelected(new Set());
      setView("model");
      await reloadAll();
      toast.ok(t("slFinalized", "Stocktaking finalized."));
    } catch (ex: any) { toast.err(errorText(t, ex)); }
    finally { setBusy(false); }
  };

  const onExport = async () => {
    try {
      const { blob, filename } = await exportStocktakingLabelsCsv({ ...filters, view: view as any });
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
  };

  const badgeKey = s?.statusLabelKey ?? "slStatusBeforeBadge";
  const badgeClass = !s ? "badge-info" : s.status === ST_COMPLETED ? "badge-ok" : "badge-warn";
  const badgeFallback = !s ? "Before Stocktaking" : s.status === ST_COMPLETED ? "Counting Closed" : "In Progress";

  return (
    <div className="pg on" id="pg-st_label">
      <div className="search-box">
        <div className="search-row">
          <label>{t("thLocation", "Location")}</label>
          <select className="w-47.5" value={locationCode}
            onChange={e => { setPage(1); setLocationCode(e.target.value); }}>
            <option value="">{t("warehouseAll", "All")}</option>
            {locations.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
          </select>

          <label className="ml-3.5">{t("thItemCode", "Item Code")}</label>
          <input className="w-35" type="text" value={itemCode}
            onChange={e => { setPage(1); setItemCode(e.target.value); }} />

          <label className="ml-3.5">{t("thItemName", "Item Name")}</label>
          <input className="w-40" type="text" value={itemName}
            onChange={e => { setPage(1); setItemName(e.target.value); }} />

          <label className="ml-3.5">{t("aggAxisLabel", "Aggregation")}</label>
          <select className="w-47.5" value={view}
            onChange={e => { setPage(1); setSelected(new Set()); setView(e.target.value); }}>
            <option value="model" disabled={s?.status === ST_COMPLETED}>
              {t("slViewModel", "By Location")}
            </option>
            <option value="label">{t("slViewLabel", "By Label")}</option>
          </select>
        </div>

        {running && (
          <div className="search-row">
            <label className="inline-flex items-center gap-[5px] text-[13px]">
              <input type="checkbox" checked={notScanned}
                onChange={e => { setPage(1); setNotScanned(e.target.checked); }} />
              <span>{t("slFilterNotScanned", "Not Yet Scanned")}</span>
            </label>
            <label className="inline-flex items-center gap-[5px] text-[13px] ml-4">
              <input type="checkbox" checked={statusDiff}
                onChange={e => { setPage(1); setStatusDiff(e.target.checked); }} />
              <span>{t("slFilterStatusDiff", "Status Mismatch")}</span>
            </label>
          </div>
        )}

        {labels.error && (
          <div className="search-row text-rose-600 text-[12px]">{labels.error}</div>
        )}

        <div className="search-btns">
          <SearchButton onClick={() => { setPage(1); labels.refresh(); session.refresh(); }} />
          <CsvButton onClick={onExport} />
        </div>
      </div>

      <div className="mb-2.5 flex flex-wrap items-center gap-3.5 rounded-xl bg-white px-3.5 py-2.5 shadow-sm ring-1 ring-slate-200">
        <div className="flex flex-wrap items-center gap-2.5">
          <span className={"badge " + badgeClass}>{t(badgeKey, badgeFallback)}</span>
          <span className="text-[13px] font-bold text-slate-800">{s?.sessionName ?? ""}</span>
          <span className="text-xs whitespace-pre-wrap text-slate-500">
            {s?.startedAt && <>{t("slStartedAt", "Started")}: {stamp(s.startedAt)}</>}
            {s?.completedAt && <>{"   "}{t("slCompletedAt", "Previous Completion")}: {stamp(s.completedAt)}</>}
            {s && <>{"   "}{t("slScannedQty", "Scanned")}: {s.scannedCount}/{s.labelCount}</>}
          </span>
        </div>
        <div className="ml-auto flex gap-1.5">
          <button className="btn btn-success" disabled={running || busy}
            onClick={() => setStartModal(true)}>{t("slStart", "Start")}</button>
          <button className="btn btn-special" disabled={s?.status !== ST_IN_PROGRESS || busy}
            onClick={onComplete}>{t("slCompleteBtn", "Complete")}</button>
          <button className="btn btn-primary" disabled={s?.status !== ST_COMPLETED || busy}
            onClick={() => setFinalizeModal(true)}>{t("slFinalize", "Finalize")}</button>
        </div>
      </div>

      {view === "label" && (
        <div className="mb-2 flex items-center gap-2">
          <button className="rounded-lg border border-slate-300 bg-white px-2 py-[5px] text-slate-600 hover:bg-slate-50" title={t("slSelectAll", "Select All")}
            onClick={() => setSelected(new Set(selectable))}>
            <svg className="size-4 fill-current" viewBox="0 0 24 24"><path d="M9.5 17.6 4.4 12.5a1.2 1.2 0 0 1 1.7-1.7l3.4 3.4 8.7-8.7a1.2 1.2 0 0 1 1.7 1.7l-10 10a1.2 1.2 0 0 1-1.7 0z" /></svg>
          </button>
          <button className="rounded-lg border border-slate-300 bg-white px-2 py-[5px] text-slate-600 hover:bg-slate-50" title={t("slDeselectAll", "Deselect All")}
            onClick={() => setSelected(new Set())}>
            <svg className="size-4 fill-current" viewBox="0 0 24 24"><path d="M18.3 5.71a1 1 0 0 0-1.41 0L12 10.59 7.11 5.7A1 1 0 0 0 5.7 7.11L10.59 12l-4.89 4.89a1 1 0 1 0 1.41 1.41L12 13.41l4.89 4.89a1 1 0 0 0 1.41-1.41L13.41 12l4.89-4.89a1 1 0 0 0 0-1.4z" /></svg>
          </button>
          <div className="ml-2 flex flex-wrap gap-4 text-[12.5px] text-slate-600 [&_b]:text-slate-800">
            <span>{t("slWithinExpiry", "Within Expiry")}: <b>{totals.ok}</b></span>
            <span>{t("slExpired", "Expired")}: <b>{totals.expired}</b></span>
            <span>{t("slLabelCount", "Labels")}: <b>{totals.count}</b></span>
          </div>
        </div>
      )}

      <div className="tbl-wrap">
        {view === "model" ? (
          <table className="tbl">
            <thead><tr>
              <SortTh field="no" sort={sort} onSort={toggleSort}>{t("thNo", "No")}</SortTh>
              <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
              <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
              <SortTh field="location_code" sort={sort} onSort={toggleSort}>{t("thLocation", "Location")}</SortTh>
              <SortTh field="lot_no" sort={sort} onSort={toggleSort}>{t("thLot", "Lot")}</SortTh>
              <SortTh field="planned_qty" sort={sort} onSort={toggleSort}>{t("slPlannedQty", "Planned")}</SortTh>
              <SortTh field="result_qty" sort={sort} onSort={toggleSort}>{t("slScannedQty", "Scanned")}</SortTh>
              <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
              <SortTh field="updated_at" sort={sort} onSort={toggleSort}>{t("thUpdatedTime", "Last Updated Time")}</SortTh>
            </tr></thead>
            <tbody>
              {labels.loading && <TableSkeleton cols={MODEL_COLS} />}
              {!labels.loading && groupRows.length === 0 && (
                <tr className="no-results-row">
                  <td colSpan={MODEL_COLS} className="text-center">
                    {running ? t("noResults", "No matching records found.")
                      : t("slNotStarted", "No stocktaking is running. Press Start to take a snapshot.")}
                  </td>
                </tr>
              )}
              {!labels.loading && groupRows.map((r, i) => (
                <tr key={`${r.itemCode}|${r.lotNo ?? ""}`}>
                  <td>{(page - 1) * pageSize + i + 1}</td>
                  <td>{r.itemCode}</td>
                  <td className="text-left">{dash(r.itemName)}</td>
                  <td>{dash(r.locationText)}</td>
                  <td>{dash(r.lotNo)}</td>
                  <td className="text-right">{r.plannedQty}</td>
                  <td className={"text-right" + (r.resultQty < r.plannedQty ? " text-red" : "")}>
                    {r.resultQty}
                  </td>
                  <td>{dash(r.unit)}</td>
                  <td>{stamp(r.updatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <table className="tbl">
            <thead><tr>
              <th className="chk-col">
                <input type="checkbox" className="row-chk"
                  checked={selectable.length > 0 && selectable.every(x => selected.has(x))}
                  ref={el => {
                    if (el) el.indeterminate = selected.size > 0 && selected.size < selectable.length;
                  }}
                  onChange={toggleAll} />
              </th>
              <SortTh field="no" sort={sort} onSort={toggleSort}>{t("thNo", "No")}</SortTh>
              <SortTh field="serial_no" sort={sort} onSort={toggleSort}>{t("slSerial", "QR Serial")}</SortTh>
              <SortTh field="item_code" sort={sort} onSort={toggleSort}>{t("thItemCode", "Item Code")}</SortTh>
              <SortTh field="item_name" sort={sort} onSort={toggleSort}>{t("thItemName", "Item Name")}</SortTh>
              <SortTh field="qty" sort={sort} onSort={toggleSort}>{t("thQty", "Qty")}</SortTh>
              <SortTh field="unit" sort={sort} onSort={toggleSort}>{t("thUnit", "Unit")}</SortTh>
              <SortTh field="location_code" sort={sort} onSort={toggleSort}>{t("thLocation", "Location")}</SortTh>
              <SortTh field="lot_no" sort={sort} onSort={toggleSort}>{t("thLot", "Lot")}</SortTh>
              <SortTh field="status" sort={sort} onSort={toggleSort}>{t("thStatus", "Status")}</SortTh>
              <SortTh field="is_scanned" sort={sort} onSort={toggleSort}>{t("slScannedQty", "Scanned")}</SortTh>
              <SortTh field="updated_at" sort={sort} onSort={toggleSort}>{t("thUpdatedTime", "Last Updated Time")}</SortTh>
            </tr></thead>
            <tbody>
              {labels.loading && <TableSkeleton cols={LABEL_COLS} />}
              {!labels.loading && labelRows.length === 0 && (
                <tr className="no-results-row">
                  <td colSpan={LABEL_COLS} className="text-center">
                    {running ? t("noResults", "No matching records found.")
                      : t("slNotStarted", "No stocktaking is running. Press Start to take a snapshot.")}
                  </td>
                </tr>
              )}
              {!labels.loading && labelRows.map((r, i) => (
                <tr key={r.serialNo} data-row-code={r.serialNo} {...rowProps(r.serialNo)}>
                  <td className="chk-col">
                    <input type="checkbox" className="row-chk" disabled={r.isScanned}
                      checked={selected.has(r.serialNo)} onChange={() => toggle(r.serialNo)} />
                  </td>
                  <td>{(page - 1) * pageSize + i + 1}</td>
                  <td>{r.serialNo}</td>
                  <td>{r.itemCode}</td>
                  <td className="text-left">{dash(r.itemName)}</td>
                  <td className="text-right">{r.qty}</td>
                  <td>{dash(r.unit)}</td>
                  <td>{r.locationCode}</td>
                  <td>{dash(r.lotNo)}</td>
                  <td className="text-left">{t(r.statusLabelKey, "Normal")}</td>
                  <td className="text-left">
                    {r.isScanned ? t("slScannedYes", "Scanned") : t("slFilterNotScanned", "Not Yet Scanned")}
                  </td>
                  <td>{stamp(r.updatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <PageFooter total={total} page={page} pageSize={pageSize}
        onGoPage={p => setPage(p, { push: true })}
        onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />

      {startModal && <StartModal busy={busy} onClose={() => setStartModal(false)} onSubmit={onStart} />}

      {finalizeModal && (
        <FinalizeModal
          count={selected.size}
          sessionName={s?.sessionName ?? ""}
          busy={busy}
          onClose={() => setFinalizeModal(false)}
          onSubmit={onFinalize} />
      )}
    </div>
  );
}


function StartModal({ busy, onClose, onSubmit }: {
  busy: boolean;
  onClose: () => void;
  onSubmit: (name: string) => void;
}) {
  const t = useT();
  const [name, setName] = useState("");
  const [err, setErr] = useState<string | null>(null);

  const submit = (e: FormEvent) => {
    e.preventDefault();
    if (!name.trim()) { setErr(t("slAlertNameRequired", "Please enter a stocktaking name.")); return; }
    onSubmit(name.trim());
  };

  return (
    <ModalShell title={t("slStart", "Start")} onClose={onClose} onSubmit={submit} saving={busy} err={err}>
      <Field label={t("stName", "Stocktaking Name")}>
        <input type="text" value={name} autoFocus
          placeholder={t("slNamePlaceholder", "First half of 2026")}
          onChange={e => { setName(e.target.value); setErr(null); }} />
      </Field>
      <div className="text-[12px] text-slate-500 mt-2">
        {t("slStartNote", "Starting takes a snapshot of every stocked label. The previous snapshot is replaced.")}
      </div>
    </ModalShell>
  );
}


function FinalizeModal({ count, sessionName, busy, onClose, onSubmit }: {
  count: number;
  sessionName: string;
  busy: boolean;
  onClose: () => void;
  onSubmit: () => void;
}) {
  const t = useT();
  return (
    <ModalShell title={t("slFinalize", "Finalize")} onClose={onClose}
      onSubmit={e => { e.preventDefault(); onSubmit(); }} saving={busy}>
      <div className="text-[13px] mb-2.5">
        {t("slFinalizeMsg1", "The checked")} <b>{count}</b> {t("slFinalizeMsg2", "label(s) will be changed to Missing.")}
      </div>
      <Field label={t("thReason", "Reason")}>
        <input type="text" readOnly
          value={`${t("slFinalizeReason", "Not found in stock-taking")} "${sessionName}"`} />
      </Field>
      <div className="text-[12px] text-slate-500 mt-2">
        {t("slFinalizeStockNote",
          "Stock quantities are not changed. Adjust them on the Stock Inquiry screen.")}
      </div>
    </ModalShell>
  );
}
