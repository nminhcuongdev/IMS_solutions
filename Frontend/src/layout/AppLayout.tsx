import { useEffect, useState } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { fetchMenu, type MenuResponse } from "@/api/menu";
import { useAuth } from "@/auth/AuthContext";
import { useLang } from "@/i18n/LanguageContext";
import { LanguageSelect } from "@/i18n/LanguageSelect";
import { FUNCTION_LABEL_KEY } from "@/i18n/labelKeys";
import { ROUTE_MAP, Sidebar } from "./Sidebar";
import { BRAND } from "@/brand";
import { errorText } from "@/lib/errorText";
import { useSearchConditionToggle } from "@/lib/useSearchConditionToggle";

const CODE_BY_PATH: Record<string, string> = Object.fromEntries(
  Object.entries(ROUTE_MAP).map(([code, path]) => [path, code]),
);

function findCurrent(pathname: string, menu: MenuResponse | null): { code: string; name: string } {
  const code = CODE_BY_PATH[pathname];
  if (!code) return { code: "", name: "" };
  for (const g of menu?.groups ?? []) {
    const f = g.functions.find(x => x.functionCode === code);
    if (f) return { code, name: f.functionName };
  }
  return { code, name: "" };
}

export function AppLayout() {
  const { user, logout } = useAuth();
  const nav = useNavigate();
  const loc = useLocation();
  useSearchConditionToggle(loc.pathname);
  const [menu, setMenu] = useState<MenuResponse | null>(null);
  const [menuError, setMenuError] = useState<string | null>(null);
  const { t } = useLang();

  useEffect(() => {
    fetchMenu().then(setMenu).catch(e => setMenuError(errorText(t, e)));
  }, []);

  const cur = findCurrent(loc.pathname, menu);
  const current = cur.code
    ? t(FUNCTION_LABEL_KEY[cur.code] ?? cur.code, cur.name || cur.code)
    : t("dashboard", "Dashboard");
  const avatarLetter = (user?.userName ?? user?.userCode ?? "?").charAt(0).toUpperCase();

  const doLogout = async () => { await logout(); nav("/login"); };

  return (
    <div className="flex h-screen w-full">
      <Sidebar menu={menu} onLogout={doLogout} />
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 shrink-0 items-center justify-between border-b border-slate-200 bg-white px-5.5 max-md:px-3">
          <div className="flex items-center gap-1.5 text-[13px] text-slate-400">
            <span className="font-semibold tracking-wider text-slate-500">{BRAND.name}</span>
            <span className="text-slate-300">/</span>
            <span className="font-semibold text-brand-700">{current}</span>
          </div>
          <div className="flex items-center gap-3.5 text-[13px]">
            <div className="flex items-center border-r border-slate-200 pr-3.5">
              <LanguageSelect />
            </div>
            <span className="text-slate-500">{t("welcome", "Welcome,")}</span>
            <strong className="text-slate-800">{user?.userName}</strong>
            <div className="flex size-8 items-center justify-center rounded-full bg-brand-600 text-[13px] font-bold text-white ring-2 ring-brand-100">
              {avatarLetter}
            </div>
          </div>
        </header>
        <main className="content flex-1 overflow-auto px-6 py-5 max-md:p-3">
          {menuError && <div className="page-info">{t("errMenuLoad", "Menu load failed")}: {menuError}</div>}
          <Outlet />
        </main>
        <footer className="flex h-[30px] shrink-0 items-center justify-center border-t border-slate-200 bg-white text-[11px] text-slate-400">
          © {new Date().getFullYear()} {BRAND.fullName}
        </footer>
      </div>
    </div>
  );
}
