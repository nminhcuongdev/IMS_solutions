import { useCallback, useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { useDebounced } from "./useDebounced";


type Setter<T> = (v: T, opts?: { push?: boolean }) => void;

export function useUrlPatch() {
  const [, setSp] = useSearchParams();
  return useCallback(
    (patch: Record<string, string | null>, opts?: { push?: boolean }) => {
      setSp(prev => {
        const next = new URLSearchParams(prev);
        for (const [k, v] of Object.entries(patch)) {
          if (v === null || v === "") next.delete(k);
          else next.set(k, v);
        }
        return next;
      }, { replace: !opts?.push });
    }, [setSp]);
}

function useParam<T>(
  key: string,
  def: T,
  parse: (raw: string) => T,
  format: (v: T) => string,
): [T, Setter<T>] {
  const [sp] = useSearchParams();
  const patch = useUrlPatch();
  const raw = sp.get(key);
  const value = raw === null ? def : parse(raw);

  const defStr = format(def);
  const set = useCallback<Setter<T>>((v, opts) => {
    const s = format(v);
    patch({ [key]: s === defStr ? null : s }, opts);
  }, [key, patch, defStr]);

  return [value, set];
}

export function useUrlString(key: string, def = ""): [string, Setter<string>] {
  return useParam(key, def, r => r, v => v);
}

export function useUrlNumber(
  key: string,
  def: number,
  allowed?: readonly number[],
): [number, Setter<number>] {
  return useParam(key, def, r => {
    const n = Number(r);
    if (!Number.isFinite(n)) return def;
    if (allowed && !allowed.includes(n)) return def;
    return n;
  }, v => String(v));
}

export function useUrlBool(key: string, def: boolean): [boolean, Setter<boolean>] {
  return useParam(key, def, r => r === "1", v => (v ? "1" : "0"));
}

export function useUrlSearch(key = "q", ms = 300): {
  q: string; draft: string; setDraft: (v: string) => void;
} {
  const [sp] = useSearchParams();
  const patch = useUrlPatch();
  const q = sp.get(key) ?? "";

  const [draft, setDraft] = useState(q);
  const settled = useDebounced(draft, ms);
  const first = useRef(true);

  useEffect(() => {
    if (first.current) { first.current = false; return; }
    if (settled === q) return;
    patch({ [key]: settled || null, page: null });
  }, [settled]);

  return { q, draft, setDraft };
}
