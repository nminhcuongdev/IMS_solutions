import { useMemo, useState } from "react";
import { createProcess, deleteProcess, exportProcessesCsv, listProcesses, updateProcess, type Process, type ProcessWrite } from "@/api/processes";
import { useT } from "@/i18n/LanguageContext";
import { statusBadge } from "@/lib/badges";
import { CommonToolbar } from "@/lib/CommonToolbar";
import { useConfirm } from "@/lib/Confirm";
import { errorText } from "@/lib/errorText";
import { Field, ModalShell } from "@/lib/ModalShell";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { useRowSelect } from "@/lib/rowSelect";
import { SortTh, useSort } from "@/lib/SortableTh";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { useToast } from "@/lib/Toast";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav } from "@/lib/useShortcuts";
import { useUrlNumber, useUrlPatch, useUrlSearch } from "@/lib/useUrlState";
import { ProcessLabelModal } from "./ProcessLabelModal";
import { BTN_ICON } from "@/layout/icons";

type Mode = "new" | "edit" | "copy";
const remarkKeys = ["remark1", "remark2", "remark3", "remark4", "remark5"] as const;
const columns = [
  ["process_code", "prdProcessCode", "Process Code"], ["process_name", "prdProcessName", "Process Name"],
  ["display_order", "prdDisplayOrder", "Display Order"], ["is_active", "thStatus", "Status"],
  ...remarkKeys.map((_, i) => [`remark_${i + 1}`, `thRemark${i + 1}`, `Remark ${i + 1}`]),
];

export function ProcessPage() {
  const t = useT();
  const toast = useToast();
  const confirm = useConfirm();
  const { flash, flashCls } = useRowFlash();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);
  const { q, draft, setDraft } = useUrlSearch();
  const { sort, toggleSort } = useSort("display_order");
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState<Mode | null>(null);
  const [target, setTarget] = useState<Process | null>(null);
  const [labelOpen, setLabelOpen] = useState(false);
  const { data, loading, error, reload } = useFetch(qs("processes", { q, page, pageSize, sort: sort.value }), () => listProcesses({ q, page, pageSize, sort: sort.value }));
  const rows = data?.data ?? [];
  const chosen = useMemo(() => selected.size === 1 ? rows.find(r => selected.has(r.processCode)) ?? null : null, [rows, selected]);
  const picked = rows.filter(r => selected.has(r.processCode));
  const load = async () => { invalidate("processes"); await reload(); };
  const toggle = (code: string) => setSelected(prev => { const next = new Set(prev); next.has(code) ? next.delete(code) : next.add(code); return next; });
  const { keys, cursorCode, setCursorTo } = useRowNav({ codes: rows.map(r => r.processCode), setSelected });
  const rowProps = useRowSelect(toggle, setCursorTo);
  const open = (mode: Mode) => { setTarget(mode === "new" ? null : chosen); setModal(mode); };
  const remove = async () => {
    if (!selected.size) return toast.err(t("alertSelectDelete", "Please select at least one row to delete."));
    if (!await confirm({ message: t("confirmDelete", "Delete the selected row(s)?"), detail: `${t("thSelectedCount", "Selected")}: ${selected.size}` })) return;
    try { for (const code of selected) await deleteProcess(code); setSelected(new Set()); await load(); toast.ok(t("toastDeleted", "Deleted")); }
    catch (e) { toast.err(errorText(t, e)); }
  };
  const exportCsv = async () => {
    try { const { blob, filename } = await exportProcessesCsv(); const url = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = url; a.download = filename; a.click(); URL.revokeObjectURL(url); }
    catch (e) { toast.err(errorText(t, e)); }
  };
  return <div className="pg on">
    <CommonToolbar searchDraft={draft} onSearchDraftChange={setDraft} onSearch={() => setPage(1)} onNew={() => open("new")} onEdit={() => open("edit")} onCopy={() => open("copy")} onDelete={remove} onExport={exportCsv} disableEdit={!chosen} disableCopy={!chosen} disableDelete={!selected.size} extraKeys={keys} hideImport labelButton={<button className="btn btn-print" onClick={() => selected.size ? setLabelOpen(true) : toast.err(t("alertSelectAtLeastOne", "Please select at least one row."))}><img className="icn-img" src={BTN_ICON.print} alt="" /> <span>{t("btnLabel", "Label")}</span></button>} />
    {error && <div className="page-info" style={{ color: "#DC3545" }}>{error}</div>}
    <div className="tbl-wrap"><table className="tbl"><thead><tr><th className="chk-col"><input type="checkbox" className="row-chk" checked={selected.size > 0 && selected.size === rows.length} onChange={() => setSelected(selected.size === rows.length ? new Set() : new Set(rows.map(r => r.processCode)))} /></th>{columns.map(([field, key, fallback]) => <SortTh key={field} field={field} sort={sort} onSort={toggleSort}>{t(key, fallback)}</SortTh>)}</tr></thead><tbody>
      {loading && <TableSkeleton cols={10} />}
      {!loading && !rows.length && <tr><td colSpan={10} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
      {!loading && rows.map(r => <tr key={r.processCode} data-row-code={r.processCode} {...rowProps(r.processCode)} className={[flashCls(r.processCode), r.processCode === cursorCode ? "is-cursor" : ""].join(" ")}>
        <td className="chk-col"><input type="checkbox" className="row-chk" checked={selected.has(r.processCode)} onChange={() => toggle(r.processCode)} /></td>
        <td>{r.processCode}</td><td className="text-left">{r.processName}</td><td className="text-right">{r.displayOrder ?? "—"}</td>
        <td className="text-left"><span className={`badge ${statusBadge(r.isActive)}`}>{r.isActive ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}</span></td>
        {remarkKeys.map(key => <td key={key} className="text-left">{r[key] || "—"}</td>)}
      </tr>)}
    </tbody></table></div>
    <PageFooter total={data?.meta.total ?? 0} page={page} pageSize={pageSize} onGoPage={p => setPage(p, { push: true })} onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
    {modal && <ProcessModal mode={modal} source={target} onClose={() => setModal(null)} onSaved={async code => { setModal(null); setSelected(new Set()); await load(); flash(code); toast.ok(t("toastSaved", "Saved")); }} />}
    {labelOpen && <ProcessLabelModal rows={picked} onClose={() => setLabelOpen(false)} onPrinted={n => { setLabelOpen(false); toast.ok(`${t("mlPrintResult", "Print result")}: ${n}`); }} />}
  </div>;
}

function ProcessModal({ mode, source, onClose, onSaved }: { mode: Mode; source: Process | null; onClose: () => void; onSaved: (code: string) => void }) {
  const t = useT();
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState<ProcessWrite>({
    processCode: mode === "edit" ? source?.processCode ?? "" : "", processName: mode === "new" ? "" : source?.processName ?? "",
    displayOrder: source?.displayOrder ?? null, isActive: source?.isActive ?? true,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "", remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "", remark5: source?.remark5 ?? "",
  });
  const set = <K extends keyof ProcessWrite>(key: K, value: ProcessWrite[K]) => setForm(prev => ({ ...prev, [key]: value }));
  return <ModalShell title={t("mProcess", "Process Master")} onClose={onClose} saving={saving} err={error} onSubmit={async e => {
    e.preventDefault(); setSaving(true); setError(null);
    try { if (mode === "edit") await updateProcess(source!.processCode, form); else await createProcess(form); onSaved(form.processCode); }
    catch (ex) { setError(errorText(t, ex)); }
    finally { setSaving(false); }
  }}>
    <Field label={t("prdProcessCode", "Process Code")}><input maxLength={20} required value={form.processCode} readOnly={mode === "edit"} onChange={e => set("processCode", e.target.value)} /></Field>
    <Field label={t("prdProcessName", "Process Name")}><input maxLength={100} required value={form.processName} onChange={e => set("processName", e.target.value)} /></Field>
    <Field label={t("prdDisplayOrder", "Display Order")}><input type="number" min={0} value={form.displayOrder ?? ""} onChange={e => set("displayOrder", e.target.value === "" ? null : Number(e.target.value))} /></Field>
    <Field label={t("thStatus", "Status")}><select value={form.isActive ? "1" : "0"} onChange={e => set("isActive", e.target.value === "1")}><option value="1">{t("statusEnable", "Enable")}</option><option value="0">{t("statusDisable", "Disable")}</option></select></Field>
    {remarkKeys.map((key, i) => <Field key={key} label={t(`thRemark${i + 1}`, `Remark ${i + 1}`)}><input maxLength={200} value={form[key] ?? ""} onChange={e => set(key, e.target.value)} /></Field>)}
  </ModalShell>;
}
