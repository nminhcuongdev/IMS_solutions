import { useCallback, useMemo, useState } from "react";
import {
  createDevice, deleteDevice, DEVICE_TYPES, exportDevicesCsv, listDevices, updateDevice,
  type Device, type DeviceWrite,
} from "@/api/devices";
import { listLocations } from "@/api/locations";
import { CommonToolbar } from "@/lib/CommonToolbar";
import { PageFooter } from "@/lib/PageFooter";
import { PAGE_SIZES } from "@/lib/Pager";
import { Field, ModalShell } from "@/lib/ModalShell";
import { SortTh, useSort } from "@/lib/SortableTh";
import { codeName, RefSelect, useRefOptions } from "@/lib/RefSelect";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { useUrlNumber, useUrlSearch, useUrlPatch } from "@/lib/useUrlState";
import { TableSkeleton, useRowFlash } from "@/lib/TableSkeleton";
import { invalidate, qs, useFetch } from "@/lib/useFetch";
import { useRowNav } from "@/lib/useShortcuts";
import { useRowSelect } from "@/lib/rowSelect";
import { statusBadge } from "@/lib/badges";
import { errorText } from "@/lib/errorText";
import { listDistDevices, type DistDevice } from "@/api/distribution";
import { fmtDateTime } from "@/lib/dateFormat";
import { DistStatusBadge, useDistributePermission } from "./DistributionPage";
import { DistributionWizard } from "./DistributionWizard";

type ModalMode = "new" | "edit" | "copy" | null;

const typeKey = (type: string) => `deviceType${type}`;
const UPDATE_FILTERS: [string, string, string][] = [
  ["latest", "dstStLatest", "Up to date"], ["downloaded", "dstStDownloaded", "Downloaded"],
  ["waiting", "dstStWaiting", "Waiting for update"], ["attention", "dstStAttention", "Needs check"],
  ["unconfirmed", "dstStUnconfirmed", "Unconfirmed"],
];
const HANDHELD = "Handheld";

export function DevicePage() {
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
  const [target, setTarget] = useState<Device | null>(null);

  const [updateStatus, setUpdateStatus] = useState("all");
  const [wizard, setWizard] = useState<string[] | null>(null);
  const canDistribute = useDistributePermission();
  const { sort, toggleSort } = useSort("device_code");
  const onSort = toggleSort;

  const { data, loading, error: err, reload } = useFetch(
    qs("devices", { page, pageSize, q, sort: sort.value, updateStatus }),
    () => listDevices({ page, pageSize, q, sort: sort.value, updateStatus: updateStatus === "all" ? undefined : updateStatus }));
  const dist = useFetch("distribution:devices", () => listDistDevices());
  const distByCode = useMemo(() => new Map((dist.data?.data ?? []).map(d => [d.deviceCode, d] as [string, DistDevice])), [dist.data]);
  const rows = data?.data ?? [];
  const total = data?.meta.total ?? 0;

  const load = useCallback(async () => { invalidate("devices"); invalidate("distribution"); await reload(); dist.refresh(); }, [reload, dist]);
  const openWizard = () => {
    if (!canDistribute) return toast.err(t("dstNoPerm", "Only users with the \"Distribute\" permission can do this (view only)."));
    setWizard(rows.filter(r => selected.has(r.deviceCode) && r.deviceType === HANDHELD).map(r => r.deviceCode));
  };

  const selectedRow = useMemo(
    () => selected.size === 1 ? rows.find(r => r.deviceCode === [...selected][0]) ?? null : null,
    [selected, rows]);
  const toggle = (c: string) => setSelected(p => { const s = new Set(p); s.has(c) ? s.delete(c) : s.add(c); return s; });
  const toggleAll = () => setSelected(p => p.size === rows.length ? new Set() : new Set(rows.map(r => r.deviceCode)));

  const { keys: rowNav, cursorCode, setCursorTo } = useRowNav({
    codes: rows.map(r => r.deviceCode), setSelected,
  });
  const rowProps = useRowSelect(toggle, setCursorTo);

  return (
    <div className="pg on">
      <CommonToolbar
        searchDraft={qDraft} onSearchDraftChange={setQDraft}
        onSearch={() => { setPage(1); dist.refresh(); }}
        extra={
          <select style={{ width: 190 }} value={updateStatus} onChange={e => { setUpdateStatus(e.target.value); setPage(1); }}>
            <option value="all">{t("dstFilterAll", "Update status: All")}</option>
            {UPDATE_FILTERS.map(([v, k, f]) => <option key={v} value={v}>{t(k, f)}</option>)}
          </select>
        }
        labelButton={
          <button className="btn btn-special" onClick={openWizard}>
            <img className="icn-img" src="/Images/Icon/upload_50_FFFFFF.svg" alt="" /> <span>{t("dstBtnDistribute", "Data Distribution")}</span>
          </button>
        }
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
            for (const c of selected) await deleteDevice(c);
            setSelected(new Set()); await load();
            toast.ok(`${t("toastDeleted", "Deleted")}: ${n}`);
          } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        onExport={async () => {
          try {
            const { blob, filename } = await exportDevicesCsv();
            const u = URL.createObjectURL(blob); const a = document.createElement("a"); a.href = u; a.download = filename; a.click(); URL.revokeObjectURL(u);
          } catch (ex: any) { toast.err(errorText(t, ex)); }
        }}
        disableEdit={!selectedRow} disableCopy={!selectedRow} disableDelete={selected.size === 0}
        extraKeys={rowNav}
      />
      {err && <div className="page-info" style={{ color: "#DC3545" }}>{err}</div>}
      <div className="tbl-wrap">
        <table className="tbl">
          <thead><tr>
            <th className="chk-col">
              <input type="checkbox" className="row-chk"
                checked={selected.size > 0 && selected.size === rows.length}
                ref={el => { if (el) el.indeterminate = selected.size > 0 && selected.size < rows.length; }}
                onChange={toggleAll} />
            </th>
            <SortTh field="device_name" sort={sort} onSort={onSort}>{t("thDeviceName", "Device Name")}</SortTh>
            <SortTh field="manufacturer" sort={sort} onSort={onSort}>{t("thManufacturer", "Manufacturer")}</SortTh>
            <SortTh field="model" sort={sort} onSort={onSort}>{t("thModel", "Model")}</SortTh>
            <SortTh field="device_type" sort={sort} onSort={onSort}>{t("thDeviceType", "Device Type")}</SortTh>
            <SortTh field="serial_no" sort={sort} onSort={onSort}>{t("thSerialNumber", "Serial Number")}</SortTh>
            <SortTh field="ip_address" sort={sort} onSort={onSort}>{t("thRfidGateIpAddress", "IP Address")}</SortTh>
            <SortTh field="location_code" sort={sort} onSort={onSort}>{t("thInstallLocation", "Installation Location")}</SortTh>
            <SortTh field="is_active" sort={sort} onSort={onSort}>{t("thStatus", "Status")}</SortTh>
            <SortTh field="remark_1" sort={sort} onSort={onSort}>{t("thRemark1", "Remark 1")}</SortTh>
            <SortTh field="remark_2" sort={sort} onSort={onSort}>{t("thRemark2", "Remark 2")}</SortTh>
            <SortTh field="remark_3" sort={sort} onSort={onSort}>{t("thRemark3", "Remark 3")}</SortTh>
            <SortTh field="remark_4" sort={sort} onSort={onSort}>{t("thRemark4", "Remark 4")}</SortTh>
            <SortTh field="remark_5" sort={sort} onSort={onSort}>{t("thRemark5", "Remark 5")}</SortTh>
            <SortTh field="app_version" sort={sort} onSort={onSort} className="dst-col">{t("dstColVersion", "Current Version")}</SortTh>
            <SortTh field="last_update_at" sort={sort} onSort={onSort} className="dst-col">{t("dstColLastUpdate", "Last Updated")}</SortTh>
            <SortTh field="last_check_at" sort={sort} onSort={onSort} className="dst-col">{t("dstColLastCheck", "Last Check")}</SortTh>
            <th className="dst-col">{t("dstColStatus", "Update Status")}</th>
            <th className="dst-col">{t("dstColTarget", "Target Version")}</th>
          </tr></thead>
          <tbody>
            {loading && <TableSkeleton cols={19} />}
            {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={19} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
            {!loading && rows.map(r => (
              <tr key={r.deviceCode} data-row-code={r.deviceCode} {...rowProps(r.deviceCode)} className={[flashCls(r.deviceCode), r.deviceCode === cursorCode ? "is-cursor" : ""].filter(Boolean).join(" ") || undefined}>
                <td className="chk-col"><input type="checkbox" className="row-chk"
                  checked={selected.has(r.deviceCode)} onChange={() => toggle(r.deviceCode)} /></td>
                <td className="text-left" title={r.deviceCode}>{r.deviceName}</td>
                <td className="text-left">{r.manufacturer ?? "—"}</td>
                <td className="text-left">{r.model ?? "—"}</td>
                <td className="text-left">{t(typeKey(r.deviceType), r.deviceType)}</td>
                <td>{r.serialNo ?? "—"}</td>
                <td>{r.ipAddress ?? "—"}</td>
                <td className="text-left">{r.locationCode ?? "—"}</td>
                <td className="text-left"><span className={"badge " + statusBadge(r.isActive)}>{r.isActive ? t("statusEnable", "Enable") : t("statusDisable", "Disable")}</span></td>
                <td className="text-left">{r.remark1 || "—"}</td>
                <td className="text-left">{r.remark2 || "—"}</td>
                <td className="text-left">{r.remark3 || "—"}</td>
                <td className="text-left">{r.remark4 || "—"}</td>
                <td className="text-left">{r.remark5 || "—"}</td>
                <DistCells device={r.deviceType === HANDHELD ? distByCode.get(r.deviceCode) : undefined} />
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <PageFooter total={total} page={page} pageSize={pageSize} onGoPage={p => setPage(p, { push: true })} onChangeSize={n => patch({ size: n === 50 ? null : String(n), page: null })} />
      {modal && <DeviceModal mode={modal} source={target}
        onClose={() => setModal(null)} onSaved={async (savedCode) => {
          setModal(null); setSelected(new Set()); await load();
          flash(savedCode);
          toast.ok(t("toastSaved", "Saved"));
        }} />}
      {wizard && <DistributionWizard preselect={wizard} onClose={() => setWizard(null)} onDone={async rule => {
        setWizard(null); invalidate("distribution"); dist.refresh();
        toast.ok(`${rule.state === "Active" ? t("dstDoneActive", "Distribution started (ACTIVE).") : t("dstDoneScheduled", "Distribution registered (SCHEDULED).")} R-${String(rule.id).padStart(4, "0")}`);
      }} />}
    </div>
  );
}

function DistCells({ device }: { device: DistDevice | undefined }) {
  if (!device) return <>{[0, 1, 2, 3, 4].map(i => <td key={i} className="dst-col">—</td>)}</>;
  const target = device.status !== "notTarget" && device.targetVersionCode != null;
  return (
    <>
      <td className="dst-col">{device.versionCode != null
        ? <>{device.versionName ?? device.versionCode} <span className="dst-sub">({device.versionCode})</span></> : "—"}</td>
      <td className="dst-col">{fmtDateTime(device.lastUpdateAt)}</td>
      <td className="dst-col">{fmtDateTime(device.lastCheckAt)}</td>
      <td className="dst-col"><DistStatusBadge status={device.status} /></td>
      <td className="dst-col">{target ? <>{device.targetVersionName}{device.versionCode != null && (device.versionCode >= device.targetVersionCode!
        ? <span className="dst-ok"> ✓</span> : <span className="dst-up"> ▲</span>)}</> : "—"}</td>
    </>
  );
}

function DeviceModal({ mode, source, onClose, onSaved }: {
  mode: "new" | "edit" | "copy"; source: Device | null;
  onClose: () => void; onSaved: (savedCode: string) => void;
}) {
  const t = useT();
  const isEdit = mode === "edit";
  const [form, setForm] = useState<DeviceWrite>({
    deviceCode: isEdit ? source!.deviceCode : "",
    deviceName: mode === "new" ? "" : source?.deviceName ?? "",
    deviceType: source?.deviceType ?? "Printer",
    model: source?.model ?? "", manufacturer: source?.manufacturer ?? "",
    spec: source?.spec ?? "", serialNo: source?.serialNo ?? "",
    ipAddress: source?.ipAddress ?? "", locationCode: source?.locationCode ?? "",
    isActive: source?.isActive ?? true,
    remark1: source?.remark1 ?? "", remark2: source?.remark2 ?? "",
    remark3: source?.remark3 ?? "", remark4: source?.remark4 ?? "",
    remark5: source?.remark5 ?? "",
  });
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const locations = useRefOptions(
    () => listLocations({ pageSize: 500 }),
    l => ({ value: l.locationCode, label: codeName(l.locationCode, l.locationName) }));

  const text = (k: keyof DeviceWrite, max: number) => (
    <input maxLength={max} value={(form[k] ?? "") as string}
      onChange={e => setForm(f => ({ ...f, [k]: e.target.value }))} />
  );

  return (
    <ModalShell title={t("mDevice", "Device Master")} onClose={onClose} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault(); setSaving(true); setErr(null);
        try {
          if (isEdit) await updateDevice(source!.deviceCode, form); else await createDevice(form);
          onSaved(form.deviceCode.trim());
        }
        catch (ex: any) { setErr(errorText(t, ex)); } finally { setSaving(false); }
      }}>
      <Field label={t("thDeviceCode", "Device Code")}>
        <input maxLength={20} required value={form.deviceCode} readOnly={isEdit}
          onChange={e => setForm(f => ({ ...f, deviceCode: e.target.value }))}
          style={isEdit ? { background: "#f7fafc", color: "#718096" } : undefined} />
      </Field>
      <Field label={t("thDeviceName", "Device Name")}>
        <input maxLength={100} required value={form.deviceName}
          onChange={e => setForm(f => ({ ...f, deviceName: e.target.value }))} />
      </Field>
      <Field label={t("thManufacturer", "Manufacturer")}>{text("manufacturer", 50)}</Field>
      <Field label={t("thModel", "Model")}>{text("model", 50)}</Field>
      <Field label={t("thDeviceType", "Device Type")}>
        <select value={form.deviceType} onChange={e => setForm(f => ({ ...f, deviceType: e.target.value }))}>
          {DEVICE_TYPES.map(d => <option key={d} value={d}>{t(typeKey(d), d)}</option>)}
        </select>
      </Field>
      <Field label={t("thSerialNumber", "Serial Number")}>{text("serialNo", 100)}</Field>
      <Field label={t("thRfidGateIpAddress", "IP Address")}>
        <input maxLength={15} inputMode="decimal" value={form.ipAddress ?? ""}
          onChange={e => setForm(f => ({ ...f, ipAddress: e.target.value.replace(/[^0-9.]/g, "") }))} />
      </Field>
      <Field label={t("thInstallLocation", "Installation Location")}>
        <RefSelect value={form.locationCode ?? ""} options={locations}
          onChange={v => setForm(f => ({ ...f, locationCode: v || null }))} />
      </Field>
      <Field label={t("thStatus", "Status")}>
        <select value={form.isActive ? "1" : "0"} onChange={e => setForm(f => ({ ...f, isActive: e.target.value === "1" }))}>
          <option value="1">{t("statusEnable", "Enable")}</option><option value="0">{t("statusDisable", "Disable")}</option>
        </select>
      </Field>
      {(["remark1", "remark2", "remark3", "remark4", "remark5"] as const).map((k, i) => (
        <Field key={k} label={t(`thRemark${i + 1}`, `Remark ${i + 1}`)}>{text(k, 200)}</Field>
      ))}
    </ModalShell>
  );
}
