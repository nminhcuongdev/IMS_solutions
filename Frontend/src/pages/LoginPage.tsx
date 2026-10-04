import { useEffect, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "@/auth/AuthContext";
import { BTN_ICON } from "@/layout/icons";
import { useLang } from "@/i18n/LanguageContext";
import { BrandLogo } from "@/layout/BrandLogo";
import { BRAND } from "@/brand";

const LABEL = "mb-1 block text-xs font-semibold text-slate-600";
const INPUT = "h-10 w-full rounded-lg border border-slate-300 px-2.5 text-[13px] focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 focus:outline-none";

const REMEMBER_KEY = "ims_remember_userid";

function rememberedUserId(): string {
  try {
    return localStorage.getItem(REMEMBER_KEY) ?? "";
  } catch {
    return "";
  }
}

function storeRememberedUserId(value: string | null) {
  try {
    if (value) localStorage.setItem(REMEMBER_KEY, value);
    else localStorage.removeItem(REMEMBER_KEY);
  } catch {
    return;
  }
}

export function LoginPage() {
  const { t, lang, setLang, slots } = useLang();
  const { login, loading, user } = useAuth();
  const [initialUserId] = useState(rememberedUserId);
  const [userId, setUserId] = useState(initialUserId);
  const [password, setPassword] = useState("");
  const [remember, setRemember] = useState(initialUserId !== "");
  const [err, setErr] = useState<string | null>(null);
  const nav = useNavigate();
  const from = (useLocation().state as { from?: string } | null)?.from;

  useEffect(() => {
    if (user) nav(from ?? "/dashboard", { replace: true });
  }, [user, from, nav]);

  async function doLogin(e: FormEvent) {
    e.preventDefault();
    setErr(null);
    try {
      await login(userId, password);
      storeRememberedUserId(remember ? userId : null);
      nav(from ?? "/dashboard", { replace: true });
    } catch (ex: any) {
      setErr(ex?.message ?? "Login failed");
    }
  }

  return (
    <div className="flex min-h-screen w-full items-center justify-center bg-gradient-to-br from-brand-50 via-slate-100 to-white px-4">
      <div className="w-full max-w-[400px] overflow-hidden rounded-2xl bg-white shadow-xl ring-1 ring-slate-900/5">
        <div className="flex flex-col items-center border-b border-slate-100 px-5 pt-8 pb-5 text-center">
          <BrandLogo size="lg" compact />
          <div className="mt-3 text-2xl font-extrabold tracking-[0.12em] text-slate-900">{BRAND.name}</div>
          <div className="mt-1 text-[13px] text-slate-500">{t("loginSubtitle", "Inventory Management System")}</div>
        </div>
        <form className="flex flex-col gap-4 px-7 pt-5 pb-7" onSubmit={doLogin}>
          {err && (
            <div className="rounded-lg bg-rose-50 px-2.5 py-2 text-xs text-rose-700 ring-1 ring-rose-200">
              {err}
            </div>
          )}
          <div>
            <label htmlFor="userid" className={LABEL}>{t("userId", "User ID")}</label>
            <input id="userid" type="text" className={INPUT} value={userId} onChange={e => setUserId(e.target.value)} autoFocus={initialUserId === ""} />
          </div>
          <div>
            <label htmlFor="loginPassword" className={LABEL}>{t("password", "Password")}</label>
            <input id="loginPassword" type="password" className={INPUT} value={password} onChange={e => setPassword(e.target.value)} autoFocus={initialUserId !== ""} />
          </div>
          <div className="flex items-center justify-between">
            <label htmlFor="rememberMe" className="text-[13px] font-semibold text-slate-600">{t("rememberMe", "Remember me")}</label>
            <label className="relative inline-flex h-[23px] w-[42px] shrink-0 cursor-pointer">
              <input id="rememberMe" type="checkbox" className="peer sr-only" checked={remember} onChange={e => setRemember(e.target.checked)} />
              <span className="absolute inset-0 rounded-full bg-slate-300 transition-colors duration-200 peer-checked:bg-brand-600 peer-focus-visible:ring-2 peer-focus-visible:ring-brand-500/40" />
              <span className="absolute top-[3px] left-[3px] size-[17px] rounded-full bg-white shadow transition-transform duration-200 peer-checked:translate-x-[19px]" />
            </label>
          </div>
          <button
            className="mt-1 flex h-[42px] w-full items-center justify-center gap-2 rounded-lg bg-brand-600 text-sm font-bold text-white shadow-sm transition-colors hover:bg-brand-700 disabled:opacity-60"
            type="submit"
            disabled={loading}>
            <img className="icn-img" src={BTN_ICON.login} alt="" />
            <span>{loading ? t("signingIn", "Signing in…") : t("login", "LOGIN")}</span>
          </button>
          <div className="flex items-center justify-center gap-1">
            {slots.map(sl => (
              <img
                key={sl.languageCode}
                className={"lang-flag" + (lang === sl.languageCode ? " active" : "")}
                src={`/Images/NationalFlag/${sl.flagIcon ?? sl.languageCode + ".png"}`}
                onClick={() => setLang(sl.languageCode)}
                alt={sl.languageCode}
                title={sl.languageName} />
            ))}
          </div>
        </form>
      </div>
    </div>
  );
}
