import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from "react";

type ToastKind = "ok" | "err" | "info";
interface Toast { id: number; kind: ToastKind; msg: string; out?: boolean }

interface ToastApi {
  ok: (msg: string) => void;
  err: (msg: string) => void;
  info: (msg: string) => void;
}

const Ctx = createContext<ToastApi | null>(null);

export function useToast(): ToastApi {
  const api = useContext(Ctx);
  if (!api) throw new Error("useToast phải nằm trong <ToastProvider>");
  return api;
}

const TOAST_ACCENT: Record<ToastKind, string> = {
  ok: "border-emerald-500",
  err: "border-rose-500",
  info: "border-brand-500",
};

const SHOW_MS = 3200;
const FADE_MS = 180;

export function ToastProvider({ children }: { children: ReactNode }) {
  const [list, setList] = useState<Toast[]>([]);
  const seq = useRef(0);
  const timers = useRef(new Map<number, ReturnType<typeof setTimeout>>());

  const remove = useCallback((id: number) => {
    const tm = timers.current.get(id);
    if (tm) { clearTimeout(tm); timers.current.delete(id); }
    setList(p => p.map(t => t.id === id ? { ...t, out: true } : t));
    setTimeout(() => setList(p => p.filter(t => t.id !== id)), FADE_MS);
  }, []);

  const push = useCallback((kind: ToastKind, msg: string) => {
    const id = ++seq.current;
    setList(p => [...p, { id, kind, msg }]);
    if (kind !== "err") {
      timers.current.set(id, setTimeout(() => remove(id), SHOW_MS));
    }
  }, [remove]);

  const api = useMemo<ToastApi>(() => ({
    ok:   m => push("ok", m),
    err:  m => push("err", m),
    info: m => push("info", m),
  }), [push]);

  return (
    <Ctx.Provider value={api}>
      {children}
      <div className="pointer-events-none fixed top-4 right-4 z-[2000] flex flex-col gap-2">
        {list.map(t => (
          <div
            key={t.id}
            role={t.kind === "err" ? "alert" : "status"}
            className={`pointer-events-auto flex max-w-[380px] min-w-[260px] items-start gap-2.5 rounded-xl border-l-4 bg-white px-3.5 py-3 text-[13px] text-slate-800 shadow-lg ring-1 ring-slate-900/5 motion-reduce:animate-none ${
              TOAST_ACCENT[t.kind]} ${t.out ? "animate-toast-out" : "animate-toast-in"}`}>
            <span className="flex-1 leading-[1.45] break-words">{t.msg}</span>
            <span className="shrink-0 cursor-pointer text-base leading-none text-slate-400 hover:text-slate-700" onClick={() => remove(t.id)}>&times;</span>
          </div>
        ))}
      </div>
    </Ctx.Provider>
  );
}
