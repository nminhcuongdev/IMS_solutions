import { useCallback, useEffect, useMemo, useState, type FormEvent } from "react";
import { getPermissionMatrix, savePermissionMatrix, type PermissionMatrixRow } from "@/api/permissions";
import { createRole, deleteRole, listRoles, updateRole, type RoleRow } from "@/api/roles";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { SortTh } from "@/lib/SortableTh";
import { useLocalSort } from "@/lib/useLocalSort";
import { PageFooter } from "@/lib/PageFooter";
import { ModalShell, Field } from "@/lib/ModalShell";
import { PlusIcon, TrashIcon } from "@/lib/CommonToolbar";
import { BTN_ICON } from "@/layout/icons";
import { FUNCTION_LABEL_KEY, groupLabelKey } from "@/i18n/labelKeys";
import { errorText } from "@/lib/errorText";
import { usePermissions } from "@/auth/PermissionContext";

type Translate = (k: string, f: string) => string;

const ACTION_LABEL: Record<string, [string, string]> = {
  Open: ["permActionView", "Open"],
  New: ["btnNew", "New"],
  Edit: ["btnEdit", "Edit"],
  Copy: ["btnCopy", "Copy"],
  Delete: ["btnDelete", "Delete"],
  Import: ["btnImport", "Import"],
  Export: ["btnExport", "Export"],
  Label: ["permActionLabel", "Label"],
  StatusChange: ["permActionStatusChange", "Status Change"],
  Shortage: ["permActionShortage", "Shortage"],
  Reprint: ["permActionReprint", "Reprint"],
  Adjust: ["permActionAdjust", "Adjust"],
  Disposal: ["permActionDisposal", "Disposal"],
  Hold: ["permActionHold", "Hold"],
  Release: ["permActionRelease", "Release"],
  RestoreDisposal: ["permActionRestoreDisposal", "Restore Disposal"],
  Ping: ["permActionPing", "Ping"],
  MaterialPicking: ["permActionMaterialPicking", "Material Picking"],
  Reverse: ["permActionReverse", "Reverse"],
  Start: ["permActionStart", "Start"],
  Complete: ["permActionComplete", "Complete"],
  Finalize: ["permActionFinalize", "Finalize"],
  Save: ["modalSave", "Save"],
  Reset: ["rsdReset", "Reset"],
  SelectTarget: ["rsdSelectParts", "Select Parts"],
  Register: ["rsdRegisterEpc", "Register EPC"],
};

function detailText(r: PermissionMatrixRow, t: Translate): string {
  if (r.device === "HT") return t("permDetailUsable", "Open (usable)");
  if (r.actionType === "Open") return t("permDetailScreen", "Open (screen display)");
  const hit = ACTION_LABEL[r.actionType];
  const name = hit ? t(hit[0], hit[1]) : r.actionType;
  return t("permDetailButton", "{0} (button)").replace("{0}", name);
}

const tabText = (r: PermissionMatrixRow, t: Translate) =>
  r.largeCode ? t(groupLabelKey(r.largeCode), r.largeName || r.largeCode) : "—";
const functionText = (r: PermissionMatrixRow, t: Translate) =>
  t(FUNCTION_LABEL_KEY[r.functionCode] ?? r.functionCode, r.functionName);

type Mode = "new" | "edit" | null;

export function PermissionPage() {
  const t = useT();
  const toast = useToast();
  const confirm = useConfirm();
  const perms = usePermissions();
  const can = (a: string) => perms.can("m_permission", a);

  const [roles, setRoles] = useState<RoleRow[]>([]);
  const [roleDraft, setRoleDraft] = useState("");
  const [deviceDraft, setDeviceDraft] = useState("");
  const [textDraft, setTextDraft] = useState("");
  const [roleCode, setRoleCode] = useState("");
  const [device, setDevice] = useState("");
  const [text, setText] = useState("");
  const [rows, setRows] = useState<PermissionMatrixRow[]>([]);
  const [draft, setDraft] = useState<Record<string, boolean>>({});
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(50);
  const [mode, setMode] = useState<Mode>(null);

  const loadRoles = useCallback(async () => {
    const list = await listRoles();
    setRoles(list);
    return list;
  }, []);

  const loadMatrix = useCallback(async (code: string) => {
    if (!code) { setRows([]); setDraft({}); return; }
    setLoading(true); setErr(null);
    try {
      const data = await getPermissionMatrix(code);
      setRows(data);
      setDraft(Object.fromEntries(data.map(r => [r.permissionCode, r.isAllowed])));
    } catch (e) { setErr(errorText(t, e)); }
    finally { setLoading(false); }
  }, [t]);

  useEffect(() => {
    loadRoles().then(list => {
      const first = list[0]?.roleCode ?? "";
      setRoleDraft(first); setRoleCode(first);
      loadMatrix(first);
    }).catch(e => setErr(errorText(t, e)));
  }, []);

  const role = roles.find(r => r.roleCode === roleCode) ?? null;
  const locked = !!role?.isBuiltin;
  const changed = useMemo(
    () => rows.filter(r => draft[r.permissionCode] !== undefined && draft[r.permissionCode] !== r.isAllowed),
    [rows, draft]);
  const dirty = changed.length > 0;

  const discardOk = async () => !dirty || await confirm({
    message: t("permDiscardConfirm", "Discard the unsaved changes?"),
    confirmLabel: t("permDiscard", "Discard"),
  });

  const visible = useMemo(() => {
    const q = text.trim().toLowerCase();
    return rows.filter(r => (!device || r.device === device)
      && (!q || [tabText(r, t), functionText(r, t), detailText(r, t)].some(s => s.toLowerCase().includes(q))));
  }, [rows, device, text, t]);

  const { sorted, sort, onSort } = useLocalSort(visible, r => r.permissionCode, {
    device: r => r.device,
    tab: r => tabText(r, t),
    function: r => functionText(r, t),
    detail: r => detailText(r, t),
    enable: r => draft[r.permissionCode] ?? false,
  });
  const pageRows = sorted.slice((page - 1) * pageSize, page * pageSize);

  const onSearch = async () => {
    if (roleDraft !== roleCode && !(await discardOk())) return;
    setRoleCode(roleDraft); setDevice(deviceDraft); setText(textDraft); setPage(1);
    if (roleDraft !== roleCode) await loadMatrix(roleDraft);
  };

  const onRoleChange = async (code: string) => {
    if (code === roleCode) { setRoleDraft(code); return; }
    if (!(await discardOk())) return;
    setRoleDraft(code); setRoleCode(code); setPage(1);
    await loadMatrix(code);
  };

  const toggle = (code: string) => {
    if (locked) return;
    setDraft(d => ({ ...d, [code]: !d[code] }));
  };

  const onSave = async () => {
    if (!role || locked || !dirty) return;
    setSaving(true); setErr(null);
    try {
      const data = await savePermissionMatrix(role.roleCode,
        changed.map(r => ({ permissionCode: r.permissionCode, isAllowed: draft[r.permissionCode] })));
      setRows(data);
      setDraft(Object.fromEntries(data.map(r => [r.permissionCode, r.isAllowed])));
      toast.ok(t("permSaved", "Permissions saved."));
      await perms.refresh();
    } catch (e) {
      setErr(errorText(t, e));
      toast.err(errorText(t, e));
    } finally { setSaving(false); }
  };

  const openNew = async () => { if (await discardOk()) setMode("new"); };
  const openEdit = async () => {
    if (!role) return;
    if (role.isBuiltin) { toast.err(t("permBuiltinLocked", "Built-in: cannot be changed")); return; }
    if (await discardOk()) setMode("edit");
  };

  const onDelete = async () => {
    if (!role) return;
    if (role.isBuiltin) { toast.err(t("permBuiltinLocked", "Built-in: cannot be changed")); return; }
    if (!(await confirm({ message: t("CFM-001", "Delete the selected row(s)?"), detail: `${role.roleCode} - ${role.roleName}` }))) return;
    try {
      await deleteRole(role.roleCode);
      toast.ok(t("toastDeleted", "Deleted"));
      const list = await loadRoles();
      const next = list[0]?.roleCode ?? "";
      setRoleDraft(next); setRoleCode(next);
      await loadMatrix(next);
    } catch (e) {
      toast.err(errorText(t, e));
    }
  };

  const onSaved = async (saved: RoleRow, created: boolean) => {
    setMode(null);
    await loadRoles();
    setRoleDraft(saved.roleCode); setRoleCode(saved.roleCode); setPage(1);
    if (created) await loadMatrix(saved.roleCode);
    await perms.refresh();
  };

  useEffect(() => {
    if (!dirty) return;
    const warn = (e: BeforeUnloadEvent) => { e.preventDefault(); e.returnValue = ""; };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  const count = (dev: "PC" | "HT") => {
    const list = rows.filter(r => r.device === dev);
    return `${list.filter(r => draft[r.permissionCode]).length} / ${list.length}`;
  };

  return (
    <div className="pg on" id="pg-m_permission">
      <div className="search-box" data-search-area="">
        <div className="search-row">
          <label>{t("thPermission", "Permission")}</label>
          <select value={roleDraft} onChange={e => onRoleChange(e.target.value)} style={{ minWidth: 220 }}>
            {roles.map(r => <option key={r.roleCode} value={r.roleCode}>{r.roleCode} - {r.roleName}</option>)}
          </select>
          <label>{t("thDevice", "Device")}</label>
          <select value={deviceDraft} onChange={e => setDeviceDraft(e.target.value)}>
            <option value="">{t("optAll", "All")}</option>
            <option value="PC">PC</option>
            <option value="HT">HT</option>
          </select>
          <label>{t("saFunction", "Function")}</label>
          <input type="text" value={textDraft} style={{ width: 200 }}
            onChange={e => setTextDraft(e.target.value)}
            onKeyDown={e => { if (e.key === "Enter") onSearch(); }} />
        </div>
        <div className="search-btns">
          <button className="btn btn-primary" data-role="search" onClick={onSearch}>
            <img className="icn-img" src={BTN_ICON.search} alt="" /> <span>{t("search", "Search")}</span>
          </button>
          {can("New") && <button className="btn btn-add" onClick={openNew}>
            <PlusIcon /> <span>{t("btnNew", "New")}</span>
          </button>}
          {can("Edit") && <button className="btn btn-edit" onClick={openEdit} disabled={!role}>
            <img className="icn-img" src={BTN_ICON.edit} alt="" /> <span>{t("btnEdit", "Edit")}</span>
          </button>}
          {can("Delete") && <button className="btn btn-delete" onClick={onDelete} disabled={!role}>
            <TrashIcon /> <span>{t("btnDelete", "Delete")}</span>
          </button>}
          {can("Save") && <button className="btn btn-primary" onClick={onSave} disabled={saving || locked || !dirty}>
            {saving ? t("saving", "Saving…") : dirty ? `${t("modalSave", "Save")} (${changed.length})` : t("modalSave", "Save")}
          </button>}
        </div>
      </div>

      {err && <div className="page-info" style={{ color: "#DC3545" }}>{err}</div>}
      {role && (
        <div className="page-info flex flex-wrap gap-2">
          <strong>{role.roleCode}</strong> {role.roleName}
          <span className="text-slate-400">·</span>{t("permUsers", "Users")}: {role.userCount}
          <span className="text-slate-400">·</span>{t("permPcEnabled", "PC enabled")} {count("PC")}
          <span className="text-slate-400">·</span>{t("permHtEnabled", "HT enabled")} {count("HT")}
          {locked
            ? <><span className="text-slate-400">·</span><span style={{ color: "#6C757D" }}>{t("permBuiltinLocked", "Built-in: cannot be changed")}</span></>
            : dirty && <><span className="text-slate-400">·</span><span style={{ color: "#E0A030" }}>{t("permUnsavedChanges", "Unsaved changes")}</span></>}
        </div>
      )}

      <div className="tbl-wrap">
        <table className="tbl" id="tbl-m_permission">
          <thead><tr>
            <SortTh field="device" sort={sort} onSort={onSort}>{t("thDevice", "Device")}</SortTh>
            <SortTh field="tab" sort={sort} onSort={onSort}>{t("thTab", "Tab")}</SortTh>
            <SortTh field="function" sort={sort} onSort={onSort}>{t("saFunction", "Function")}</SortTh>
            <SortTh field="detail" sort={sort} onSort={onSort}>{t("thFunctionDetail", "Function Detail")}</SortTh>
            <SortTh field="enable" sort={sort} onSort={onSort}>{t("permEnableDisable", "Enable / Disable")}</SortTh>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={5} />}
            {!loading && pageRows.length === 0 && (
              <tr className="no-results-row"><td colSpan={5} className="text-center">{t("noResults", "No matching records found.")}</td></tr>
            )}
            {!loading && pageRows.map(r => {
              const on = draft[r.permissionCode] ?? false;
              const pending = on !== r.isAllowed;
              return (
                <tr key={r.permissionCode}>
                  <td>{r.device}</td>
                  <td className="text-left">{tabText(r, t)}</td>
                  <td className="text-left">{functionText(r, t)}</td>
                  <td className="text-left">{detailText(r, t)}</td>
                  <td>
                    <button
                      className={"rounded px-2 py-1 text-xs " + (on ? "bg-emerald-100 text-emerald-800" : "bg-slate-100 text-slate-600") + (pending ? " ring-2 ring-amber-400" : "")}
                      disabled={locked}
                      title={pending ? t("permUnsaved", "Not saved yet") : undefined}
                      onClick={() => toggle(r.permissionCode)}>
                      {on ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      <PageFooter total={sorted.length} page={page} pageSize={pageSize}
        onGoPage={setPage} onChangeSize={s => { setPageSize(s); setPage(1); }} />

      {mode && (
        <RoleModal mode={mode} role={mode === "edit" ? role : null}
          onClose={() => setMode(null)} onSaved={onSaved} />
      )}
    </div>
  );
}

function RoleModal({ mode, role, onClose, onSaved }: {
  mode: "new" | "edit";
  role: RoleRow | null;
  onClose: () => void;
  onSaved: (r: RoleRow, created: boolean) => void;
}) {
  const t = useT();
  const [code, setCode] = useState(role?.roleCode ?? "");
  const [name, setName] = useState(role?.roleName ?? "");
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setSaving(true); setErr(null);
    try {
      const saved = mode === "new"
        ? await createRole({ roleCode: code, roleName: name })
        : await updateRole(role!.roleCode, { roleName: name });
      onSaved(saved, mode === "new");
    } catch (ex) { setErr(errorText(t, ex)); }
    finally { setSaving(false); }
  };

  return (
    <ModalShell
      title={mode === "new" ? t("permNewTitle", "New Permission") : t("permEditTitle", "Edit Permission")}
      onClose={onClose} onSubmit={submit} saving={saving} err={err}>
      <Field label={t("fldPermissionCode", "Permission Code")}>
        <input type="text" maxLength={20} value={code} readOnly={mode === "edit"}
          onChange={e => setCode(e.target.value)} />
      </Field>
      <Field label={t("fldPermissionName", "Permission Name")}>
        <input type="text" maxLength={100} value={name} onChange={e => setName(e.target.value)} />
      </Field>
    </ModalShell>
  );
}
