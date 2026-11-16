import { useEffect, useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import type { MenuResponse, MenuGroup, MenuFunction } from "@/api/menu";
import { FUNCTION_ICON, GROUP_ICON, BTN_ICON, FALLBACK_ICON } from "./icons";
import { useT } from "@/i18n/LanguageContext";
import { BrandLogo } from "./BrandLogo";
import { FUNCTION_LABEL_KEY, groupLabelKey, NAV_LABEL_KEY } from "@/i18n/labelKeys";

export const ROUTE_MAP: Record<string, string> = {
  dashboard: "/dashboard",
  m_factory: "/masters/factories",
  m_device: "/masters/devices",
  dist_status: "/masters/distribution",
  m_zone: "/masters/zones",
  m_location: "/masters/locations",
  m_location_3d: "/masters/locations-3d",
  m_itemgroup: "/masters/item-groups",
  m_item: "/masters/items",
  m_process: "/masters/processes",
  trace_back: "/traceability/backward",
  trace_fwd: "/traceability/forward",
  m_partner: "/masters/partners",
  m_user: "/masters/users",
  m_reason: "/masters/reasons",
  inv: "/inventory/stock",
  inv_history: "/inventory/history",
  inv_hold: "/inventory/hold",
  rcv_import: "/receiving/schedule",
  rcv_result: "/receiving/result",
  returns: "/receiving/returns",
  pick_import: "/picking/schedule",
  pick_result: "/picking/result",
  shp_import: "/shipping/schedule",
  shp_result: "/shipping/result",
  prd_import: "/production/orders",
  prd_result: "/production/results",
  alert_center: "/alerts",
  st_label: "/stocktaking/labels",
  st_ref: "/stocktaking/history",
  st_rfid_demo: "/stocktaking/rfid",
  m_permission: "/other/permissions",
  settings: "/other/settings",
  lang_data: "/other/language-data",
  print_history: "/history/print",
  rfid_gate_history: "/history/rfid",
  super_admin: "/super-admin",
};

const DIVIDER_BEFORE = new Set(["printHistory", "masterMgmt"]);

const PartnerIcon = ({ className }: { className: string }) => (
  <svg className={className} viewBox="0 0 24 24">
    <circle cx="9" cy="8" r="4" />
    <circle cx="16.5" cy="9" r="3.3" fillOpacity="0.55" />
    <path d="M2 21c0-3.3 3.1-6 7-6s7 2.7 7 6v0.3H2V21z" />
    <path d="M14 21.3V21c0-1.6-.7-3.1-1.9-4.2 1-.6 2.3-.9 3.6-.9 3.4 0 6.3 2.2 6.3 5v0.4H14z" fillOpacity="0.55" />
  </svg>
);

const CaretIcon = () => (
  <svg className="size-3 fill-current" viewBox="0 0 24 24">
    <path d="M12 15.4a1 1 0 0 1-.71-.3l-5-5a1 1 0 1 1 1.42-1.4L12 12.58l4.3-4.3a1 1 0 1 1 1.4 1.42l-5 5a1 1 0 0 1-.7.3z" />
  </svg>
);

const CollapseIcon = () => (
  <svg className="size-3.5 fill-current" viewBox="0 0 24 24">
    <path d="M14.7 18.3a1 1 0 0 1-1.4 0l-6-6a1 1 0 0 1 0-1.4l6-6a1 1 0 1 1 1.4 1.4L9.4 12l5.3 5.3a1 1 0 0 1 0 1.4z" />
  </svg>
);

const ICON_CLS = "size-4 shrink-0 object-contain";

function topLinkCls(collapsed: boolean, active: boolean): string {
  return [
    "flex items-center gap-2.5 text-[13px] whitespace-nowrap transition-colors",
    collapsed ? "justify-center py-3" : "mx-2 rounded-lg px-3 py-2.5",
    active ? "bg-brand-500 font-semibold text-white shadow-sm" : "text-slate-300 hover:bg-ink-700 hover:text-white",
  ].join(" ");
}

function subLinkCls(active: boolean): string {
  return [
    "mx-2 flex items-center gap-2 rounded-lg py-2 pr-3 pl-10 text-xs whitespace-nowrap transition-colors",
    active
      ? "bg-white/10 font-semibold text-white shadow-[inset_3px_0_0_var(--color-brand-400)]"
      : "text-slate-400 hover:bg-ink-700 hover:text-white",
  ].join(" ");
}

function Divider({ collapsed }: { collapsed: boolean }) {
  return <li aria-hidden="true" className={`my-1.5 h-px bg-white/10 ${collapsed ? "mx-1.5" : "mx-3.5"}`} />;
}

export function Sidebar({ menu, onLogout }: { menu: MenuResponse | null; onLogout: () => void }) {
  const [collapsed, setCollapsed] = useState(false);
  const [openGroup, setOpenGroup] = useState<string | null>(null);
  const loc = useLocation();
  const t = useT();

  useEffect(() => {
    if (!menu) return;
    const current = menu.groups.find(g => g.functions.some(f => ROUTE_MAP[f.functionCode] === loc.pathname));
    if (current) setOpenGroup(current.largeCode);
  }, [menu, loc.pathname]);

  const toggleGroup = (code: string) => setOpenGroup(prev => (prev === code ? null : code));

  return (
    <aside
      id="sidebar"
      className={`relative z-10 flex flex-col overflow-hidden bg-ink-900 text-slate-200 shadow-xl transition-[width,min-width] duration-300 max-md:hidden ${
        collapsed ? "w-14 min-w-14" : "w-64 min-w-64"}`}>
      <button
        type="button"
        aria-label="Toggle sidebar"
        className={`absolute top-4 right-2.5 z-20 flex size-7 items-center justify-center rounded-full border border-white/15 bg-ink-800 text-slate-300 shadow transition-transform duration-300 hover:bg-ink-700 hover:text-white ${
          collapsed ? "rotate-180" : ""}`}
        onClick={() => setCollapsed(v => !v)}>
        <CollapseIcon />
      </button>

      <div className={`flex h-14 shrink-0 items-center overflow-hidden border-b border-white/10 whitespace-nowrap ${
        collapsed ? "justify-center" : "px-4"}`}>
        {collapsed ? null : <BrandLogo tone="light" />}
      </div>

      <nav className="flex-1 overflow-x-hidden overflow-y-auto py-2">
        <ul className="flex flex-col gap-0.5">
          {menu?.groups.map((g, idx) => (
            <GroupRender
              key={g.largeCode}
              group={g}
              collapsed={collapsed}
              open={openGroup === g.largeCode}
              onToggle={() => toggleGroup(g.largeCode)}
              showDividerBefore={idx > 0 && (
                menu.groups[idx - 1].largeCode === "menu" || DIVIDER_BEFORE.has(g.largeCode))} />
          ))}

          <Divider collapsed={collapsed} />
          <li>
            <a onClick={onLogout} className={`${topLinkCls(collapsed, false)} cursor-pointer`}>
              <img className={ICON_CLS} src={BTN_ICON.logout} alt="" />
              {!collapsed && <span>{t("logout", "Logout")}</span>}
            </a>
          </li>
        </ul>
      </nav>
    </aside>
  );
}

function GroupRender({
  group, collapsed, open, onToggle, showDividerBefore,
}: {
  group: MenuGroup; collapsed: boolean; open: boolean; onToggle: () => void; showDividerBefore: boolean;
}) {
  const t = useT();
  const icon = GROUP_ICON[group.largeCode] ?? FALLBACK_ICON;
  const isFlat = group.functions.length === 1;
  if (isFlat) {
    const f = group.functions[0];
    return (
      <>
        {showDividerBefore && <Divider collapsed={collapsed} />}
        <li>
          <FunctionLink f={f} collapsed={collapsed} sub={false} />
        </li>
      </>
    );
  }

  return (
    <>
      {showDividerBefore && <Divider collapsed={collapsed} />}
      <li>
        <a onClick={onToggle} className={`${topLinkCls(collapsed, false)} cursor-pointer`}>
          {icon && <img className={ICON_CLS} src={icon} alt="" />}
          {!collapsed && (
            <>
              <span>{t(groupLabelKey(group.largeCode), group.largeName)}</span>
              <span className={`ml-auto transition-transform duration-200 ${open ? "rotate-180" : ""}`}>
                <CaretIcon />
              </span>
            </>
          )}
        </a>
        {!collapsed && (
          <ul className={`overflow-hidden transition-[max-height] duration-300 ${open ? "max-h-[800px] py-0.5" : "max-h-0"}`}>
            {group.functions.map(f => (
              <li key={f.functionCode}>
                <FunctionLink f={f} collapsed={false} sub />
              </li>
            ))}
          </ul>
        )}
      </li>
    </>
  );
}

function FunctionLink({ f, collapsed, sub }: { f: MenuFunction; collapsed: boolean; sub: boolean }) {
  const t = useT();
  const to = ROUTE_MAP[f.functionCode] ?? `/wip/${f.functionCode}`;
  const icon = FUNCTION_ICON[f.functionCode] ?? FALLBACK_ICON;
  const label = f.functionCode === "dashboard"
    ? "Menu"
    : t(NAV_LABEL_KEY[f.functionCode] ?? FUNCTION_LABEL_KEY[f.functionCode] ?? f.functionCode, f.functionName);
  return (
    <NavLink
      to={to}
      title={collapsed ? label : undefined}
      className={({ isActive }) => (sub ? subLinkCls(isActive) : topLinkCls(collapsed, isActive))}>
      {f.functionCode === "m_partner"
        ? <PartnerIcon className="size-4 shrink-0 fill-current" />
        : icon && <img className={ICON_CLS} src={icon} alt="" />}
      {!collapsed && <span>{label}</span>}
    </NavLink>
  );
}
