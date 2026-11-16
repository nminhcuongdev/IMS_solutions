import { useScreenCan } from "@/auth/PermissionContext";
import { useState } from "react";
import { exportTrace, getTrace, getTraceBalance, getTraceEvents, listTraceLots, type TraceBalance, type TraceDirection, type TraceEvent, type TraceLot, type TraceNode, type TraceResult, type TraceSearch } from "@/api/traceability";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";
import { useToast } from "@/lib/Toast";

type SearchValues = Record<string, string>;
type DateKey = "productionDate" | "shipped" | "received";
type TreeEntry = { index: number; node: TraceNode; children: TreeEntry[] };

const initial: SearchValues = { orderNo: "", productionLotNo: "", productionDateFrom: "", productionDateTo: "", fgLotNo: "", soNo: "", customer: "", shippedFrom: "", shippedTo: "", materialLotNo: "", poNo: "", receivedFrom: "", receivedTo: "" };
const kindInfo: Record<string, { icon: string; cls: string; key: string; fallback: string }> = {
  lot: { icon: "box_50_000000.svg", cls: "k-lot", key: "prdKindLot", fallback: "Material lot" },
  "lot-made": { icon: "inventory_50_000000.svg", cls: "k-made", key: "prdKindMade", fallback: "Produced lot" },
  process: { icon: "precision_manufacturing_50_000000.svg", cls: "k-proc", key: "prdKindProc", fallback: "Process" },
  receiving: { icon: "warehouse_50_000000.svg", cls: "k-recv", key: "prdKindRecv", fallback: "Receiving" },
  receipt: { icon: "warehouse_50_000000.svg", cls: "k-recv", key: "prdKindRecv", fallback: "Receiving" },
  shipping: { icon: "delivery_truck_speed_50_000000.svg", cls: "k-ship", key: "prdKindShip", fallback: "Shipping" },
  ship: { icon: "delivery_truck_speed_50_000000.svg", cls: "k-ship", key: "prdKindShip", fallback: "Shipping" },
  return: { icon: "restart_alt_50_000000.svg", cls: "k-ret", key: "prdKindRet", fallback: "Return" },
  unknown: { icon: "warning_50_000000.svg", cls: "k-unk", key: "prdKindUnk", fallback: "Unknown lot" },
};
const eventKeys: Record<string, string> = { receiving: "prdEvReceiving", consume: "prdEvConsume", consumption: "prdEvConsume", produce: "prdEvProduce", output: "prdEvProduce", production_return: "prdEvReturn", prodreturn: "prdEvReturn", shipping: "prdEvShipping", return: "prdEvReturn", adjust: "actionAdjust", disposal: "actionDisposal", restoredisposal: "actionRestoreDisposal", stocktakediff: "actionStocktakeDiff", stocktakemove: "actionStocktakeMove", prodreverse: "actionProdReverse", hold: "actionHold", holdrelease: "actionHoldRelease", picking: "actionPicking" };
const iconRoot = "/Images/Icon/";
const qty = (n: number) => new Intl.NumberFormat(undefined, { maximumFractionDigits: 4 }).format(n);
const date = (s: string | null) => s ? s.slice(0, 10).replaceAll("-", "/") : "—";
const timestamp = (s: string | null) => s ? s.slice(0, 16).replace("T", " ").replaceAll("-", "/") : "—";
const value = (s: string | null | undefined) => s || "—";

function buildTree(nodes: TraceNode[]): TreeEntry[] {
  const entries = nodes.map((node, index) => ({ index, node, children: [] as TreeEntry[] }));
  const roots: TreeEntry[] = [];
  const levels: TreeEntry[] = [];
  entries.forEach(entry => {
    const parent = entry.node.parentNo == null ? null : entries[entry.node.parentNo];
    const fallback = levels[entry.node.level - 1];
    const target = parent && parent.index < entry.index && parent.node.level < entry.node.level ? parent : fallback;
    if (target) target.children.push(entry); else roots.push(entry);
    levels[entry.node.level] = entry;
    levels.length = entry.node.level + 1;
  });
  return roots;
}

export function TraceabilityPage({ direction }: { direction: TraceDirection }) { const can = useScreenCan();
  const t = useT();
  const toast = useToast();
  const back = direction === "backward";
  const [draft, setDraft] = useState<SearchValues>(initial);
  const [dates, setDates] = useState<Record<DateKey, boolean>>({ productionDate: false, shipped: false, received: false });
  const [lots, setLots] = useState<TraceLot[] | null>(null);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [trace, setTrace] = useState<TraceResult | null>(null);
  const [chosenLot, setChosenLot] = useState<TraceLot | null>(null);
  const [selected, setSelected] = useState(0);
  const [closed, setClosed] = useState<Set<number>>(new Set());
  const [balance, setBalance] = useState<TraceBalance | null>(null);
  const [events, setEvents] = useState<TraceEvent[]>([]);
  const [busy, setBusy] = useState(false);

  const field = (key: string, label: string, fallback: string, width: number, first = false) => <><label htmlFor={`tr-${key}`} style={first ? undefined : { marginLeft: 14 }}>{t(label, fallback)}</label><input id={`tr-${key}`} type="text" value={draft[key]} onChange={e => setDraft({ ...draft, [key]: e.target.value })} style={{ width }} /></>;
  const range = (kind: DateKey, label: string, fallback: string) => <><input type="checkbox" checked={dates[kind]} onChange={e => setDates({ ...dates, [kind]: e.target.checked })} style={{ marginLeft: 14 }} /><label>{t(label, fallback)}</label><input type="date" value={draft[`${kind}From`]} disabled={!dates[kind]} onChange={e => setDraft({ ...draft, [`${kind}From`]: e.target.value })} style={{ width: 145 }} /><span className="search-sep">～</span><input type="date" value={draft[`${kind}To`]} disabled={!dates[kind]} onChange={e => setDraft({ ...draft, [`${kind}To`]: e.target.value })} style={{ width: 145 }} /></>;
  const clear = () => { setDraft(initial); setDates({ productionDate: false, shipped: false, received: false }); setLots(null); setTrace(null); setChosenLot(null); setEvents([]); setBalance(null); setPage(1); };
  const search = async (nextPage = 1) => {
    const active = ["orderNo", "productionLotNo", ...(back ? ["fgLotNo", "soNo", "customer"] : ["materialLotNo", "poNo"])].some(k => draft[k].trim()) || dates.productionDate || (back ? dates.shipped : dates.received);
    if (!active) { toast.err(t("prdNeedCond", "Enter at least one search condition.")); return; }
    for (const kind of ["productionDate", back ? "shipped" : "received"] as DateKey[]) {
      if (dates[kind] && draft[`${kind}From`] && draft[`${kind}To`] && draft[`${kind}From`] > draft[`${kind}To`]) { toast.err(t("alertDateRangeInverted", "The From date is after the To date. Please check the date range.")); return; }
    }
    const params: TraceSearch = { direction, page: nextPage, pageSize: 20 };
    for (const key of ["orderNo", "productionLotNo", ...(back ? ["fgLotNo", "soNo", "customer"] : ["materialLotNo", "poNo"])]) if (draft[key].trim()) params[key] = draft[key].trim();
    for (const kind of ["productionDate", back ? "shipped" : "received"] as DateKey[]) if (dates[kind]) { params[`${kind}From`] = draft[`${kind}From`]; params[`${kind}To`] = draft[`${kind}To`]; }
    setBusy(true); setTrace(null); setChosenLot(null);
    try { const result = await listTraceLots(params); setLots(result.data); setTotal(result.meta.total); setPage(nextPage); }
    catch (e) { toast.err(errorText(t, e)); }
    finally { setBusy(false); }
  };
  const traceLot = async (lot: TraceLot) => {
    if (!lot.lotNo) return;
    setBusy(true);
    try {
      const result = await getTrace(direction, lot.itemCode, lot.lotNo);
      setTrace(result); setChosenLot(lot); setSelected(0); setClosed(new Set()); setBalance(result.balance);
      const timeline = await getTraceEvents(lot.itemCode, lot.lotNo);
      setEvents(timeline.data);
    } catch (e) { toast.err(errorText(t, e)); }
    finally { setBusy(false); }
  };
  const chooseNode = async (index: number) => {
    if (!trace) return;
    setSelected(index);
    const node = trace.nodes[index];
    if (node.type !== "lot" && node.type !== "lot-made") return;
    if (!node.lotNo) return;
    try {
      const [nextBalance, nextEvents] = await Promise.all([getTraceBalance(node.itemCode, node.lotNo), getTraceEvents(node.itemCode, node.lotNo)]);
      setBalance(nextBalance); setEvents(nextEvents.data);
    } catch (e) { toast.err(errorText(t, e)); }
  };
  const download = async () => {
    if (!chosenLot?.lotNo) { toast.err(t("prdTraceFirst", "Search and trace a lot first.")); return; }
    try { const file = await exportTrace(direction, chosenLot.itemCode, chosenLot.lotNo); const url = URL.createObjectURL(file.blob); const anchor = document.createElement("a"); anchor.href = url; anchor.download = file.filename; anchor.click(); URL.revokeObjectURL(url); }
    catch (e) { toast.err(errorText(t, e)); }
  };
  const renderNode = (entry: TreeEntry): React.ReactNode => {
    const n = entry.node;
    const info = kindInfo[n.type] || { ...kindInfo.unknown, fallback: n.type };
    const isLot = n.type === "lot" || n.type === "lot-made";
    const title = nodeTitle(n);
    const expLabel = expText(n.expiryState ?? n.expiryCheck);
    return <li className="tv-node" key={entry.index}><div className={`tv-row ${info.cls}${selected === entry.index ? " sel" : ""}`} onClick={() => void chooseNode(entry.index)}>
      {entry.children.length ? <button type="button" className="tv-tgl" onClick={e => { e.stopPropagation(); setClosed(prev => { const next = new Set(prev); next.has(entry.index) ? next.delete(entry.index) : next.add(entry.index); return next; }); }}><img className="icn-img" src={iconRoot + (closed.has(entry.index) ? "chevron_right_50_000000.svg" : "expand_more_50_000000.svg")} alt="" /></button> : <span className="tv-tgl-sp" />}
      <span className="tv-chip"><img className="icn-img" src={iconRoot + info.icon} alt="" /></span><span className="tv-txt"><span className="tv-title">{title}</span>{isLot && <span className="tv-sub">{t("prdLotNo", "Lot No")}: {value(n.lotNo)}</span>}{isLot && (!!n.returnedQty || !!expLabel || !!n.suppliers) && <span className="tv-tags">{!!n.suppliers && <span className="tv-tag tv-tag-warn">{t("prdMultiSupplier", "Multiple suppliers")}</span>}{!!n.returnedQty && <span className="tv-tag tv-tag-ret">{t("prdRetQty", "Returned")} {qty(n.returnedQty)}</span>}{expLabel && <span className={`tv-tag ${(n.expiryState ?? n.expiryCheck) === "expired" ? "tv-tag-bad" : (n.expiryState ?? n.expiryCheck) === "near" ? "tv-tag-warn" : "tv-tag-ok"}`}>{expLabel}</span>}</span>}</span>
      {isLot && n.qty !== 0 && <span className="tv-qty">{qty(n.qty)}{n.unit ? ` ${n.unit}` : ""}</span>}
    </div>{entry.children.length > 0 && !closed.has(entry.index) && <ul className="tv-kids">{entry.children.map(renderNode)}</ul>}</li>;
  };
  const expText = (state?: string | null) => state === "expired" ? t("prdExpExpired", "Used after expiry") : state === "near" ? t("prdExpNear", "Near expiry") : state === "ok" ? "OK" : "";
  const nodeTitle = (n: TraceNode) => {
    const partner = n.partnerName || n.partnerCode;
    switch (n.type) {
      case "lot": case "lot-made": return `${n.itemCode} ${n.itemName || ""}`;
      case "process": return `${t("prdProcess", "Process")}: ${n.processName || n.processCode || "—"}${back ? "" : ` — ${value(n.documentNo)}  ${qty(n.qty)}${n.returnedQty ? `  [${t("prdRetQty", "Returned")} ${qty(n.returnedQty)}]` : ""}`}`;
      case "receipt": return `${t("prdReceiptType", "Receiving")}: ${value(n.documentNo)}${partner ? ` / ${partner}` : ""} (${date(n.occurredAt)})`;
      case "shipping": return `${t("prdSrcShipping", "Shipping")}: ${n.documentNo || t("prdDestUnknown", "Destination unknown")}${partner ? ` / ${partner}` : ""}  ${qty(n.qty)} (${date(n.occurredAt)})`;
      case "return": return `${t("prdTypeRet", "Return")}: ${qty(n.qty)}${n.unit ? ` ${n.unit}` : ""} (${timestamp(n.occurredAt ?? "")})`;
      case "unknown": return `${n.itemCode} ${n.itemName || ""} — ${t("prdUnknownLotHt", "Input lot unknown (no HT input)")}`;
      default: return `${n.type}: ${value(n.documentNo)}`;
    }
  };
  const node = trace?.nodes[selected];
  const info = node ? kindInfo[node.type] || kindInfo.unknown : null;
  const isLot = node?.type === "lot" || node?.type === "lot-made";
  const details: [string, string, string | number | null | undefined][] = node ? [
    ["thItemCode", "Item Code", node.itemCode], ["thItemName", "Item Name", node.itemName || (chosenLot?.itemCode === node.itemCode ? chosenLot.itemName : null)],
    ["prdLotNo", "Lot No", node.lotNo], ["prdWorker", "Worker", node.worker], ["prdSupplier", "Supplier", node.suppliers?.join(", ")], ["prdVoucher", "Voucher No", node.documentNo], ["prdProcess", "Process", node.processName], ["prdProdLot", "Production Lot", node.productionLotNo], ["prdExpiry", "Expiry", node.expiryDate], ["prdExpCheck", "Expiry check", expText(node.expiryState ?? node.expiryCheck) || null],
    [node.type === "shipping" ? "prdShipDate" : "prdRecvDate", node.type === "shipping" ? "Shipping Date" : "Receiving Date", node.occurredAt ? timestamp(node.occurredAt) : null],
    [node.type === "shipping" ? "prdPartner" : "prdSupplier", node.type === "shipping" ? "Business Partner" : "Supplier", node.partnerCode],
  ] : [];
  const balanceRows: [string, string, number, string][] = balance ? [
    ["prdBalRecv", "Receiving", balance.receivedQty, "+"], ["prdBalProd", "Production", balance.producedQty, "+"],
    ["prdBalCons", "Input", balance.consumedQty, "−"], ["prdBalMatRet", "Leftover return", balance.returnedInputQty, "+"],
    ["prdBalShip", "Shipping", balance.shippedQty, "−"], ["prdBalFgRet", "Customer return", balance.customerReturnQty, "+"],
    ["actionAdjust", "Adjust", balance.adjustmentQty, balance.adjustmentQty < 0 ? "" : "+"],
  ] : [];
  return <div className="pg on">
    <div className="search-box"><div className="search-row">
      {field("orderNo", "prdOrderNo", "Production Order No", 150, true)}{field("productionLotNo", "prdProdLot", "Production Lot", 130)}{range("productionDate", "prdProdDate", "Production Date")}
      {back ? <>{field("fgLotNo", "prdFgLot", "Finished Goods Lot", 140)}{field("soNo", "prdSoNo", "SO No", 150)}{field("customer", "prdCustomer", "Customer", 130)}{range("shipped", "prdShipDate", "Shipping Date")}</> : <>{field("materialLotNo", "prdMatLot", "Material Lot", 140)}{field("poNo", "prdPoNo", "PO No", 160)}{range("received", "prdRecvDate", "Receiving Date")}</>}
    </div><div className="search-btns"><button className="btn btn-primary" data-role="search" onClick={() => void search()} disabled={busy}><img className="icn-img" src={iconRoot + "search_50_FFFFFF.svg"} alt="" /> {t("search", "Search")}</button><button className="btn btn-cancel" onClick={clear}>{t("prdClear", "Clear")}</button>{can("Export") && <button className="btn btn-outline" onClick={() => void download()}><img className="icn-img" src={iconRoot + "database_upload_50_FFFFFF.svg"} alt="" /> {t("csvExport", "CSV Export")}</button>}</div></div>
    {lots && (lots.length ? <><div className="tbl-wrap"><table className="tbl"><thead><tr>{(back ? [["prdSoNo", "SO No"], ["prdPartner", "Business Partner"], ["prdShipDate", "Shipping Date"], ["thItemCode", "Item Code"], ["thItemName", "Item Name"], ["prdFgLot", "Finished Goods Lot"], ["prdShipNet", "Net shipped qty"], ["prdOrderNo", "Production Order No"], ["prdProdLot", "Production Lot"]] : [["thItemCode", "Item Code"], ["thItemName", "Item Name"], ["prdMatLot", "Material Lot"], ["prdPoNo", "PO No"], ["prdRecvDate", "Receiving Date"], ["prdOrderNo", "Production Order No"], ["prdProdLot", "Production Lot"], ["prdProdDate", "Production Date"]]).map(([key, fallback]) => <th key={key}>{t(key, fallback)}</th>)}<th /></tr></thead><tbody>{lots.map((lot, index) => <tr key={`${lot.itemCode}|${lot.lotNo}|${lot.poNo}|${lot.soNo}|${index}`}>{back ? <><td>{value(lot.soNo)}</td><td className="text-left">{value(lot.customer)}</td><td>{date(lot.shippedAt)}</td><td>{lot.itemCode}</td><td className="text-left">{value(lot.itemName)}</td><td>{value(lot.lotNo)}</td><td className="text-right">{qty(lot.qty)}</td><td>{value(lot.orderNo)}</td><td>{value(lot.productionLotNo)}</td></> : <><td>{lot.itemCode}</td><td className="text-left">{value(lot.itemName)}</td><td>{value(lot.lotNo)}</td><td>{value(lot.poNo)}</td><td>{date(lot.receivedAt)}</td><td>{value(lot.orderNo)}</td><td>{value(lot.productionLotNo)}</td><td>{date(lot.productionDate ?? null)}</td></>}<td><button className="btn btn-primary" disabled={!lot.lotNo || busy} onClick={() => void traceLot(lot)}>{t("prdTrace", "Trace")}</button></td></tr>)}</tbody></table></div>{total > 20 && <div className="trace-pages"><button className="btn btn-outline" disabled={page <= 1 || busy} onClick={() => void search(page - 1)}>‹</button><span>{page} / {Math.ceil(total / 20)}</span><button className="btn btn-outline" disabled={page * 20 >= total || busy} onClick={() => void search(page + 1)}>›</button></div>}</> : <div className="prd-empty">{t("noResults", "No matching records found.")}</div>)}
    {trace && <div className="tv-split"><section className="tv-panel"><div className="tv-bar"><span className="tv-bar-title">{t("prdTreePanel", "Components")}</span></div><div className="tv-body"><ul className="tv-root">{buildTree(trace.nodes).map(renderNode)}</ul></div></section><section className="tv-panel tv-detail-panel"><div className="tv-bar"><span className="tv-bar-title">{t("prdTreeDetail", "Details")}</span></div><div className="tv-body tv-detail">{node && info && <><div className={`tv-dhead ${info.cls}`}><span className="tv-chip tv-chip-l"><img className="icn-img" src={iconRoot + info.icon} alt="" /></span><div><div className="tv-dkind">{kindInfo[node.type] ? t(info.key, info.fallback) : node.type}</div><div className="tv-dtitle">{nodeTitle(node)}</div></div></div>{isLot && !!node.suppliers && <div className="tv-alert">{t("prdMultiSupplier", "Multiple suppliers")}: {node.suppliers.join(", ")}</div>}{(node.qty !== 0 || !!node.inputQty) && <div className="tv-tiles">{node.qty !== 0 && <div className="tv-tile"><span className="tv-tile-l">{node.type === "shipping" ? t("prdNetQty", "Net Qty") : t("thQty", "Qty")}</span><span className="tv-tile-v">{qty(node.qty)}{node.unit ? ` ${node.unit}` : ""}</span></div>}{!!node.inputQty && <div className="tv-tile"><span className="tv-tile-l">{t("prdInputQty", "Input Qty")}</span><span className="tv-tile-v">{qty(node.inputQty)}{node.unit ? ` ${node.unit}` : ""}</span></div>}{!!node.returnedQty && <div className="tv-tile"><span className="tv-tile-l">{t("prdRetQty", "Returned")}</span><span className="tv-tile-v">{qty(node.returnedQty)}{node.unit ? ` ${node.unit}` : ""}</span></div>}</div>}<dl className="tv-dl">{details.filter(([, , v]) => v !== null && v !== undefined && v !== "").map(([key, fallback, v]) => <div className="tv-dr" key={key}><dt>{t(key, fallback)}</dt><dd>{key.includes("Date") ? timestamp(String(v)) : v}</dd></div>)}</dl>{isLot && balance && <><h4 className="tv-h">{t("prdBalance", "Quantity balance")}</h4><table className="tv-bal"><tbody>{balanceRows.map(([key, fallback, amount, sign]) => <tr key={key}><td>{t(key, fallback)}</td><td className="text-right">{sign}{qty(amount)}</td></tr>)}<tr className="tv-bal-sum"><td>{t("prdBalExpected", "Expected stock")}</td><td className="text-right">{qty(balance.expectedQty)}</td></tr><tr className="tv-bal-sum"><td>{t("prdBalStock", "Stock")}</td><td className="text-right">{qty(balance.stockQty)} <span className={`tv-tag ${balance.ok ? "tv-tag-ok" : "tv-tag-bad"}`}>{balance.ok ? "OK" : `${t("prdBalNg", "Mismatch")} (${qty(balance.diffQty)})`}</span></td></tr></tbody></table></>}{isLot && events.length > 0 && <><h4 className="tv-h">{t("prdTimeline", "Timeline")}</h4><ol className="tv-tl">{events.map((event, index) => <li key={index}><span className="tv-tl-at">{timestamp(event.occurredAt)}</span><span className="tv-tl-ev">{t(eventKeys[event.type.toLowerCase()] || `prdEv${event.type[0]?.toUpperCase()}${event.type.slice(1)}`, event.type)} {event.documentNo && <span className="tv-tl-doc">{event.documentNo}</span>}</span><span className="tv-tl-q">{event.qty > 0 ? "+" : ""}{qty(event.qty)}</span></li>)}</ol></>}</>}</div></section></div>}
  </div>;
}
