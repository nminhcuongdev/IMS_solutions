import { useRef, useState, type FormEvent } from "react";
import {
  downloadImportTemplate, IMPORT_COLUMNS, importSchedule,
  type ImportKind, type ImportRowError,
} from "@/api/imports";
import { ModalShell } from "@/lib/ModalShell";
import { useT } from "@/i18n/LanguageContext";
import { errorText } from "@/lib/errorText";

function reason(t: (k: string, f: string) => string, r: ImportRowError): string {
  const s = t(r.code, r.message);
  if (!s.includes("{0}")) return s;
  return r.detail ? s.replace("{0}", r.detail) : r.message;
}

export function ScheduleImportModal({
  kind, onClose, onDone,
}: {
  kind: ImportKind;
  onClose: () => void;
  onDone: (imported: number) => void;
}) {
  const t = useT();
  const fileRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [rows, setRows] = useState<ImportRowError[] | null>(null);
  const cols = IMPORT_COLUMNS[kind];

  const onTemplate = async () => {
    try {
      const { blob, filename } = await downloadImportTemplate(kind);
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a"); a.href = url; a.download = filename; a.click();
      URL.revokeObjectURL(url);
    } catch (ex: any) { setErr(errorText(t, ex)); }
  };

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!file || busy) return;
    setBusy(true); setErr(null); setRows(null);
    try {
      const r = await importSchedule(kind, file);
      if (r.errors.length > 0) {
        setRows(r.errors);
      } else {
        onDone(r.importedCount);
      }
    } catch (ex: any) {
      setErr(errorText(t, ex));
    } finally {
      setBusy(false);
    }
  };

  return (
    <ModalShell
      title={t("btnImport", "Import")}
      onClose={onClose} onSubmit={onSubmit} saving={busy} err={err}
      submitLabel={t("btnImport", "Import")} submitDisabled={!file}>
      <div className="mfg">
        <label>{t("thFile", "File")}</label>
        <input
          ref={fileRef}
          type="file"
          accept=".xlsx,.csv"
          onChange={e => { setFile(e.target.files?.[0] ?? null); setRows(null); setErr(null); }} />
      </div>

      <div className="text-[12px] text-slate-500 mt-2 leading-[1.6]">
        {t("impColumns", "Columns: {0}.").replace("{0}", cols.columns.join(", "))}
        <br />
        {t("impRequired", "{0} are required; the rest may be left blank.").replace("{0}", cols.required.join(", "))}
        <br />
        <strong>{t("impAllOrNothing", "If any row is invalid, nothing is imported.")}</strong>
        {" "}
        <a className="text-brand-600 cursor-pointer underline" role="button" onClick={onTemplate}>
          {t("impTemplate", "Download template")}
        </a>
      </div>

      {rows && rows.length > 0 && (
        <div className="mt-3">
          <div className="text-rose-600 text-[12px] font-semibold mb-1.5">
            {t("impFailedRows", "Nothing was imported. Fix these rows and try again:")}
          </div>
          <div className="max-h-55 overflow-y-auto border border-[#e2e8f0] rounded-md">
            <table className="alert-mini-tbl">
              <thead>
                <tr>
                  <th>{t("csvRow", "Row")}</th>
                  <th>{t("thReason", "Reason")}</th>
                </tr>
              </thead>
              <tbody>
                {rows.map(r => (
                  <tr key={r.row}>
                    <td>{r.row}</td>
                    <td className="text-left">{reason(t, r)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </ModalShell>
  );
}
