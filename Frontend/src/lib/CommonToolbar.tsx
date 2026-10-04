import { useRef, useState, type ReactNode } from "react";
import { BTN_ICON } from "@/layout/icons";
import { useT } from "@/i18n/LanguageContext";
import { useShortcuts, type ShortcutMap } from "./useShortcuts";
import { ShortcutHelp } from "./ShortcutHelp";

export function CommonToolbar({
  searchDraft, onSearchDraftChange, onSearch,
  onNew, onEdit, onCopy, onDelete, onExport,
  disableEdit, disableCopy, disableDelete,
  extra, extraKeys, labelButton,
}: {
  searchDraft: string;
  onSearchDraftChange: (v: string) => void;
  onSearch: () => void;
  onNew: () => void;
  onEdit: () => void;
  onCopy: () => void;
  onDelete: () => void;
  onExport: () => void;
  disableEdit?: boolean;
  disableCopy?: boolean;
  disableDelete?: boolean;
  extra?: ReactNode;
  extraKeys?: ShortcutMap;
  labelButton?: ReactNode;
}) {
  const t = useT();
  const searchRef = useRef<HTMLInputElement>(null);
  const [showHelp, setShowHelp] = useState(false);

  useShortcuts({
    ...extraKeys,
    "/": () => searchRef.current?.focus(),
    n: onNew,
    N: onNew,
    e: () => { if (!disableEdit) onEdit(); },
    E: () => { if (!disableEdit) onEdit(); },
    c: () => { if (!disableCopy) onCopy(); },
    C: () => { if (!disableCopy) onCopy(); },
    Delete: () => { if (!disableDelete) onDelete(); },
    "?": () => setShowHelp(true),
    Escape: () => {
      if (showHelp) { setShowHelp(false); return; }
      if (document.activeElement === searchRef.current) searchRef.current?.blur();
    },
  });

  return (
    <div className="search-box">
      <div className="search-row">
        <input className="w-55"
          ref={searchRef}
          type="text"
          placeholder={t("searchCodeName", "Code or Name...")}
          value={searchDraft}
          onChange={e => onSearchDraftChange(e.target.value)}
          onKeyDown={e => { if (e.key === "Enter") onSearch(); }} />
        {extra}
      </div>
      <div className="search-btns">
        <button className="btn btn-primary" onClick={onSearch}>
          <img className="icn-img" src={BTN_ICON.search} alt="" /> <span>{t("search", "Search")}</span>
        </button>
        <button className="btn btn-add" onClick={onNew}>
          <PlusIcon /> <span>{t("btnNew", "New")}</span>
        </button>
        <button className="btn btn-edit" onClick={onEdit} disabled={disableEdit}>
          <img className="icn-img" src={BTN_ICON.edit} alt="" /> <span>{t("btnEdit", "Edit")}</span>
        </button>
        <button className="btn btn-outline" onClick={onCopy} disabled={disableCopy}>
          <img className="icn-img" src={BTN_ICON.copy} alt="" /> <span>{t("btnCopy", "Copy")}</span>
        </button>
        <button className="btn btn-delete" onClick={onDelete} disabled={disableDelete}>
          <TrashIcon /> <span>{t("btnDelete", "Delete")}</span>
        </button>
        {labelButton}
        <button className="btn btn-outline" disabled title={t("importDisabled", "CSV import is not available in this version.")}>
          <img className="icn-img" src={BTN_ICON.upload} alt="" /> <span>{t("btnImport", "Import")}</span>
        </button>
        <button className="btn btn-outline" onClick={onExport}>
          <ExportIcon /> <span>{t("btnExport", "Export")}</span>
        </button>
      </div>

      {showHelp && <ShortcutHelp onClose={() => setShowHelp(false)} />}
    </div>
  );
}

export function PlusIcon() {
  return <svg className="ic" viewBox="0 0 24 24"><path d="M11 5a1 1 0 0 1 2 0v6h6a1 1 0 0 1 0 2h-6v6a1 1 0 0 1-2 0v-6H5a1 1 0 0 1 0-2h6V5z" /></svg>;
}
export function TrashIcon() {
  return <svg className="ic" viewBox="0 -960 960 960"><path d="M261-120q-24.75 0-42.37-17.63Q201-155.25 201-180v-570h-41v-60h188v-30h264v30h188v60h-41v570q0 24-18 42t-42 18H261Zm438-630H261v570h438v-570ZM367-266h60v-399h-60v399Zm166 0h60v-399h-60v399ZM261-750v570-570Z" /></svg>;
}
export function ExportIcon() {
  return <svg className="ic" viewBox="0 -960 960 960"><path d="M780-160H260q-24 0-42-18t-18-42v-640q0-24 18-42t42-18h348l232 232v468q0 24-18 42t-42 18ZM578-662v-198H260v640h520v-442H578ZM140-40q-24 0-42-18t-18-42v-619h60v619h498v60H140Zm120-820v198-198 640-640Z" /></svg>;
}
