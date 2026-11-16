import { useCallback, useMemo, useState } from "react";
import {
  createPartner, deletePartner, exportPartnersCsv, listPartners, updatePartner,
  type Partner, type PartnerWrite,
} from "@/api/partners";
import { CommonToolbar } from "@/lib/CommonToolbar";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { Field, ModalShell } from "@/lib/ModalShell";
import { SortTh, useSort } from "@/lib/SortableTh";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { useUrlNumber, useUrlSearch, useUrlPatch } from "@/lib/useUrlState";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav } from "@/lib/useShortcuts";
import { useRowSelect } from "@/lib/rowSelect";
import { errorText } from "@/lib/errorText";

type ModalMode = "new" | "edit" | "copy" | null;

export function PartnerPage() {
  const t = useT();
  const toast = useToast();
  const ask = useConfirm();
  const { flash, flashCls } = useRowFlash();
  const [page, setPage] = useUrlNumber("page", 1);
  const patch = useUrlPatch();
  const [pageSize] = useUrlNumber("size", 50, PAGE_SIZES);
  const { q, draft: qDraft, setDraft: setQDraft } = useUrlSearch();
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [modal, setModal] = useState<ModalMode>(null);
  const [target, setTarget] = useState<Partner | null>(null);

  const { sort, toggleSort } = useSort("partner_code");
  const onSort = toggleSort;

  const { data, loading, error: err, reload } = useFetch(
    qs("partners", { page, pageSize, q, sort: sort.value }),
    () => listPartners({ page, pageSize, q, sort: sort.value }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("partners"); await reload(); }, [reload]);

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => r.partnerCode === [...selected][0]) ?? null : null,
    [selected, rows]);
  const toggle = (c: string) => setSelected(p => { const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s; });
  const toggleAll = () => setSelected(p => p.size === rows.length ? new Set() : new Set(rows.map(r => r.partnerCode)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(r => r.partnerCode), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);

  return (
    <div className="pg on">
      <CommonToolbar fn="m_partner"
        searchDraft={qDraft} onSearchDraftChange={setQDraft}
        onSearch={() => setPage(1)}
        onNew={() => { setModal("new"); setTarget(null); }}
        onEdit={() => selectedRow && (setModal("edit"), setTarget(selectedRow))}
        onCopy={() => selectedRow && (setModal("copy"), setTarget(selectedRow))}
        onDelete={async () => {
          if (selected.size === 0) return toast.err(t("alertSelectDelete", "Please select at least one row to delete."));
          if (!await ask({
            message: t("confirmDelete", "Delete the selected row(s)?"),
            detail: `${t("thSelectedCount", "Selected")}: ${selected.size}`,
          })) return;
          const n = selected.size;
    try {
      for (const c of selected) await deletePartner(c);
      setSelected(new Set()); await load();
      toast.ok(`${t("toastDeleted", "Deleted")}: ${n}`);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        onExport={async () => {
          try { const { blob, filename } = await exportPartnersCsv();
            const u = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = u; a.download = filename; a.click(); URL.revokeObjectURL(u);
          } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        disableEdit={!selectedRow} disableCopy={!selectedRow} disableDelete={selected.size === 0}
        extraKeys={rowNav} />
      {err && <div className="page-info text-rose-600">{err}</div>}
      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <th className="chk-col">
              <input type="checkbox" className="row-chk"
                checked={selected.size > 0 && selected.size === rows.length}
                ref={el => { if (el) el.indeterminate = selected.size > 0 && selected.size < rows.length; }}
                onChange={toggleAll} />
            </th>
            <SortTh field="partner_code" sort={sort} onSort={onSort}>{t("thPartnerCode", "Partner Code")}</SortTh>
            <SortTh field="partner_name" sort={sort} onSort={onSort}>{t("thPartnerName", "Partner Name")}</SortTh>
            <SortTh field="is_supplier" sort={sort} onSort={onSort}>{t("receivingMgmt", "Receiving")}</SortTh>
            <SortTh field="is_customer" sort={sort} onSort={onSort}>{t("shippingMgmt", "Shipping")}</SortTh>
            <SortTh field="is_active" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="remark_1" sort={sort} onSort={onSort}>{t("thRemark1", "Remark 1")}</SortTh>
            <SortTh field="remark_2" sort={sort} onSort={onSort}>{t("thRemark2", "Remark 2")}</SortTh>
            <SortTh field="remark_3" sort={sort} onSort={onSort}>{t("thRemark3", "Remark 3")}</SortTh>
            <SortTh field="remark_4" sort={sort} onSort={onSort}>{t("thRemark4", "Remark 4")}</SortTh>
            <SortTh field="remark_5" sort={sort} onSort={onSort}>{t("thRemark5", "Remark 5")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={11} />}
            {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={11} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
            {!loading && rows.map(r => (
              <tr key={r.partnerCode} data-row-code={r.partnerCode} {...rowProps(r.partnerCode)} className={[flashCls(r.partnerCode), r.partnerCode === cursorCode ? "is-cursor" : ""].filter(Boolean).join(" ") || undefined}>
                <td className="chk-col"><input type="checkbox" className="row-chk"
                  checked={selected.has(r.partnerCode)} onChange={() => toggle(r.partnerCode)} /></td>
                <td>{r.partnerCode}</td>
                <td className="text-left">{r.partnerName}</td>
                <td className="text-left"><span className={"badge " + (r.isSupplier ? "badge-ok" : "badge-ng")}>{r.isSupplier ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}</span></td>
                <td className="text-left"><span className={"badge " + (r.isCustomer ? "badge-ok" : "badge-ng")}>{r.isCustomer ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}</span></td>
                <td className="text-left"><span className={"badge " + (r.isActive ? "badge-ok" : "badge-ng")}>{r.isActive ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}</span></td>
                <td className="text-left">{r.remark1 ?? "—"}</td>
                <td className="text-left">{r.remark2 ?? "—"}</td>
                <td className="text-left">{r.remark3 ?? "—"}</td>
                <td className="text-left">{r.remark4 ?? "—"}</td>
                <td className="text-left">{r.remark5 ?? "—"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <PageFooter total={total} page={page} pageSize={pageSize} onGoPage={p => setPage(p, { push: true })} onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
      {modal && <PartnerModal mode={modal} source={target}
        onClose={() => setModal(null)} onSaved={async (savedCode) => {
          setModal(null); setSelected(new Set()); await load();
          flash(savedCode);
          toast.ok(t("toastSaved", "Saved"));
        }} />}
    </div>
  );
}

function PartnerModal({ mode, source, onClose, onSaved }: {
  mode: "new" | "edit" | "copy"; source: Partner | null;
  onClose: () => void; onSaved: (savedCode: string) => void;
}) {
  const t = useT();
  const isEdit = mode === "edit";
  const [form, setForm] = useState<PartnerWrite>({
    partnerCode: isEdit ? source!.partnerCode : "",
    partnerName: mode === "new" ? "" : source?.partnerName ?? "",
    isSupplier: source?.isSupplier ?? false,
    isCustomer: source?.isCustomer ?? false,
    country: source?.country ?? "", address: source?.address ?? "",
    isActive: source?.isActive ?? true,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "",
    remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "",
    remark5: source?.remark5 ?? "",
  });
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  return (
    <ModalShell title={t("mPartner", "Business Partner Master")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          if (isEdit) await updatePartner(source!.partnerCode, form); else await createPartner(form);
          onSaved(form.partnerCode);
        }
        catch (ex: any) { setErr(errorText(t, ex)); } finally { setSaving(false); }
      }}>
      <Field label={t("thPartnerCode", "Partner Code")}>
        <input maxLength={20} required value={form.partnerCode} readOnly={isEdit}
          onChange={e => setForm(f => ({ ...f, partnerCode: e.target.value }))}
          style={isEdit ? { background: "#f7fafc", color: "#718096" } : undefined} />
      </Field>
      <Field label={t("thPartnerName", "Partner Name")}>
        <input maxLength={100} required value={form.partnerName}
          onChange={e => setForm(f => ({ ...f, partnerName: e.target.value }))} />
      </Field>
      <Field label={t("thAuthority", "Authority")}>
        <div className="flex gap-4 pt-1.5">
          <label className="flex items-center gap-1.5 font-normal">
            <input type="checkbox" checked={!!form.isSupplier} onChange={e => setForm(f => ({ ...f, isSupplier: e.target.checked }))} />
            {t("thSupplier", "Supplier")}
          </label>
          <label className="flex items-center gap-1.5 font-normal">
            <input type="checkbox" checked={!!form.isCustomer} onChange={e => setForm(f => ({ ...f, isCustomer: e.target.checked }))} />
            {t("thCustomer", "Customer")}
          </label>
        </div>
      </Field>
      <Field label={t("thCountry", "Country")}>
        <input maxLength={50} value={form.country ?? ""} onChange={e => setForm(f => ({ ...f, country: e.target.value }))} />
      </Field>
      <Field label={t("thAddress", "Address")}>
        <input maxLength={500} value={form.address ?? ""} onChange={e => setForm(f => ({ ...f, address: e.target.value }))} />
      </Field>
      <Field label={t("thStatus", "Status")}>
        <select value={form.isActive ? "1" : "0"} onChange={e => setForm(f => ({ ...f, isActive: e.target.value === "1" }))}>
          <option value="1">{t("statusEnable", "Enable")}</option><option value="0">{t("statusDisable", "Disable")}</option>
        </select>
      </Field>
      {(["remark1", "remark2", "remark3", "remark4", "remark5"] as const).map((k, i) => (
        <Field key={k} label={t(`thRemark${i + 1}`, `Remark ${i + 1}`)}>
          <input maxLength={200} value={(form[k] ?? "") as string}
            onChange={e => setForm(f => ({ ...f, [k]: e.target.value }))} />
        </Field>
      ))}
    </ModalShell>
  );
}
