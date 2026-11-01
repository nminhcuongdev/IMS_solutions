import type { MouseEvent } from "react";
import { useNavigate } from "react-router-dom";
import { useT } from "@/i18n/LanguageContext";

type Link = { key: string; fallback: string; to?: string };
type Card = { icon: string; key: string; fallback: string; to: string; links: Link[] };

const CARDS: Card[] = [
  {
    icon: "shelves", key: "dCardInventory", fallback: "Inventory", to: "/inventory/stock",
    links: [
      { key: "invInquiry", fallback: "Stock Inquiry", to: "/inventory/stock" },
      { key: "invHistory", fallback: "Work History", to: "/inventory/history" },
      { key: "invHold", fallback: "Hold Item", to: "/inventory/hold" },
    ],
  },
  {
    icon: "warning", key: "alertCenterTitle", fallback: "Alert Center", to: "/alerts",
    links: [{ key: "alertCenterSub", fallback: "Click an alert to view details" }],
  },
  {
    icon: "forklift", key: "dCardRecv", fallback: "Receiving", to: "/receiving/result",
    links: [
      { key: "rcvImport", fallback: "Import Receiving Schedule", to: "/receiving/schedule" },
      { key: "rcvResult", fallback: "Receiving/Returns Result", to: "/receiving/result" },
      { key: "returnsMgmt", fallback: "Returns Schedule", to: "/receiving/returns" },
    ],
  },
  {
    icon: "delivery_truck_speed", key: "dCardShip", fallback: "Shipping", to: "/shipping/result",
    links: [
      { key: "pickImport", fallback: "Import Picking Schedule", to: "/picking/schedule" },
      { key: "pickResult", fallback: "Picking Result", to: "/picking/result" },
      { key: "shpImport", fallback: "Import Shipping Schedule", to: "/shipping/schedule" },
      { key: "shpResult", fallback: "Shipping Result", to: "/shipping/result" },
    ],
  },
  {
    icon: "Stocktakingbucket_check", key: "dCardStocktake", fallback: "Stocktaking", to: "/stocktaking/labels",
    links: [
      { key: "stLabel", fallback: "Item Stocktaking", to: "/stocktaking/labels" },
      { key: "stRef", fallback: "Stocktaking History", to: "/stocktaking/history" },
    ],
  },
];

export function DashboardPage() {
  const t = useT();
  const nav = useNavigate();

  function open(e: MouseEvent, to: string | undefined) {
    if (!to) return;
    e.stopPropagation();
    nav(to);
  }

  return (
    <div className="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-4 max-md:grid-cols-1">
      {CARDS.map(c => (
        <div
          className="cursor-pointer overflow-hidden rounded-xl bg-white shadow-sm ring-1 ring-slate-200 transition hover:-translate-y-0.5 hover:shadow-md"
          key={c.key} onClick={() => nav(c.to)}>
          <div className="flex items-center gap-2 bg-brand-600 px-4 py-3 text-sm font-semibold text-white">
            <img className="icn-img text-base" src={`/Images/Icon/${c.icon}_50_FFFFFF.svg`} alt="" /> <span>{t(c.key, c.fallback)}</span>
          </div>
          <div className="px-4 py-3.5">
            <ul>
              {c.links.map(l => (
                <li key={l.key} onClick={e => open(e, l.to)}
                  className="border-b border-slate-100 py-1.5 text-[13px] last:border-0 hover:text-brand-700">{t(l.key, l.fallback)}</li>
              ))}
            </ul>
          </div>
        </div>
      ))}
    </div>
  );
}
