import { useScreenCan } from "@/auth/PermissionContext";
import { useState } from "react";
import { exportProductionOrders, getProductionLines, getProductionOrder, getProductionWorks, importProductionOrders, listProductionOrders, setProductionPicking, setProductionStatus, type ProductionDetail, type ProductionLineRow, type ProductionOrderRow, type ProductionWorks } from "@/api/production";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useToast } from "@/lib/Toast";
import { PageFooter } from "@/lib/PageFooter";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { ModalShell } from "@/lib/ModalShell";
import { SearchButton, CsvButton } from "@/lib/Buttons";
import { fmtDateTime } from "@/lib/dateFormat";

const slash = (v?: string | null) => v ? v.replaceAll("-", "/") : "—";
import type { ImportRowError } from "@/api/imports";

const nil = (v: unknown) => v === null || v === undefined || v === "" ? "—" : String(v);
const statusKeys = ["prdStPending", "prdStInProgress", "prdStCompleted", "prdStCanceled"];
const statusNames = ["Pending", "In Progress", "Completed", "Canceled"];
const statusClasses = ["badge-info", "badge-warn", "badge-ok", "badge-cancel"];

function Status({ code }: { code: number }) {
  const t = useT();
  return <span className={`badge ${statusClasses[code] ?? "badge-cancel"}`}>{t(statusKeys[code] ?? "thStatus", statusNames[code] ?? String(code))}</span>;
}

function Flag({ value, count, required = true }: { value: boolean; count?: number; required?: boolean }) {
  const t = useT();
  return <span className={`badge ${!required ? "badge-cancel" : value ? "badge-ok" : "badge-warn"}`}>
    {required ? t(value ? "prdScanYes" : "prdScanNo", value ? "Yes" : "No") : t("prdNotRequired", "Not required")}{count ? ` (${count})` : ""}
  </span>;
}

function Progress({ value }: { value: number }) {
  const p = Math.round(value);
  return <div className="prd-bar"><div className={`prd-bar-fill ${p <= 0 ? "p0" : p >= 100 ? "p100" : "p50"}`} style={{ width: `${Math.max(0, Math.min(100, p))}%` }} /><span className="prd-bar-txt">{p}%</span></div>;
}

function saveCsv(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
  URL.revokeObjectURL(url);
}

export function ProductionOrderPage() { const can = useScreenCan();
  const t = useT();
  const toast = useToast();
  const [unit, setUnit] = useState<"order" | "line">("order");
  const [draft, setDraft] = useState({ orderNo: "", itemCode: "", itemName: "", productionLotNo: "", fgLotNo: "", status: "", from: "", to: "" });
  const [query, setQuery] = useState(draft);
  const [dates, setDates] = useState(true);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [detailNo, setDetailNo] = useState<string | null>(null);
  const [statusOpen, setStatusOpen] = useState(false);
  const [importOpen, setImportOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const filters = { unit, orderNo: query.orderNo, itemCode: query.itemCode, itemName: query.itemName, productionLotNo: query.productionLotNo, fgLotNo: query.fgLotNo, status: query.status ? Number(query.status) : undefined, startDateFrom: dates ? query.from : undefined, startDateTo: dates ? query.to : undefined, page, pageSize };
  const { data, loading, error, reload, refresh } = useFetch(qs("productionOrder", filters), () => listProductionOrders(filters));
  const rows = data?.data ?? [];
  const rowKey = (r: ProductionOrderRow | ProductionLineRow) => `${r.productionOrderNo}/${unit === "line" ? (r as ProductionLineRow).lineNo : ""}`;
  const checked = rows.filter(r => selected.has(rowKey(r)));
  const orderNos = [...new Set(checked.map(r => r.productionOrderNo))];
  const update = (key: keyof typeof draft, value: string) => setDraft(d => ({ ...d, [key]: value }));
  const search = () => { setPage(1); setQuery({ ...draft }); refresh(); };
  const changed = async () => { invalidate("productionOrder"); invalidate("productionResult"); invalidate("inventoryHistory"); await reload(); };
  const action = async (fn: () => Promise<unknown>, message: string) => {
    setBusy(true);
    try { await fn(); setSelected(new Set()); await changed(); toast.ok(message); }
    catch (ex) { toast.err(errorText(t, ex)); }
    finally { setBusy(false); }
  };
  const pick = async () => {
    if (!checked.length) return toast.err(t("alertSelectAtLeastOne", "Please select at least one row."));
    try {
      const items = checked.map(r => ({ orderNo: r.productionOrderNo, ...(unit === "line" ? { lineNo: (r as ProductionLineRow).lineNo } : {}) }));
      const picked = unit === "line" ? checked.every(r => (r as ProductionLineRow).picked) : (await Promise.all(orderNos.map(getProductionLines))).every(lines => lines.length > 0 && lines.every(l => l.picked));
      await action(() => setProductionPicking(items, !picked), t(picked ? "prdPickUndone" : "prdPickDone", picked ? "Material picking cleared" : "Material picking registered"));
    } catch (ex) { toast.err(errorText(t, ex)); }
  };
  const exportCsv = async () => { try { const file = await exportProductionOrders(); saveCsv(file.blob, file.filename); } catch (ex) { toast.err(errorText(t, ex)); } };
  const openStatus = () => {
    if (!orderNos.length) return toast.err(t("alertSelectAtLeastOne", "Please select at least one row."));
    if (new Set(checked.map(r => r.statusCode)).size > 1) return toast.err(t("STS-003", "Select rows with the same status."));
    if (checked.some(r => r.statusCode === 2)) return toast.err(t("STS-002", "Completed rows cannot be changed."));
    setStatusOpen(true);
  };
  const allSelected = rows.length > 0 && rows.every(r => selected.has(rowKey(r)));

  return <div className="pg on" id="pg-prd_import">
    <div className="search-box">
      <div className="search-row">
        <label>{t("prdDisplayUnit", "Display unit")}</label><select value={unit} onChange={e => { setUnit(e.target.value as "order" | "line"); setSelected(new Set()); setPage(1); }}><option value="order">{t("prdUnitOrder", "By order")}</option><option value="line">{t("prdUnitLine", "By line")}</option></select>
        <label>{t("prdOrderNo", "Production Order No")}</label><input value={draft.orderNo} onChange={e => update("orderNo", e.target.value)} onKeyDown={e => e.key === "Enter" && search()} />
        <label>{t("thItemCode", "Item Code")}</label><input value={draft.itemCode} onChange={e => update("itemCode", e.target.value)} onKeyDown={e => e.key === "Enter" && search()} />
        <label>{t("thItemName", "Item Name")}</label><input value={draft.itemName} onChange={e => update("itemName", e.target.value)} onKeyDown={e => e.key === "Enter" && search()} />
        <label>{t("prdProdLot", "Production Lot")}</label><input value={draft.productionLotNo} onChange={e => update("productionLotNo", e.target.value)} onKeyDown={e => e.key === "Enter" && search()} />
        <label>{t("prdFgLot", "Finished Goods Lot")}</label><input value={draft.fgLotNo} onChange={e => update("fgLotNo", e.target.value)} onKeyDown={e => e.key === "Enter" && search()} />
        <label>{t("status", "Status")}</label><select value={draft.status} onChange={e => update("status", e.target.value)}><option value="">{t("warehouseAll", "All")}</option>{statusNames.map((n, i) => <option key={n} value={i}>{t(statusKeys[i], n)}</option>)}</select>
        <label><input type="checkbox" checked={dates} onChange={e => setDates(e.target.checked)} /> {t("prdPlanStart", "Planned Start")}</label><input type="date" disabled={!dates} value={draft.from} onChange={e => update("from", e.target.value)} /><span>–</span><input type="date" disabled={!dates} value={draft.to} onChange={e => update("to", e.target.value)} />
      </div>
      {error && <div className="search-row text-red">{error}</div>}
      <div className="search-btns"><SearchButton onClick={search} />{can("StatusChange") && <button className="btn btn-edit" disabled={busy} onClick={openStatus}><img className="icn-img" src="/Images/Icon/edit_50_000000.svg" alt="" /> {t("prdStatusChange", "Status Change")}</button>}{can("MaterialPicking") && <button className="btn btn-outline" disabled={busy} onClick={pick}>{t("prdPickBtn", "Material Picking")}</button>}{can("Import") && <button className="btn btn-outline" onClick={() => setImportOpen(true)}><img className="icn-img" src="/Images/Icon/upload_50_000000.svg" alt="" /> {t("btnImport", "Import")}</button>}<CsvButton onClick={exportCsv} /></div>
    </div>
    <div className="tbl-wrap"><table className="tbl"><thead><tr><th className="chk-col"><input type="checkbox" className="row-chk" checked={allSelected} onChange={() => setSelected(allSelected ? new Set() : new Set(rows.map(rowKey)))} /></th><th>{t("thNo", "No")}</th><th>{t("prdOrderNo", "Production Order No")}</th>
      {unit === "order" ? <><th>{t("prdOrderDate", "Order Date")}</th><th>{t("prdFactory", "Factory")}</th><th>{t("thItemCode", "Item Code")}</th><th>{t("thItemName", "Item Name")}</th><th>{t("prdPlanned", "Planned")}</th><th>{t("prdProduced", "Produced")}</th><th>{t("thUnit", "Unit")}</th></> : <><th>{t("thItemCode", "Product Code")}</th><th>{t("prdLineNo", "Line No")}</th><th>{t("prdSchedDate", "Production Scheduled Date")}</th><th>{t("prdProcess", "Process")}</th><th>{t("prdMatCode", "Material Code")}</th><th>{t("prdMatName", "Material Name")}</th><th>{t("prdCategory", "Category")}</th><th>{t("prdMatPlanned", "Planned (material)")}</th><th>{t("prdConsumed", "Consumed")}</th><th>{t("thUnit", "Unit")}</th></>}
      <th>{t("prdProdLot", "Production Lot")}</th><th>{t("prdFgLot", "Finished Goods Lot")}</th><th>{t("prdStorageLoc", "Storage Location")}</th>{unit === "line" && <><th>{t("prdPicking", "Material Picking")}</th><th>{t("prdReceipt", "Material Receipt")}</th><th>{t("prdHtWork", "HT Input Work")}</th></>}<th>{t("prdPlanStart", "Planned Start")}</th><th>{t("prdPlanEnd", "Planned Finish")}</th><th>{t("prdActStart", "Actual Start")}</th><th>{t("prdActEnd", "Actual Finish")}</th><th>{t("thStatus", "Status")}</th></tr></thead>
      <tbody>{loading && <TableSkeleton cols={unit === "order" ? 19 : 25} />}{!loading && rows.length === 0 && <tr><td colSpan={unit === "order" ? 19 : 25}>{t("noResults", "No matching records found.")}</td></tr>}{!loading && rows.map((r, i) => { const l = r as ProductionLineRow; const o = r as ProductionOrderRow; return <tr key={rowKey(r)}><td className="chk-col"><input type="checkbox" className="row-chk" checked={selected.has(rowKey(r))} onChange={() => setSelected(s => { const n = new Set(s); n.has(rowKey(r)) ? n.delete(rowKey(r)) : n.add(rowKey(r)); return n; })} /></td><td>{(page - 1) * pageSize + i + 1}</td><td><a className="prd-link" onClick={() => setDetailNo(r.productionOrderNo)}>{r.productionOrderNo}</a></td>
        {unit === "order" ? <><td>{slash(o.orderDate)}</td><td>{o.factoryCode}</td><td>{o.outputItemCode}</td><td className="text-left">{nil(o.outputItemName)}</td><td className="text-right">{o.plannedQty}</td><td className={`text-right ${o.outputQty > o.plannedQty ? "text-red" : ""}`}>{o.outputQty}</td><td>{o.uomCode}</td></> : <><td>{l.outputItemCode}</td><td>{l.lineNo}</td><td>{slash(l.scheduledDate)}</td><td>{l.processCode}</td><td>{l.itemCode}</td><td className="text-left">{nil(l.itemName)}</td><td><span className={`badge ${l.category === "RawMaterials" ? "badge-info" : l.category === "WIP" ? "badge-warn" : l.category === "FinishedGoods" ? "badge-ok" : "badge-cancel"}`}>{nil(l.category)}</span></td><td className="text-right">{l.plannedQty}</td><td className={`text-right ${l.consumedQty > l.plannedQty ? "text-red" : ""}`}>{l.consumedQty}</td><td>{l.uomCode}</td></>}
        <td>{nil(r.productionLotNo)}</td><td>{nil(r.outputLotNo)}</td><td>{nil(r.storageLocationCode)}</td>{unit === "line" && <><td><Flag value={l.picked} /></td><td><Flag value={l.received} /></td><td><Flag value={l.htInputCount > 0} count={l.htInputCount} required={l.htScanRequired} /></td></>}<td>{slash(r.plannedStartDate)}</td><td>{slash(r.plannedEndDate)}</td><td>{r.startedAt ? fmtDateTime(r.startedAt) : "—"}</td><td>{r.completedAt ? fmtDateTime(r.completedAt) : "—"}</td><td><Status code={r.statusCode} /></td></tr>; })}</tbody></table></div>
    <PageFooter total={data?.meta.total ?? 0} page={page} pageSize={pageSize} onGoPage={setPage} onChangeSize={n => { setPageSize(n); setPage(1); }} />
    {detailNo && <OrderDetail no={detailNo} onClose={() => setDetailNo(null)} />}
    {statusOpen && <StatusModal orderNos={orderNos} onClose={() => setStatusOpen(false)} onSaved={changed} />}
    {importOpen && <ProductionImportModal onClose={() => setImportOpen(false)} onSaved={changed} />}
  </div>;
}

function OrderDetail({ no, onClose }: { no: string; onClose: () => void }) {
  const t = useT();
  const { data: detail, error } = useFetch(`productionDetail:${no}`, () => getProductionOrder(no));
  const { data: works } = useFetch(`productionWorks:${no}`, () => getProductionWorks(no));
  const d = detail as ProductionDetail | undefined;
  const w = works as ProductionWorks | undefined;
  const sections: { key: keyof ProductionWorks; title: string; fallback: string }[] = [
    { key: "consumptions", title: "prdConsHistory", fallback: "Material input results" },
    { key: "returns", title: "prdRetHistory", fallback: "Leftover material returns" },
    { key: "outputs", title: "prdOutHistory", fallback: "Finished goods output results" },
    { key: "receipts", title: "prdReceiptHistory", fallback: "Material received (line)" },
    { key: "labels", title: "prdLabelHistory", fallback: "Label issues" },
  ];
  return <div className="modal-overlay" onMouseDown={e => e.target === e.currentTarget && onClose()}><div className="modal-box prd-wide"><div className="modal-head"><span>{t("prdDetailTitle", "Production Order")} {no}</span><span className="modal-close" onClick={onClose}>×</span></div><div className="modal-body">{error && <p className="text-red">{error}</p>}{d && <><table className="tbl prd-kv"><tbody><tr><th>{t("prdOrderDate", "Order Date")}</th><td>{d.order.orderDate}</td><th>{t("prdFactory", "Factory")}</th><td>{d.order.factoryCode}</td><th>{t("thStatus", "Status")}</th><td><Status code={d.order.statusCode} /></td></tr><tr><th>{t("thItemCode", "Item Code")}</th><td>{d.order.outputItemCode}</td><th>{t("prdProdLot", "Production Lot")}</th><td>{nil(d.order.productionLotNo)}</td><th>{t("prdFgLot", "Finished Goods Lot")}</th><td>{nil(d.order.outputLotNo)}</td></tr><tr><th>{t("prdPlanned", "Planned")}</th><td>{d.order.plannedQty} {d.order.uomCode}</td><th>{t("prdProduced", "Produced")}</th><td>{d.outputQty} {d.order.uomCode}</td><th>{t("prdProgress", "Progress")}</th><td><Progress value={d.progress.percent} /></td></tr><tr><th>{t("prdPicking", "Material Picking")}</th><td>{d.pickedStatus}</td><th>{t("prdReceipt", "Material Receipt")}</th><td>{d.receiptStatus}</td><th>{t("prdHtWork", "HT Input Work")}</th><td>{d.htInputStatus}</td></tr></tbody></table><h4 className="prd-h">{t("prdLines", "Lines (process / material)")}</h4><div className="tbl-wrap"><table className="tbl"><thead><tr>{["prdLineNo", "prdSchedDate", "prdProcess", "thItemCode", "thItemName", "prdCategory", "prdMatPlanned", "prdConsumed", "thUnit", "prdPicking", "prdReceipt", "prdHtWork", "thStatus"].map(k => <th key={k}>{t(k, k)}</th>)}</tr></thead><tbody>{d.lines.map(l => <tr key={l.no}><td>{l.lineNo}</td><td>{l.scheduledDate}</td><td>{l.processCode}</td><td>{l.itemCode}</td><td>{nil(l.itemName)}</td><td>{nil(l.category)}</td><td className="text-right">{l.plannedQty}</td><td className="text-right">{l.consumedQty}</td><td>{l.uomCode}</td><td><Flag value={l.picked} /></td><td><Flag value={l.received} /></td><td><Flag value={l.htInputCount > 0} count={l.htInputCount} required={l.htScanRequired} /></td><td><Status code={l.statusCode} /></td></tr>)}</tbody></table></div></>}{w && sections.map(s => <div key={s.key}><h4 className="prd-h">{t(s.title, s.fallback)}</h4><div className="tbl-wrap"><table className="tbl"><thead><tr><th>{t("prdDateTime", "Date/Time")}</th><th>{t("prdType", "Type")}</th><th>{t("thItemCode", "Item Code")}</th><th>{t("prdLotNo", "Lot No")}</th><th>{t("thQty", "Qty")}</th><th>{t("prdWorker", "Worker")}</th></tr></thead><tbody>{(w[s.key] as Record<string, unknown>[]).map((r, i) => <tr key={i}><td>{r.createdAt ? fmtDateTime(String(r.createdAt)) : "—"}</td><td>{s.fallback}</td><td>{nil(r.itemCode)}</td><td>{nil(r.lotNo)}</td><td>{nil(r.qty ?? r.totalQty)}</td><td>{nil(r.createdBy)}</td></tr>)}</tbody></table></div></div>)}</div><div className="modal-actions"><button className="btn btn-cancel" onClick={onClose}>{t("modalClose", "Close")}</button></div></div></div>;
}

function StatusModal({ orderNos, onClose, onSaved }: { orderNos: string[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const t = useT(); const [status, setStatus] = useState(""); const [busy, setBusy] = useState(false); const [err, setErr] = useState<string | null>(null);
  return <ModalShell title={t("prdStatusChange", "Status Change")} onClose={onClose} saving={busy} err={err} submitDisabled={!status} onSubmit={async e => { e.preventDefault(); setBusy(true); try { await setProductionStatus(orderNos, status); await onSaved(); onClose(); } catch (ex) { setErr(errorText(t, ex)); } finally { setBusy(false); } }}><div className="mfg"><label>{t("scmSelectedCount", "Selected Rows")}</label><input readOnly value={orderNos.length} /></div><div className="mfg prd-radios"><label>{t("scmSelectStatus", "New Status")}</label>{[["done", "prdStCompleted", "Completed"], ["notDone", "prdStNotStarted", "Not Started"], ["cancelled", "statusCancelled", "Cancelled"]].map(([v, k, f]) => <label key={v}><input type="radio" checked={status === v} onChange={() => setStatus(v)} />{t(k, f)}</label>)}</div></ModalShell>;
}

function ProductionImportModal({ onClose, onSaved }: { onClose: () => void; onSaved: () => Promise<void> }) {
  const t = useT(); const [file, setFile] = useState<File | null>(null); const [errors, setErrors] = useState<ImportRowError[]>([]); const [busy, setBusy] = useState(false); const [err, setErr] = useState<string | null>(null);
  return <ModalShell title={t("btnImport", "Import")} onClose={onClose} saving={busy} err={err} submitLabel={t("btnImport", "Import")} submitDisabled={!file} onSubmit={async e => { e.preventDefault(); if (!file) return; setBusy(true); setErr(null); try { const result = await importProductionOrders(file); if (result.errors.length) setErrors(result.errors); else { await onSaved(); onClose(); } } catch (ex) { setErr(errorText(t, ex)); } finally { setBusy(false); } }}><div className="mfg"><label>{t("thFile", "File")}</label><input type="file" accept=".csv" onChange={e => setFile(e.target.files?.[0] ?? null)} /></div><p className="prd-note">{t("impAllOrNothing", "If any row is invalid, nothing is imported.")}</p>{errors.length > 0 && <div><strong>{t("impFailedRows", "Nothing was imported. Fix these rows and try again:")}</strong><div className="tbl-wrap"><table className="alert-mini-tbl"><thead><tr><th>{t("csvRow", "Row")}</th><th>{t("thReason", "Reason")}</th></tr></thead><tbody>{errors.map((r, i) => <tr key={i}><td>{r.row}</td><td>{t(r.code, r.message)} {r.detail}</td></tr>)}</tbody></table></div></div>}</ModalShell>;
}
