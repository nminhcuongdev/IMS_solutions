import type { ReactNode } from "react";
import { BTN_ICON } from "@/layout/icons";
import { useT } from "@/i18n/LanguageContext";
import { ExportIcon, PlusIcon } from "./CommonToolbar";
import { useScreenCan } from "@/auth/PermissionContext";

type Btn = { onClick?: () => void; disabled?: boolean; title?: string; children?: ReactNode; action?: string };

function useAllowed(action: string) {
  return useScreenCan()(action);
}

export function SearchButton({ onClick, disabled, title }: Btn) {
  const t = useT();
  return (
    <button className="btn btn-primary" data-role="search" onClick={onClick} disabled={disabled} title={title}>
      <img className="icn-img" src={BTN_ICON.search} alt="" /> <span>{t("search", "Search")}</span>
    </button>
  );
}

export function PrintButton({ onClick, disabled, title, children , action = "Label" }: Btn) {
  const allowed = useAllowed(action);
  if (!allowed) return null;
  return (
    <button className="btn btn-print" onClick={onClick} disabled={disabled} title={title}>
      <img className="icn-img" src={BTN_ICON.print} alt="" /> <span>{children}</span>
    </button>
  );
}

export function CsvButton({ onClick, disabled, title , action = "Export" }: Btn) {
  const allowed = useAllowed(action);
  const t = useT();
  if (!allowed) return null;
  return (
    <button className="btn btn-outline" onClick={onClick} disabled={disabled} title={title}>
      <ExportIcon /> <span>{t("csvExport", "CSV")}</span>
    </button>
  );
}

export function NewButton({ onClick, disabled, title , action = "New" }: Btn) {
  const allowed = useAllowed(action);
  const t = useT();
  if (!allowed) return null;
  return (
    <button className="btn btn-add" onClick={onClick} disabled={disabled} title={title}>
      <PlusIcon /> <span>{t("btnNew", "New")}</span>
    </button>
  );
}

function CycleIcon() {
  return <svg className="ic" viewBox="0 0 24 24"><path d="M12 3a9 9 0 1 0 9 9h-2a7 7 0 1 1-2.05-4.95L14 10h7V3l-2.35 2.35A9 9 0 0 0 12 3z" /></svg>;
}

export function StatusChangeButton({ onClick, disabled, title , action = "StatusChange" }: Btn) {
  const allowed = useAllowed(action);
  const t = useT();
  if (!allowed) return null;
  return (
    <button className="btn btn-primary" onClick={onClick} disabled={disabled} title={title}>
      <CycleIcon /> <span>{t("btnStatusChange", "Status Change")}</span>
    </button>
  );
}

export function ShortageButton({ onClick, disabled, title , action = "Shortage" }: Btn) {
  const allowed = useAllowed(action);
  const t = useT();
  if (!allowed) return null;
  return (
    <button className="btn btn-special" onClick={onClick} disabled={disabled} title={title}>
      <img className="icn-img" src={BTN_ICON.warning} alt="" /> <span>{t("btnShortage", "Shortage Registration")}</span>
    </button>
  );
}

export function ImportButton({ onClick, disabled, title , action = "Import" }: Btn) {
  const allowed = useAllowed(action);
  const t = useT();
  if (!allowed) return null;
  return (
    <button className="btn btn-outline" onClick={onClick} disabled={disabled} title={title}>
      <img className="icn-img" src={BTN_ICON.upload} alt="" /> <span>{t("btnImport", "Import")}</span>
    </button>
  );
}
