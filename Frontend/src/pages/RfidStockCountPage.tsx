import { useScreenCan } from "@/auth/PermissionContext";
import { useEffect, useMemo, useState } from "react";
import { getStockScanParts, getStockScanSummary, listStockScan, resetStockScan, type ScannedFilter, type StockScanRow } from "@/api/stockScan";
import { SearchButton } from "@/lib/Buttons";
import { useConfirm } from "@/lib/Confirm";
import { useToast } from "@/lib/Toast";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useUrlString } from "@/lib/useUrlState";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";
import { RfidPartSelectModal } from "./RfidPartSelectModal";

const POLL_MS = 3000;

const SCANNED_VALUES: readonly ScannedFilter[] = ["all", "yes", "no"];

const asScanned = (v: string): ScannedFilter =>
  (SCANNED_VALUES as readonly string[]).includes(v) ? (v as ScannedFilter) : "all";

function ResetIcon() {
  return (
    <svg className="ic" viewBox="0 0 24 24">
      <path d="M17.65 6.35A8 8 0 1 0 19.4 15h-2.12A6 6 0 1 1 12 6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z" />
    </svg>
  );
}

type TileState = "" | " short" | " scanned";

const itemKey = (code: string | null, name: string | null) => `${code ?? ""}|${name ?? ""}`;

function Tile({ row, state }: { row: StockScanRow; state: TileState }) {
  return (
    <div className={"rsd-tile" + state}
      title={row.workerName ? `${row.workerCode ?? ""} ${row.workerName}`.trim() : undefined}>
      <div className="rsd-item">{row.itemName || row.itemCode || "—"}</div>
      <div className="rsd-epc">{row.epc}</div>
    </div>
  );
}

export function RfidStockCountPage() { const can = useScreenCan();
  const t = useT();
  const toast = useToast();
  const ask = useConfirm();

  const [itemCode, setItemCode] = useUrlString("itemCode");
  const [itemName, setItemName] = useUrlString("itemName");
  const [scannedRaw, setScanned] = useUrlString("scanned", "all");
  const scanned = asScanned(scannedRaw);

  const [codeDraft, setCodeDraft] = useState(itemCode);
  const [nameDraft, setNameDraft] = useState(itemName);
  const [scannedDraft, setScannedDraft] = useState<ScannedFilter>(scanned);
  const [busy, setBusy] = useState(false);
  const [selecting, setSelecting] = useState(false);

  const { data, loading, error, reload, refresh } = useFetch(
    qs("stockScan", { itemCode, itemName, scanned }),
    () => listStockScan({ itemCode: itemCode || undefined, itemName: itemName || undefined, scanned }));

  const summary = useFetch("stockScanSummary", getStockScanSummary);
  const parts = useFetch("stockScanParts", getStockScanParts);
  const reloadSummary = summary.reload;

  useEffect(() => {
    const id = window.setInterval(() => {
      if (document.visibilityState !== "visible") return;
      void reload();
      void reloadSummary();
    }, POLL_MS);
    return () => window.clearInterval(id);
  }, [reload, reloadSummary]);

  const rows = data?.rows ?? [];

  const complete = useMemo(() => {
    const done = new Set<string>();
    for (const it of summary.data?.items ?? []) {
      if (it.plan > 0 && it.qty >= it.plan) done.add(itemKey(it.itemCode, it.itemName));
    }
    return done;
  }, [summary.data]);

  const stateOf = (r: StockScanRow): TileState => {
    if (!r.isScanned) return "";
    return complete.has(itemKey(r.itemCode, r.itemName)) ? " scanned" : " short";
  };

  const search = () => {
    setItemCode(codeDraft.trim());
    setItemName(nameDraft.trim());
    setScanned(scannedDraft);
    refresh();
    void reloadSummary();
    void parts.reload();
  };

  const onEnter = (e: React.KeyboardEvent) => { if (e.key === "Enter") search(); };

  const onReset = async () => {
    const ok = await ask({
      message: t("rsdResetConfirm", "Are you sure you want to reset? All scanned flags will be cleared."),
      confirmLabel: t("modalOk", "OK"),
    });
    if (!ok) return;
    setBusy(true);
    try {
      await resetStockScan();
      invalidate("stockScan");
      await Promise.all([reload(), reloadSummary(), parts.reload()]);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
    finally { setBusy(false); }
  };

  return (
    <div className="pg on" id="pg-st_rfid_demo">
      <div className="search-box">
        <div className="search-row">
          <label>{t("thItemCode", "Item Code")}</label>
          <input type="text" style={{ width: 140 }} value={codeDraft}
            onChange={e => setCodeDraft(e.target.value)} onKeyDown={onEnter} />
          <label style={{ marginLeft: 14 }}>{t("thItemName", "Item Name")}</label>
          <input type="text" style={{ width: 160 }} value={nameDraft}
            onChange={e => setNameDraft(e.target.value)} onKeyDown={onEnter} />
          <label style={{ marginLeft: 14 }}>{t("rsdScannedFilter", "Scanned")}</label>
          <select style={{ width: 110 }} value={scannedDraft}
            onChange={e => setScannedDraft(asScanned(e.target.value))}>
            <option value="all">{t("warehouseAll", "All")}</option>
            <option value="yes">{t("rsdYes", "Yes")}</option>
            <option value="no">{t("rsdNo", "No")}</option>
          </select>
        </div>

        {error && <div className="search-row" style={{ color: "#DC3545", fontSize: 12 }}>{error}</div>}

        <div className="search-btns">
          <SearchButton onClick={search} />
          {can("Reset") && <button className="btn btn-delete" onClick={onReset} disabled={busy || loading}>
            <ResetIcon /> <span>{t("rsdReset", "Reset")}</span>
          </button>}
          {can("SelectTarget") && <button className="btn btn-primary" onClick={() => setSelecting(true)}>{t("rsdSelectParts", "Select Parts")}</button>}
        </div>
      </div>

      {selecting && <RfidPartSelectModal canDelete={can("Delete")} onClose={() => setSelecting(false)} onChanged={async () => { invalidate("stockScan"); await Promise.all([reload(), reloadSummary(), parts.reload()]); }} onSaved={async () => { invalidate("stockScan"); await Promise.all([reload(), reloadSummary(), parts.reload()]); toast.ok(t("toastSaved", "Saved")); setSelecting(false); }} />}
      <div className="flex items-center justify-between border-b border-slate-200 bg-white px-4 py-2.5">
        <span>{parts.data && t("rsdTargetParts", "Counting: {0} / {1} parts").replace("{0}", String(parts.data.filter(part => part.isTarget).length)).replace("{1}", String(parts.data.length))}</span>
        <span className="rsd-count">{data ? `${data.scanned} / ${data.total}` : "0 / 0"}</span>
      </div>

      <div className="rsd-grid">
        {loading && <div className="rsd-empty">…</div>}
        {!loading && rows.length === 0 && (
          <div className="rsd-empty">{t("noResults", "No matching records found.")}</div>
        )}
        {!loading && rows.map(r => <Tile key={r.epc} row={r} state={stateOf(r)} />)}
      </div>
    </div>
  );
}
