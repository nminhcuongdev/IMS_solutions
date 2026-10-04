import { useCallback, useEffect, useRef, useState } from "react";


interface Entry { value: unknown; at: number }

const MAX_ENTRIES = 60;
const cache = new Map<string, Entry>();

function cacheGet(key: string): Entry | undefined {
  const hit = cache.get(key);
  if (!hit) return undefined;
  cache.delete(key);
  cache.set(key, hit);
  return hit;
}

function cacheSet(key: string, value: unknown) {
  cache.delete(key);
  cache.set(key, { value, at: Date.now() });
  while (cache.size > MAX_ENTRIES) {
    const oldest = cache.keys().next().value;
    if (oldest === undefined) break;
    cache.delete(oldest);
  }
}

export function invalidate(prefix: string) {
  for (const k of [...cache.keys()]) {
    if (k.startsWith(prefix)) cache.delete(k);
  }
}

export function clearCache() {
  cache.clear();
}

export interface FetchState<T> {
  data: T | undefined;
  loading: boolean;
  revalidating: boolean;
  error: string | null;
  reload: () => Promise<void>;
  refresh: () => void;
}

export function useFetch<T>(
  key: string | null,
  fetcher: () => Promise<T>,
): FetchState<T> {
  const cached = key ? cacheGet(key) : undefined;
  const [data, setData] = useState<T | undefined>(cached?.value as T | undefined);
  const [loading, setLoading] = useState(!cached && !!key);
  const [revalidating, setRevalidating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetcherRef = useRef(fetcher);
  fetcherRef.current = fetcher;

  const [nonce, setNonce] = useState(0);
  const lastNonce = useRef(0);

  const runId = useRef(0);
  const alive = useRef(true);
  useEffect(() => () => { alive.current = false; }, []);

  const run = useCallback(async (k: string, hasCached: boolean) => {
    const id = ++runId.current;
    if (hasCached) setRevalidating(true); else setLoading(true);
    setError(null);
    try {
      const v = await fetcherRef.current();
      if (!alive.current || id !== runId.current) return;
      cacheSet(k, v);
      setData(v);
    } catch (ex: any) {
      if (!alive.current || id !== runId.current) return;
      setError(ex?.message ?? String(ex));
      if (!hasCached) setData(undefined);
    } finally {
      if (alive.current && id === runId.current) { setLoading(false); setRevalidating(false); }
    }
  }, []);

  useEffect(() => {
    if (!key) { setLoading(false); return; }
    const forcing = nonce !== lastNonce.current;
    lastNonce.current = nonce;
    if (forcing) cache.delete(key);
    const hit = cacheGet(key);
    if (hit) { setData(hit.value as T); setLoading(false); }
    else { setData(undefined); setLoading(true); }
    void run(key, !!hit);
  }, [key, nonce, run]);

  const reload = useCallback(async () => {
    if (!key) return;
    cache.delete(key);
    await run(key, true);
  }, [key, run]);

  const refresh = useCallback(() => setNonce(n => n + 1), []);

  return { data, loading, revalidating, error, reload, refresh };
}

export function qs(prefix: string, params: Record<string, unknown>): string {
  const parts = Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== null && v !== "")
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`);
  return parts.length ? `${prefix}?${parts.join("&")}` : prefix;
}
