import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from "react";
import { useT } from "@/i18n/LanguageContext";

interface ConfirmOpts {
  message: string;
  detail?: string;
  confirmLabel?: string;
}

type Ask = (o: ConfirmOpts) => Promise<boolean>;

const Ctx = createContext<Ask | null>(null);

export function useConfirm(): Ask {
  const ask = useContext(Ctx);
  if (!ask) throw new Error("useConfirm must be used inside <ConfirmProvider>");
  return ask;
}

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const t = useT();
  const [opts, setOpts] = useState<ConfirmOpts | null>(null);
  const resolver = useRef<((v: boolean) => void) | null>(null);

  const ask = useCallback<Ask>(o => {
    setOpts(o);
    return new Promise<boolean>(res => { resolver.current = res; });
  }, []);

  const close = (v: boolean) => {
    setOpts(null);
    resolver.current?.(v);
    resolver.current = null;
  };

  return (
    <Ctx.Provider value={ask}>
      {children}
      {opts && (
        <div className="modal-overlay" onMouseDown={e => { if (e.target === e.currentTarget) close(false); }}>
          <div className="modal-box w-[390px]"
            onKeyDown={e => { if (e.key === "Escape") close(false); }}>
            <div className="modal-head bg-rose-50 text-rose-700">
              <span>{t("confirmTitle", "Confirm")}</span>
              <span className="modal-close" onClick={() => close(false)}>&times;</span>
            </div>
            <div className="modal-body">
              <div className="text-[13.5px] leading-relaxed text-slate-800">{opts.message}</div>
              {opts.detail && <div className="mt-2 text-[12.5px] text-slate-500">{opts.detail}</div>}
            </div>
            <div className="modal-actions">
              <button type="button" className="btn btn-cancel" onClick={() => close(false)}>
                {t("modalCancel", "Cancel")}
              </button>
              <button type="button" className="btn btn-danger" autoFocus onClick={() => close(true)}>
                {opts.confirmLabel ?? t("btnDelete", "Delete")}
              </button>
            </div>
          </div>
        </div>
      )}
    </Ctx.Provider>
  );
}
