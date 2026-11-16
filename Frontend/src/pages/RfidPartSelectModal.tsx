import { useEffect, useMemo, useRef, useState } from "react";
import { deleteStockScanPart, getStockScanParts, setStockScanTargets, type StockScanPart } from "@/api/stockScan";
import { useT } from "@/i18n/LanguageContext";
import { useConfirm } from "@/lib/Confirm";
import { errorText } from "@/lib/errorText";
import { ModalShell } from "@/lib/ModalShell";
import { useToast } from "@/lib/Toast";

export function RfidPartSelectModal({ onClose, onSaved, onChanged, canDelete }: { onClose: () => void; onSaved: () => Promise<void>; onChanged: () => Promise<void>; canDelete: boolean }) {
  const t = useT();
  const toast = useToast();
  const ask = useConfirm();
  const [parts, setParts] = useState<StockScanPart[]>([]);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [filter, setFilter] = useState("");
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const allRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    let active = true;
    getStockScanParts().then(rows => {
      if (!active) return;
      setParts(rows);
      setSelected(new Set(rows.filter(row => row.isTarget).map(row => row.itemCode ?? "")));
    }).catch(ex => { if (active) toast.err(errorText(t, ex)); });
    return () => { active = false; };
  }, []);

  const visible = useMemo(() => {
    const query = filter.trim().toLocaleLowerCase();
    return parts.filter(row => (row.itemCode ?? "").toLocaleLowerCase().includes(query) || (row.itemName ?? "").toLocaleLowerCase().includes(query));
  }, [parts, filter]);
  const visibleSelected = visible.filter(row => selected.has(row.itemCode ?? "")).length;
  const allSelected = visible.length > 0 && visibleSelected === visible.length;
  useEffect(() => { if (allRef.current) allRef.current.indeterminate = visibleSelected > 0 && !allSelected; }, [visibleSelected, allSelected]);

  const toggle = (codes: string[], checked: boolean) => setSelected(current => {
    const next = new Set(current);
    for (const code of codes) { if (checked) next.add(code); else next.delete(code); }
    return next;
  });

  const remove = async (row: StockScanPart) => {
    const code = row.itemCode ?? "";
    const name = [row.itemCode, row.itemName].filter(Boolean).join(" ") || "—";
    const ok = await ask({ message: t("rsdDeletePartConfirm", "Delete all {0} tags of {1}? This cannot be undone.").replace("{0}", String(row.total)).replace("{1}", name), confirmLabel: t("modalOk", "OK") });
    if (!ok) return;
    setDeleting(true);
    try {
      const result = await deleteStockScanPart(code);
      setParts(result.parts);
      setSelected(current => { const next = new Set(current); next.delete(code); return next; });
      toast.ok(t("rsdDeletedTags", "{0} tags deleted.").replace("{0}", String(result.deleted)));
      await onChanged();
    } catch (ex) { toast.err(errorText(t, ex)); }
    finally { setDeleting(false); }
  };

  return <ModalShell title={t("rsdSelectPartsTitle", "Select parts to count")} onClose={onClose} saving={saving || deleting} submitDisabled={selected.size === 0} onSubmit={async e => {
    e.preventDefault();
    if (!selected.size) return;
    setSaving(true);
    try { await setStockScanTargets([...selected]); await onSaved(); }
    catch (ex) { toast.err(errorText(t, ex)); }
    finally { setSaving(false); }
  }}>
    <div className="space-y-3">
      <input type="text" value={filter} onChange={e => setFilter(e.target.value)} placeholder={t("rsdPartFilter", "Filter by part no or name")} aria-label={t("rsdPartFilter", "Filter by part no or name")} />
      <div className="tbl-wrap max-h-96 overflow-auto"><table className="tbl"><thead><tr>
        <th><input ref={allRef} type="checkbox" checked={allSelected} disabled={!visible.length} onChange={e => toggle(visible.map(row => row.itemCode ?? ""), e.target.checked)} aria-label={t("rsdSelectParts", "Select Parts")} /></th>
        <th>{t("rsdPartNo", "Part No")}</th><th>{t("rsdPartName", "Part Name")}</th><th>{t("rsdTagCount", "Tags")}</th><th>{t("rsdScannedFilter", "Scanned")}</th>
        {canDelete && <th>{t("btnDelete", "Delete")}</th>}
      </tr></thead><tbody>{visible.map(row => <tr key={row.itemCode ?? ""}>
        <td><input type="checkbox" checked={selected.has(row.itemCode ?? "")} onChange={e => toggle([row.itemCode ?? ""], e.target.checked)} aria-label={`${t("rsdSelectParts", "Select Parts")} ${row.itemCode || row.itemName || "—"}`} /></td>
        <td>{row.itemCode || "—"}</td><td>{row.itemName || "—"}</td><td>{row.total}</td><td>{row.scanned} / {row.total}</td>
        {canDelete && <td><button type="button" className="btn btn-delete" disabled={saving || deleting} onClick={() => void remove(row)}>{t("btnDelete", "Delete")}</button></td>}
      </tr>)}</tbody></table></div>
      <div className="text-sm text-slate-600">{t("rsdPartsSelected", "{0} / {1} parts selected").replace("{0}", String(selected.size)).replace("{1}", String(parts.length))}</div>
    </div>
  </ModalShell>;
}
