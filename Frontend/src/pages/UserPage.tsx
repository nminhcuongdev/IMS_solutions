import { useCallback, useEffect, useMemo, useState } from "react";
import { roleBadge } from "@/lib/badges";
import {
  createUser, deleteUser, exportUsersCsv, listUsers, updateUser,
  type AppUser, type UserWrite,
} from "@/api/users";
import { listFactories, type Factory } from "@/api/factories";
import { CommonToolbar } from "@/lib/CommonToolbar";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { Field, ModalShell } from "@/lib/ModalShell";
import { SortTh, useSort } from "@/lib/SortableTh";
import { codeName, RefSelect } from "@/lib/RefSelect";
import { useLang, useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { useUrlNumber, useUrlSearch, useUrlPatch } from "@/lib/useUrlState";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav } from "@/lib/useShortcuts";
import { useRowSelect } from "@/lib/rowSelect";
import { errorText } from "@/lib/errorText";
import { listRoles, type RoleRow } from "@/api/roles";

type ModalMode = "new" | "edit" | "copy" | null;

const PASSWORD_MASK = "•".repeat(8);

const LANG_LABEL: Record<string, string> = { en: "langEn", ja: "langJa", vi: "langVi" };


export function UserPage() {
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
  const [target, setTarget] = useState<AppUser | null>(null);

  const [roles, setRoles] = useState<RoleRow[]>([]);
  useEffect(() => { listRoles().then(setRoles).catch(() => {}); }, []);
  const roleOf = (code: string) => roles.find(r => r.roleCode.toUpperCase() === code.toUpperCase());

  const { sort, toggleSort } = useSort("user_code");
  const onSort = toggleSort;

  const { data, loading, error: err, reload } = useFetch(
    qs("users", { page, pageSize, q, sort: sort.value }),
    () => listUsers({ page, pageSize, q, sort: sort.value }));
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("users"); await reload(); }, [reload]);

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => r.userCode === [...selected][0]) ?? null : null,
    [selected, rows]);
  const toggle = (c: string) => setSelected(p => { const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s; });
  const toggleAll = () => setSelected(p => p.size === rows.length ? new Set() : new Set(rows.map(r => r.userCode)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(r => r.userCode), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);

  return (
    <div className="pg on">
      <CommonToolbar fn="m_user"
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
      for (const c of selected) await deleteUser(c);
      setSelected(new Set()); await load();
      toast.ok(`${t("toastDeleted", "Deleted")}: ${n}`);
    } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        onExport={async () => {
          try { const { blob, filename } = await exportUsersCsv();
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
            <SortTh field="user_code" sort={sort} onSort={onSort}>{t("thUserCode", "User Code")}</SortTh>
            <SortTh field="user_name" sort={sort} onSort={onSort}>{t("thUserName", "User Name")}</SortTh>
            <th>{t("password", "Password")}</th>
            <SortTh field="role_code" sort={sort} onSort={onSort}>{t("thAuthority", "Authority")}</SortTh>
            <SortTh field="is_active" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="default_language" sort={sort} onSort={onSort}>{t("thDefaultLanguage", "Default Language")}</SortTh>
            <SortTh field="remark_1" sort={sort} onSort={onSort}>{t("thRemark1", "Remark 1")}</SortTh>
            <SortTh field="remark_2" sort={sort} onSort={onSort}>{t("thRemark2", "Remark 2")}</SortTh>
            <SortTh field="remark_3" sort={sort} onSort={onSort}>{t("thRemark3", "Remark 3")}</SortTh>
            <SortTh field="remark_4" sort={sort} onSort={onSort}>{t("thRemark4", "Remark 4")}</SortTh>
            <SortTh field="remark_5" sort={sort} onSort={onSort}>{t("thRemark5", "Remark 5")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={12} />}
            {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={12} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
            {!loading && rows.map(r => (
              <tr key={r.userCode} data-row-code={r.userCode} {...rowProps(r.userCode)} className={[flashCls(r.userCode), r.userCode === cursorCode ? "is-cursor" : ""].filter(Boolean).join(" ") || undefined}>
                <td className="chk-col"><input type="checkbox" className="row-chk"
                  checked={selected.has(r.userCode)} onChange={() => toggle(r.userCode)} /></td>
                <td>{r.userCode}</td>
                <td className="text-left">{r.userName}</td>
                <td>{PASSWORD_MASK}</td>
                <td><span className={"badge " + roleBadge(roleOf(r.roleCode)?.isBuiltin)}>{r.roleName ?? roleOf(r.roleCode)?.roleName ?? r.roleCode}</span></td>
                <td className="text-left"><span className={"badge " + (r.isActive ? "badge-ok" : "badge-ng")}>{r.isActive ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}</span></td>
                <td>{r.defaultLanguage ? t(LANG_LABEL[r.defaultLanguage] ?? "", r.defaultLanguage) : "—"}</td>
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
      {modal && <UserModal mode={modal} source={target}
        onClose={() => setModal(null)} onSaved={async (savedCode) => {
          setModal(null); setSelected(new Set()); await load();
          flash(savedCode);
          toast.ok(t("toastSaved", "Saved"));
        }} />}
    </div>
  );
}

function UserModal({ mode, source, onClose, onSaved }: {
  mode: "new" | "edit" | "copy"; source: AppUser | null;
  onClose: () => void; onSaved: (savedCode: string) => void;
}) {
  const { t, languages } = useLang();
  const isEdit = mode === "edit";
  const [form, setForm] = useState<UserWrite>({
    userCode: isEdit ? source!.userCode : "",
    userName: mode === "new" ? "" : source?.userName ?? "",
    roleCode: source?.roleCode ?? "",
    password: "",
    factoryCode: source?.factoryCode ?? "",
    defaultLanguage: source?.defaultLanguage ?? "en",
    isActive: source?.isActive ?? true,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "",
    remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "",
    remark5: source?.remark5 ?? "",
  });
  const [factories, setFactories] = useState<Factory[]>([]);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const [roles, setRoles] = useState<RoleRow[]>([]);
  useEffect(() => { listFactories({ pageSize: 200 }).then(r => setFactories(r.data)).catch(() => {}); }, []);
  useEffect(() => { listRoles().then(setRoles).catch(() => {}); }, []);

  return (
    <ModalShell title={t("mUser", "User Master")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          const body: UserWrite = { ...form, password: form.password || (isEdit ? null : "") };
          if (isEdit) await updateUser(source!.userCode, body);
          else await createUser(body);
          onSaved(form.userCode);
        } catch (ex: any) { setErr(errorText(t, ex)); } finally { setSaving(false); }
      }}>
      <Field label={t("thUserCode", "User Code")}>
        <input maxLength={20} required value={form.userCode} readOnly={isEdit}
          onChange={e => setForm(f => ({ ...f, userCode: e.target.value }))}
          style={isEdit ? { background: "#f7fafc", color: "#718096" } : undefined} />
      </Field>
      <Field label={t("thUserName", "User Name")}>
        <input maxLength={100} required value={form.userName}
          onChange={e => setForm(f => ({ ...f, userName: e.target.value }))} />
      </Field>
      <Field label={isEdit ? t("passwordKeepBlank", "Password (leave blank to keep current)") : t("password", "Password")}>
        <input type="password" maxLength={100} required={!isEdit}
          placeholder={isEdit ? t("passwordKeepPlaceholder", "Leave blank to keep the current password") : ""}
          value={form.password ?? ""}
          onChange={e => setForm(f => ({ ...f, password: e.target.value }))} />
      </Field>
      <Field label={t("thAuthority", "Authority")}>
        <select required value={form.roleCode}
          onChange={e => setForm(f => ({ ...f, roleCode: e.target.value }))}>
          {!form.roleCode && <option value="" />}
          {roles.map(r => <option key={r.roleCode} value={r.roleCode}>{r.roleCode} - {r.roleName}</option>)}
        </select>
      </Field>
      <Field label={t("saDefaultFactory", "Default Factory")}>
        <RefSelect value={form.factoryCode ?? ""}
          options={factories.map(fc => ({ value: fc.factoryCode, label: codeName(fc.factoryCode, fc.factoryName) }))}
          onChange={v => setForm(f => ({ ...f, factoryCode: v || null }))} />
      </Field>
      <Field label={t("thDefaultLanguage", "Default Language")}>
        <select value={form.defaultLanguage ?? ""}
          onChange={e => setForm(f => ({ ...f, defaultLanguage: e.target.value || null }))}>
          <option value="">—</option>
          {languages.map(l => <option key={l.languageCode} value={l.languageCode}>{l.languageName}</option>)}
        </select>
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
