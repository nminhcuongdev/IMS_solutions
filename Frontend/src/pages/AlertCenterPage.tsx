import { useNavigate } from "react-router-dom";
import { listAlerts, type AlertBlock } from "@/api/alerts";
import { useFetch } from "@/lib/useFetch";
import { useT } from "@/i18n/LanguageContext";


function targetOf(code: string): string {
  const yesterday = new Date(Date.now() - 86400000).toISOString().slice(0, 10);
  switch (code) {
    case "ExpiryWarning": return "/inventory/stock?exp=expiringSoon";
    case "LowStock": return "/inventory/stock?lvl=low";
    case "StockOverflow": return "/inventory/stock?lvl=overflow";
    case "HoldReview": return "/inventory/hold?hold=hold";
    case "ReceivingDelay": return `/receiving/schedule?to=${yesterday}`;
    case "PickingDelay": return `/picking/schedule?to=${yesterday}`;
    case "ShippingDelay": return `/shipping/schedule?to=${yesterday}`;
    default: return "/dashboard";
  }
}

function Block({ b }: { b: AlertBlock }) {
  const t = useT();
  const nav = useNavigate();
  const to = targetOf(b.alertTypeCode);

  return (
    <div className="flex min-h-[230px] flex-col overflow-hidden rounded-xl border-t-4 bg-white shadow-sm ring-1 ring-slate-200 border-amber-500">
      <div className="flex shrink-0 items-center justify-between border-b border-slate-100 px-3.5 py-3">
        <span className="text-[13.5px] font-bold text-slate-900">{t(b.labelKey, b.alertTypeCode)}</span>
        <span className="text-[19px] font-extrabold text-amber-600 tabular-nums">{b.total}</span>
      </div>
      <div className="max-h-[230px] flex-1 overflow-y-auto">
        {b.rows.length === 0 ? (
          <div className="px-3.5 py-5 text-center text-xs text-slate-400">{t("noResults", "No matching records found.")}</div>
        ) : (
          <table className="alert-mini-tbl">
            <thead>
              <tr>
                <th>{t("thItemCode", "Item Code")}</th>
                <th className="text-left">{t("thItemName", "Item Name")}</th>
                <th className="text-left">{t(b.extraLabelKey, "Ref")}</th>
              </tr>
            </thead>
            <tbody>
              {b.rows.map((r, i) => (
                <tr key={`${r.code}-${i}`} onClick={() => nav(to)}>
                  <td>{r.code}</td>
                  <td className="text-left">{r.name || "—"}</td>
                  <td className="text-left">{r.extra || "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

export function AlertCenterPage() {
  const t = useT();
  const blocks = useFetch("alerts", listAlerts);

  return (
    <div className="pg on" id="pg-alert_center">
      {blocks.error && (
        <div className="px-0.5 py-1.5 text-xs text-rose-600">{t(blocks.error, blocks.error)}</div>
      )}
      <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-4 px-0.5 py-1">
        {blocks.data
          ? blocks.data.map(b => <Block key={b.alertTypeCode} b={b} />)
          : Array.from({ length: 7 }, (_, i) => (
              <div className="flex min-h-[230px] flex-col overflow-hidden rounded-xl border-t-4 bg-white shadow-sm ring-1 ring-slate-200 border-slate-200" key={i}>
                <div className="flex shrink-0 items-center justify-between border-b border-slate-100 px-3.5 py-3">
                  <span className="sk w-[120px]" />
                  <span className="sk w-6" />
                </div>
                <div className="flex-1" />
              </div>
            ))}
      </div>
    </div>
  );
}
