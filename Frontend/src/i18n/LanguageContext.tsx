import {
  createContext, useCallback, useContext, useEffect, useMemo, useState,
  type ReactNode,
} from "react";
import { getLanguages, getLabels, type LanguageOption } from "@/api/language";

export type LangCode = string;

interface LanguageCtx {
  lang: LangCode;
  setLang: (l: LangCode) => void;
  languages: LanguageOption[];
  t: (key: string, fallback?: string) => string;
  loading: boolean;
}

const Ctx = createContext<LanguageCtx | null>(null);

const STORAGE_KEY = "ims_lang";

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<LangCode>(
    () => localStorage.getItem(STORAGE_KEY) || "en");
  const [languages, setLanguages] = useState<LanguageOption[]>([]);
  const [labels, setLabels] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);

  const [cache] = useState<Map<string, Record<string, string>>>(() => new Map());

  useEffect(() => {
    getLanguages().then(setLanguages).catch(() => setLanguages([]));
  }, []);

  useEffect(() => {
    let cancelled = false;
    const cached = cache.get(lang);
    if (cached) { setLabels(cached); setLoading(false); return; }

    setLoading(true);
    getLabels(lang)
      .then(map => {
        if (cancelled) return;
        cache.set(lang, map);
        setLabels(map);
      })
      .catch(() => { if (!cancelled) setLabels({}); })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [lang, cache]);

  const setLang = useCallback((l: LangCode) => {
    localStorage.setItem(STORAGE_KEY, l);
    setLangState(l);
    document.documentElement.lang = l;
  }, []);

  useEffect(() => { document.documentElement.lang = lang; }, [lang]);

  const t = useCallback(
    (key: string, fallback?: string) => labels[key] ?? fallback ?? key,
    [labels]);

  const value = useMemo<LanguageCtx>(
    () => ({ lang, setLang, languages, t, loading }),
    [lang, setLang, languages, t, loading]);

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useLang(): LanguageCtx {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useLang phải nằm trong <LanguageProvider>");
  return ctx;
}

export function useT() {
  return useLang().t;
}
