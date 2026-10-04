import { useEffect, type FormEvent, type ReactNode } from "react";
import { useT } from "@/i18n/LanguageContext";

export function ModalShell({
  title, onClose, onSubmit, saving, err, children, submitLabel, submitDisabled, cancelLabel,
}: {
  title: string;
  onClose: () => void;
  onSubmit: (e: FormEvent) => void;
  saving?: boolean;
  err?: string | null;
  children: ReactNode;
  submitLabel?: string;
  submitDisabled?: boolean;
  cancelLabel?: string;
}) {
  const t = useT();

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape" && !saving) onClose(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose, saving]);

  return (
    <div className="modal-overlay" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}>
      <form className="modal-box" onSubmit={onSubmit}>
        <div className="modal-head">
          <span>{title}</span>
          <span className="modal-close" onClick={onClose}>&times;</span>
        </div>
        <div className="modal-body">
          {err && (
            <div className="mfg bg-rose-50 text-rose-600 py-2 px-2.5 rounded text-[12px]">
              {err}
            </div>
          )}
          {children}
        </div>
        <div className="modal-actions">
          <button type="button" className="btn btn-cancel" onClick={onClose} disabled={saving}>{cancelLabel ?? t("modalCancel", "Cancel")}</button>
          <button type="submit" className="btn btn-primary" disabled={saving || submitDisabled}>{saving ? t("saving", "Saving…") : (submitLabel ?? t("modalSave", "Save"))}</button>
        </div>
      </form>
    </div>
  );
}

export function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="mfg">
      <label>{label}</label>
      {children}
    </div>
  );
}
