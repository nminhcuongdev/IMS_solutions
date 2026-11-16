import { ApiCallError, type ApiError, type ApiListSuccess, type ApiSuccess } from "./types";

function resolveApiBase(): string {
  const explicit = (import.meta.env.VITE_API_BASE as string | undefined)?.trim();
  if (explicit) {
    const clean = explicit.replace(/\/+$/, "");
    return clean.startsWith("/") ? `${window.location.origin}${clean}` : clean;
  }

  const port = (import.meta.env.VITE_API_PORT as string | undefined)?.trim() || "8091";
  const { protocol, hostname } = window.location;
  return `${protocol}//${hostname}:${port}`;
}

export const API_BASE = resolveApiBase();
const BASE = API_BASE;
const TOKEN_KEY = "ims.token";

let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(fn: () => void) { onUnauthorized = fn; }

export function getToken() { return sessionStorage.getItem(TOKEN_KEY); }
export function setToken(t: string) { sessionStorage.setItem(TOKEN_KEY, t); }
export function clearToken() { sessionStorage.removeItem(TOKEN_KEY); }

interface RequestOptions {
  method?: string;
  body?: unknown;
  query?: Record<string, string | number | boolean | undefined | null>;
  skipAuth?: boolean;
}

export async function apiCall<T>(path: string, opts: RequestOptions = {}): Promise<T> {
  const url = buildUrl(path, opts.query);
  const headers: Record<string, string> = { "Content-Type": "application/json" };
  const token = getToken();
  if (token && !opts.skipAuth) headers.Authorization = `Bearer ${token}`;

  const res = await fetch(url, {
    method: opts.method ?? "GET",
    headers,
    body: opts.body === undefined ? undefined : JSON.stringify(opts.body),
  });

  if (res.status === 401 && !opts.skipAuth) {
    clearToken();
    onUnauthorized?.();
  }

  if (res.status === 204) return undefined as T;

  const text = await res.text();
  const json = text ? JSON.parse(text) : {};
  if (!res.ok || json?.success === false) {
    const errBody = (json as ApiError).error ?? {
      code: `HTTP-${res.status}`, httpStatus: res.status, severity: "Error",
      message: text || res.statusText,
    };
    throw new ApiCallError(errBody);
  }
  return (json as ApiSuccess<T>).data;
}

export async function apiList<T>(
  path: string, opts: Omit<RequestOptions, "method"> = {}
): Promise<ApiListSuccess<T>> {
  const url = buildUrl(path, opts.query);
  const headers: Record<string, string> = {};
  const token = getToken();
  if (token && !opts.skipAuth) headers.Authorization = `Bearer ${token}`;
  const res = await fetch(url, { method: "GET", headers });
  if (res.status === 401 && !opts.skipAuth) { clearToken(); onUnauthorized?.(); }
  const text = await res.text();
  const json = text ? JSON.parse(text) : {};
  if (!res.ok || json?.success === false) {
    const errBody = (json as ApiError).error ?? {
      code: `HTTP-${res.status}`, httpStatus: res.status, severity: "Error", message: text,
    };
    throw new ApiCallError(errBody);
  }
  return json as ApiListSuccess<T>;
}

export async function apiDownload(path: string): Promise<{ blob: Blob; filename: string }> {
  const url = buildUrl(path);
  const token = getToken();
  const res = await fetch(url, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  if (res.status === 401) { clearToken(); onUnauthorized?.(); }
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const cd = res.headers.get("Content-Disposition") ?? "";
  const m = cd.match(/filename="?([^";]+)"?/);
  const filename = m?.[1] ?? path.split("/").pop() ?? "download";
  return { blob: await res.blob(), filename };
}

function buildUrl(path: string, query?: RequestOptions["query"]) {
  const url = new URL(`${BASE}${path.startsWith("/") ? path : `/${path}`}`);
  if (query) {
    for (const [k, v] of Object.entries(query)) {
      if (v === undefined || v === null || v === "") continue;
      url.searchParams.set(k, String(v));
    }
  }
  return url.toString();
}
