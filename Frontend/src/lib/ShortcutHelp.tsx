import { useT } from "@/i18n/LanguageContext";

export function ShortcutHelp({ onClose }: { onClose: () => void }) {
  const t = useT();

  const rows: [string, string, string][] = [
    ["/",        "scKeySearch",  "Jump to the search box"],
    ["N",        "scKeyNew",     "New"],
    ["E",        "scKeyEdit",    "Edit the selected row"],
    ["C",        "scKeyCopy",    "Copy the selected row"],
    ["Delete",   "scKeyDelete",  "Delete the selected row(s)"],
    ["↑ ↓",      "scKeyMove",    "Move between rows"],
    ["Shift+↑↓", "scKeyExtend",  "Add rows to the selection"],
    ["Space",    "scKeyPick",    "Tick / untick the current row"],
    ["Esc",      "scKeyEsc",     "Close dialog, or leave the search box"],
    ["?",        "scKeyHelp",    "Show this list"],
  ];

  return (
    <div className="modal-overlay" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-box w-[420px]"
        onKeyDown={e => { if (e.key === "Escape") onClose(); }}>
        <div className="modal-head">
          <span>{t("scTitle", "Keyboard shortcuts")}</span>
          <span className="modal-close" onClick={onClose}>&times;</span>
        </div>
        <div className="modal-body">
          <table className="w-full border-collapse text-[13px]">
            <tbody>
              {rows.map(([key, labelKey, en]) => (
                <tr key={key} className="border-b border-slate-100">
                  <td className="w-[92px] px-1 py-[7px] align-middle"><kbd>{key}</kbd></td>
                  <td className="px-1 py-[7px] align-middle">{t(labelKey, en)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <p className="mt-3 text-[11.5px] leading-normal text-slate-500">
            {t("scNote", "Shortcuts are off while you are typing in a field.")}
          </p>
        </div>
        <div className="modal-actions">
          <button type="button" className="btn btn-primary" autoFocus onClick={onClose}>
            {t("modalClose", "Close")}
          </button>
        </div>
      </div>
    </div>
  );
}
