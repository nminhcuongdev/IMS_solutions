import { useEffect, useMemo, useState } from "react";
import {
  deletePackage, deleteRule, deviceHistory, getDistSettings, getRule, getVersionJson, listAudit, listDistDevices,
  listPackages, listRules, listUnknown, ruleAction, saveDistSettings, updateSourceUrl,
  type DeviceCheck, type DistRule, type DistSettings, type RuleDetail,
} from "@/api/distribution";
import { getCurrentPermissions } from "@/api/permissions";
import { useT } from "@/i18n/LanguageContext";
import { useToast } from "@/lib/Toast";
import { useConfirm } from "@/lib/Confirm";
import { errorText } from "@/lib/errorText";
import { fmtDateTime } from "@/lib/dateFormat";
import { invalidate, useFetch } from "@/lib/useFetch";
import { TableSkeleton } from "@/lib/TableSkeleton";
import { BTN_ICON } from "@/layout/icons";
import { distStatusBadge, distStatusSymbol, ruleStateBadge } from "@/lib/badges";
import { SortTh } from "@/lib/SortableTh";
import { useLocalSort, type SortValue } from "@/lib/useLocalSort";
import { DistributionWizard, tzLabel } from "./DistributionWizard";
import type { DistAudit, DistDevice, DistPackage, UnknownDevice } from "@/api/distribution";

type Tab = "rules" | "devices" | "packages" | "server" | "unknown" | "audit";
const TABS: Tab[] = ["rules", "devices", "packages", "server", "unknown", "audit"];
const STATUS_KEYS: Record<string, [string, string]> = {
  latest: ["dstStLatest", "Up to date"], downloaded: ["dstStDownloaded", "Downloaded"],
  waiting: ["dstStWaiting", "Waiting for update"], attention: ["dstStAttention", "Needs check"],
  unconfirmed: ["dstStUnconfirmed", "Unconfirmed"], notTarget: ["dstStNotTarget", "Not a target"],
};
const COUNT_KEYS = ["latest", "downloaded", "waiting", "attention", "unconfirmed"] as const;

export function useDistributePermission() {
  const { data: perms } = useFetch("currentPermissions", getCurrentPermissions);
  return !!perms && (perms.isSuperAdmin || perms.functions.some(f =>
    f.functionCode === "dist_status" && f.actions.some(a => a.toLowerCase() === "distribute")));
}

export function DistStatusBadge({ status }: { status: string }) {
  const t = useT();
  const [k, f] = STATUS_KEYS[status] ?? [status, status];
  return <span className={"badge " + distStatusBadge(status)}>{distStatusSymbol(status)} {t(k, f)}</span>;
}

export function RuleStateBadge({ state }: { state: string }) {
  const t = useT();
  return <span className={"badge " + ruleStateBadge(state)}>{t(`dstRule_${state.toUpperCase()}`, state)}</span>;
}

const RULE_SORT: Record<string, (r: DistRule) => SortValue> = {
  id: r => r.id, version: r => r.versionCode, condition: r => r.models.join("/") + r.locations.join("/"),
  targets: r => r.summary.targets, deliver: r => r.deliverAt, end: r => r.endAt, state: r => r.state,
  force: r => r.force || !!r.minVersionCode, latest: r => r.summary.latest, downloaded: r => r.summary.downloaded,
  waiting: r => r.summary.waiting, attention: r => r.summary.attention, unconfirmed: r => r.summary.unconfirmed,
  by: r => r.createdBy,
};
const DEVICE_SORT: Record<string, (d: DistDevice) => SortValue> = {
  name: d => d.deviceName, model: d => d.model, location: d => d.locationCode, version: d => d.versionCode,
  lastUpdate: d => d.lastUpdateAt, lastCheck: d => d.lastCheckAt, download: d => d.downloadedAt,
  status: d => d.status, target: d => d.targetVersionCode,
};
const PACKAGE_SORT: Record<string, (p: DistPackage) => SortValue> = {
  id: p => p.id, file: p => p.fileName, pkg: p => p.packageName, version: p => p.versionCode, size: p => p.sizeBytes,
  sha: p => p.sha256, sig: p => p.signerFingerprint, by: p => p.createdBy, at: p => p.createdAt,
};
const UNKNOWN_SORT: Record<string, (u: UnknownDevice) => SortValue> = {
  id: u => u.deviceCode, first: u => u.firstAt, last: u => u.lastAt, count: u => u.requestCount,
};
const AUDIT_SORT: Record<string, (a: DistAudit) => SortValue> = {
  at: a => a.at, who: a => a.who, action: a => a.action, target: a => a.target,
};

const ruleNo = (id: number) => `R-${String(id).padStart(4, "0")}`;
const versionText = (name: string | null, code: number | null) =>
  code == null ? "—" : <>{name ?? code} <span className="dst-sub">({code})</span></>;

export function DistributionPage() {
  const t = useT();
  const toast = useToast();
  const ask = useConfirm();
  const canDistribute = useDistributePermission();
  const [tab, setTab] = useState<Tab>("rules");
  const [selected, setSelected] = useState<number | null>(null);
  const [wizard, setWizard] = useState(false);
  const [detail, setDetail] = useState<RuleDetail | null>(null);
  const [history, setHistory] = useState<{ code: string; rows: DeviceCheck[] } | null>(null);
  const [devFilter, setDevFilter] = useState("all");

  const rules = useFetch("distribution:rules", listRules);
  const devices = useFetch("distribution:devices", () => listDistDevices());
  const ruleRows = rules.data?.data ?? [];
  const deviceRows = devices.data?.data ?? [];
  const selectedRule = ruleRows.find(r => r.id === selected) ?? null;
  const ruleSort = useLocalSort(ruleRows, r => String(r.id), RULE_SORT);

  const refreshAll = async () => {
    invalidate("distribution");
    await Promise.all([rules.refresh(), devices.refresh()]);
  };

  const need = () => {
    if (canDistribute) return true;
    toast.err(t("dstNoPerm", "Only users with the \"Distribute\" permission can do this (view only)."));
    return false;
  };

  const act = async (action: "stop" | "resume" | "retry") => {
    if (!need() || !selectedRule) return;
    try { await ruleAction(selectedRule.id, action); await refreshAll(); toast.ok(t("toastSaved", "Saved")); }
    catch (ex) { toast.err(errorText(t, ex)); }
  };

  const del = async () => {
    if (!need() || !selectedRule) return;
    if (!await ask({ message: t("confirmDelete", "Delete the selected row(s)?"), detail: ruleNo(selectedRule.id) })) return;
    try { await deleteRule(selectedRule.id); setSelected(null); await refreshAll(); toast.ok(t("toastDeleted", "Deleted")); }
    catch (ex) { toast.err(errorText(t, ex)); }
  };

  const openDetail = async (id: number) => {
    try { setDetail(await getRule(id)); } catch (ex) { toast.err(errorText(t, ex)); }
  };
  const openHistory = async (code: string) => {
    try { setHistory({ code, rows: (await deviceHistory(code)).data }); } catch (ex) { toast.err(errorText(t, ex)); }
  };

  const errorRules = ruleRows.filter(r => r.state === "Error");
  const attention = deviceRows.filter(d => d.status === "attention").length;
  const st = selectedRule?.state;
  const rs = { sort: ruleSort.sort, onSort: ruleSort.onSort };

  return (
    <div className="pg on">
      <div className="search-box">
        <div className="search-row">
          <span className="dst-sub">{tzLabel()}</span>
        </div>
        <div className="search-btns">
          <button className="btn btn-primary" onClick={() => void refreshAll()}>
            <img className="icn-img" src={BTN_ICON.search} alt="" /> <span>{t("search", "Search")}</span>
          </button>
          <button className="btn btn-special" onClick={() => need() && setWizard(true)}>
            <img className="icn-img" src="/Images/Icon/upload_50_FFFFFF.svg" alt="" /> <span>{t("dstBtnDistribute", "Data Distribution")}</span>
          </button>
          <button className="btn btn-edit" disabled={tab !== "rules" || st !== "Active"} onClick={() => void act("stop")}>{t("dstBtnStop", "Stop")}</button>
          <button className="btn btn-primary" disabled={tab !== "rules" || st !== "Stopped"} onClick={() => void act("resume")}>{t("dstBtnResume", "Resume")}</button>
          <button className="btn btn-outline" disabled={tab !== "rules" || st !== "Error"} onClick={() => void act("retry")}>{t("dstBtnRetry", "Retry")}</button>
          <button className="btn btn-delete" disabled={tab !== "rules" || !selectedRule} onClick={() => void del()}>{t("btnDelete", "Delete")}</button>
        </div>
      </div>

      {errorRules.length > 0 && <div className="prd-note dst-warn">✕ {t("dstAlertError", "Deployment failed. version.json was kept as before.")} ({errorRules.map(r => ruleNo(r.id)).join(", ")})</div>}
      {attention > 0 && <div className="prd-note dst-warn">× {t("dstAlertAttention", "Devices that downloaded but did not update")}: {attention}</div>}
      {(rules.error || devices.error) && <div className="page-info" style={{ color: "#DC3545" }}>{rules.error ?? devices.error}</div>}

      <div className="prd-tabs">
        {TABS.map(k => (
          <button key={k} className={"prd-tab" + (tab === k ? " on" : "")} onClick={() => setTab(k)}>{t(`dstTab_${k}`, k)}</button>
        ))}
      </div>

      {tab === "rules" && (
        <>
          <div className="tbl-wrap">
            <table className="tbl">
              <thead><tr>
                <th className="chk-col"></th>
                <SortTh field="id" {...rs}>{t("dstColRule", "Rule")}</SortTh>
                <SortTh field="version" {...rs}>{t("dstSumVersion", "Version")}</SortTh>
                <SortTh field="condition" {...rs}>{t("dstColTargets", "Target condition")}</SortTh>
                <SortTh field="targets" {...rs}>{t("dstTargetCount", "Targets")}</SortTh>
                <SortTh field="deliver" {...rs}>{t("dstColDeliver", "Delivery")}</SortTh>
                <SortTh field="end" {...rs}>{t("dstEndDate", "End date")}</SortTh>
                <SortTh field="state" {...rs}>{t("thStatus", "Status")}</SortTh>
                <SortTh field="force" {...rs}>{t("dstForceShort", "Force")}</SortTh>
                {COUNT_KEYS.map(k => <SortTh key={k} field={k} {...rs}>{distStatusSymbol(k)} {t(STATUS_KEYS[k][0], STATUS_KEYS[k][1])}</SortTh>)}
                <SortTh field="by" {...rs}>{t("dstColBy", "Created by")}</SortTh>
              </tr></thead>
              <tbody>
                {rules.loading && <TableSkeleton cols={15} />}
                {!rules.loading && ruleRows.length === 0 && <tr className="no-results-row"><td colSpan={15} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
                {!rules.loading && ruleSort.sorted.map(r => (
                  <tr key={r.id} onClick={() => setSelected(r.id === selected ? null : r.id)} className={r.id === selected ? "is-cursor" : undefined}>
                    <td className="chk-col"><input type="checkbox" className="row-chk" checked={r.id === selected}
                      onChange={() => setSelected(r.id === selected ? null : r.id)} onClick={e => e.stopPropagation()} /></td>
                    <td><a href="#" className="prd-link" onClick={e => { e.preventDefault(); e.stopPropagation(); void openDetail(r.id); }}>{ruleNo(r.id)}</a></td>
                    <td>{versionText(r.versionName, r.versionCode)}</td>
                    <td className="text-left">{conditionText(t, r)}</td>
                    <td className="text-right">{r.summary.targets}</td>
                    <td>{fmtDateTime(r.deliverAt)}</td>
                    <td>{fmtDateTime(r.endAt)}</td>
                    <td><RuleStateBadge state={r.state} /></td>
                    <td>{r.force || r.minVersionCode ? t("dstYes", "Yes") : "—"}</td>
                    {COUNT_KEYS.map(k => <td key={k} className="text-right">{r.state === "Active" ? r.summary[k] : "—"}</td>)}
                    <td>{r.createdBy ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="prd-note" style={{ marginTop: 8 }}>{t("dstRulesNote", "Only one rule per package can be ACTIVE. A new rule that becomes ACTIVE replaces the previous one (SUPERSEDED).")}</div>
        </>
      )}

      {tab === "devices" && (
        <DevicesTab rows={deviceRows.filter(d => devFilter === "all" || d.status === devFilter)} loading={devices.loading}
          filter={devFilter} onFilter={setDevFilter} onHistory={code => void openHistory(code)} />
      )}
      {tab === "packages" && <PackagesTab canDistribute={canDistribute} onChanged={refreshAll} />}
      {tab === "server" && <ServerTab canDistribute={canDistribute} />}
      {tab === "unknown" && <UnknownTab />}
      {tab === "audit" && <AuditTab />}

      {wizard && <DistributionWizard onClose={() => setWizard(false)} onDone={async rule => {
        setWizard(false); await refreshAll(); setSelected(rule.id);
        toast.ok(`${rule.state === "Active" ? t("dstDoneActive", "Distribution started (ACTIVE).") : t("dstDoneScheduled", "Distribution registered (SCHEDULED).")} ${ruleNo(rule.id)}`);
      }} />}
      {detail && <RuleDetailModal detail={detail} onClose={() => setDetail(null)} />}
      {history && (
        <SimpleModal title={`${t("dstHistory", "History")} — ${history.code}`} onClose={() => setHistory(null)}>
          <div className="tbl-wrap"><table className="tbl">
            <thead><tr><th>{t("dstColLastCheck", "Last Check")}</th><th>X-App-Version</th></tr></thead>
            <tbody>
              {history.rows.length === 0 && <tr><td colSpan={2} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
              {history.rows.map((h, i) => <tr key={i}><td>{fmtDateTime(h.receivedAt, true)}</td><td>{h.appVersion}</td></tr>)}
            </tbody>
          </table></div>
        </SimpleModal>
      )}
    </div>
  );
}

function conditionText(t: (k: string, f: string) => string, r: DistRule) {
  return `${r.models.length ? r.models.join("/") : t("dstAllModels", "All models")} · ${r.locations.length ? r.locations.join("/") : t("dstAllLocs", "All locations")}`
    + (r.include.length ? ` +${r.include.length}` : "") + (r.exclude.length ? ` −${r.exclude.length}` : "");
}

function SimpleModal({ title, onClose, children }: { title: string; onClose: () => void; children: React.ReactNode }) {
  const t = useT();
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);
  return (
    <div className="modal-overlay" onMouseDown={e => { if (e.target === e.currentTarget) onClose(); }}>
      <div className="modal-box prd-wide">
        <div className="modal-head"><span>{title}</span><span className="modal-close" onClick={onClose}>&times;</span></div>
        <div className="modal-body">{children}</div>
        <div className="modal-actions"><button className="btn btn-cancel" onClick={onClose}>{t("modalClose", "Close")}</button></div>
      </div>
    </div>
  );
}

function RuleDetailModal({ detail, onClose }: { detail: RuleDetail; onClose: () => void }) {
  const t = useT();
  const r = detail.rule;
  const notes = ([["JA", r.noteJa], ["EN", r.noteEn], ["VI", r.noteVi]] as const).filter(([, v]) => v);
  return (
    <SimpleModal title={`${t("dstDetailTitle", "Distribution rule")} ${ruleNo(r.id)}`} onClose={onClose}>
      <table className="tbl prd-kv"><tbody>
        <tr><th>{t("dstColRule", "Rule")}</th><td>{ruleNo(r.id)}</td><th>{t("thStatus", "Status")}</th>
          <td><RuleStateBadge state={r.state} />
            {r.error && <span className="text-red"> {t(`dstErr_${r.error}`, r.error)}</span>}
            {r.stopReason && <span className="dst-sub"> ({t(`dstStop_${r.stopReason}`, r.stopReason)})</span>}</td></tr>
        <tr><th>{t("dstSumVersion", "Version")}</th><td>{r.versionName} ({r.versionCode})</td><th>{t("dstColFile", "File")}</th><td>{r.apkFile}</td></tr>
        <tr><th>{t("dstColDeliver", "Delivery")}</th><td>{fmtDateTime(r.deliverAt)}</td><th>{t("dstEndDate", "End date")}</th><td>{fmtDateTime(r.endAt)}</td></tr>
        <tr><th>{t("dstForce", "Force update (no \"Later\" button)")}</th><td>{r.force ? t("dstYes", "Yes") : t("dstNo", "No")}</td>
          <th>{t("dstMinVerCode", "Minimum version (versionCode)")}</th><td>{r.minVersionCode ?? "—"}</td></tr>
        <tr><th>SHA-256</th><td colSpan={3} style={{ wordBreak: "break-all" }}>{r.sha256}</td></tr>
      </tbody></table>
      {notes.length > 0 && <>
        <div className="prd-h">{t("dstSecNotes", "Update notes (optional)")}</div>
        {notes.map(([l, v]) => <div key={l}><b>{l}</b> {v}</div>)}
      </>}
      <div className="prd-h">{t("dstTargetCount", "Targets")} ({detail.targets.length})</div>
      <div className="tbl-wrap"><table className="tbl">
        <thead><tr><th>{t("thDeviceName", "Device Name")}</th><th>{t("thModel", "Model")}</th><th>{t("thInstallLocation", "Installation Location")}</th>
          <th>{t("dstColVersion", "Current Version")}</th><th>{t("dstColStatus", "Update Status")}</th></tr></thead>
        <tbody>{detail.targets.map(d => (
          <tr key={d.deviceCode}><td className="text-left" title={d.deviceCode}>{d.deviceName}</td><td className="text-left">{d.model ?? "—"}</td>
            <td className="text-left">{d.locationCode ?? "—"}</td><td>{versionText(d.versionName, d.versionCode)}</td>
            <td>{r.state === "Active" ? <DistStatusBadge status={d.status} /> : "—"}</td></tr>
        ))}</tbody>
      </table></div>
      {r.deploySteps.length > 0 && <>
        <div className="prd-h">{t("dstDeploySteps", "Deployment steps (spec 7.3)")}</div>
        <ul className="dst-steps">{r.deploySteps.map(s => <li key={s}>✓ {t(`dstStep_${s}`, s)}</li>)}
          {r.error && <li>✕ {t(`dstErr_${r.error}`, r.error)}</li>}</ul>
      </>}
      <div className="prd-h">version.json ({ruleNo(r.id)})</div>
      <pre className="dst-json">{JSON.stringify(detail.versionJson, null, 2)}</pre>
    </SimpleModal>
  );
}

function DevicesTab({ rows, loading, filter, onFilter, onHistory }: {
  rows: DistDevice[]; loading: boolean; filter: string;
  onFilter: (v: string) => void; onHistory: (code: string) => void;
}) {
  const t = useT();
  const { data: settings } = useFetch("distribution:settings", getDistSettings);
  const { sorted, sort, onSort } = useLocalSort(rows, d => d.deviceCode, DEVICE_SORT);
  const ss = { sort, onSort };
  return (
    <>
      <div className="search-row" style={{ marginBottom: 8 }}>
        <label>{t("thStatus", "Status")}</label>
        <select value={filter} onChange={e => onFilter(e.target.value)}>
          <option value="all">{t("warehouseAll", "All")}</option>
          {Object.entries(STATUS_KEYS).map(([k, [key, f]]) => <option key={k} value={k}>{distStatusSymbol(k)} {t(key, f)}</option>)}
        </select>
      </div>
      <div className="tbl-wrap"><table className="tbl">
        <thead><tr>
          <SortTh field="name" {...ss}>{t("thDeviceName", "Device Name")}</SortTh>
          <SortTh field="model" {...ss}>{t("thModel", "Model")}</SortTh>
          <SortTh field="location" {...ss}>{t("thInstallLocation", "Installation Location")}</SortTh>
          <SortTh field="version" {...ss}>{t("dstColVersion", "Current Version")}</SortTh>
          <SortTh field="lastUpdate" {...ss}>{t("dstColLastUpdate", "Last Updated")}</SortTh>
          <SortTh field="lastCheck" {...ss}>{t("dstColLastCheck", "Last Check")}</SortTh>
          <SortTh field="download" {...ss}>{t("dstColDownload", "Download done")}</SortTh>
          <SortTh field="status" {...ss}>{t("dstColStatus", "Update Status")}</SortTh>
          <SortTh field="target" {...ss}>{t("dstColTarget", "Target Version")}</SortTh><th></th>
        </tr></thead>
        <tbody>
          {loading && <TableSkeleton cols={10} />}
          {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={10} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
          {!loading && sorted.map(d => (
            <tr key={d.deviceCode}>
              <td className="text-left" title={d.deviceCode}>{d.deviceName}</td><td className="text-left">{d.model ?? "—"}</td>
              <td className="text-left">{d.locationCode ?? "—"}</td><td>{versionText(d.versionName, d.versionCode)}</td>
              <td>{fmtDateTime(d.lastUpdateAt)}</td><td>{fmtDateTime(d.lastCheckAt)}</td><td>{fmtDateTime(d.downloadedAt)}</td>
              <td><DistStatusBadge status={d.status} /></td><td>{d.targetVersionName ?? "—"}</td>
              <td><a href="#" className="prd-link" onClick={e => { e.preventDefault(); onHistory(d.deviceCode); }}>{t("dstHistory", "History")}</a></td>
            </tr>
          ))}
        </tbody>
      </table></div>
      <div className="prd-note" style={{ marginTop: 8 }}>
        N = {settings?.nDays ?? "—"} / M = {settings?.mDays ?? "—"} — {t("dstStatusNote", "Status follows spec 6.3. Failure reasons are not collected (the HT does not report).")}
      </div>
    </>
  );
}

function PackagesTab({ canDistribute, onChanged }: { canDistribute: boolean; onChanged: () => Promise<void> }) {
  const t = useT();
  const toast = useToast();
  const ask = useConfirm();
  const { data, loading, reload } = useFetch("distribution:packages", listPackages);
  const { data: settings } = useFetch("distribution:settings", getDistSettings);
  const rows = data?.data ?? [];
  const { sorted, sort, onSort } = useLocalSort(rows, p => String(p.id), PACKAGE_SORT);
  const ss = { sort, onSort };
  const remove = async (id: number, name: string) => {
    if (!await ask({ message: t("confirmDelete", "Delete the selected row(s)?"), detail: name })) return;
    try { await deletePackage(id); invalidate("distribution"); await reload(); await onChanged(); toast.ok(t("toastDeleted", "Deleted")); }
    catch (ex) { toast.err(errorText(t, ex)); }
  };
  return (
    <>
      <div className="tbl-wrap"><table className="tbl">
        <thead><tr>
          <SortTh field="id" {...ss}>{t("dstColId", "ID")}</SortTh>
          <SortTh field="file" {...ss}>{t("dstColFile", "File")}</SortTh>
          <SortTh field="pkg" {...ss}>{t("dstChk_package", "Package name")}</SortTh>
          <SortTh field="version" {...ss}>{t("dstSumVersion", "Version")}</SortTh>
          <SortTh field="size" {...ss}>{t("dstColSize", "Size")}</SortTh>
          <SortTh field="sha" {...ss}>SHA-256</SortTh>
          <SortTh field="sig" {...ss}>{t("dstChk_signature", "Signature")}</SortTh>
          <SortTh field="by" {...ss}>{t("dstColBy", "Created by")}</SortTh>
          <SortTh field="at" {...ss}>{t("prdDateTime", "Date/Time")}</SortTh><th></th>
        </tr></thead>
        <tbody>
          {loading && <TableSkeleton cols={10} />}
          {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={10} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
          {!loading && sorted.map(p => (
            <tr key={p.id}>
              <td>P-{String(p.id).padStart(4, "0")}</td>
              <td className="text-left" title={p.originalFileName}>{p.fileName}</td>
              <td className="text-left">{p.packageName}</td>
              <td>{versionText(p.versionName, p.versionCode)}</td>
              <td className="text-right">{(p.sizeBytes / 1048576).toFixed(1)} MB</td>
              <td className="text-left" title={p.sha256}>{p.sha256.slice(0, 12)}…{p.sha256.slice(-6)}</td>
              <td className="text-left" title={p.signerFingerprint}>{p.signerFingerprint.slice(0, 23)}…</td>
              <td>{p.createdBy ?? "—"}</td><td>{fmtDateTime(p.createdAt)}</td>
              <td><button className="btn btn-delete" disabled={p.inUse || !canDistribute}
                style={p.inUse || !canDistribute ? { opacity: 0.5 } : undefined}
                onClick={() => void remove(p.id, p.fileName)}>{t("btnDelete", "Delete")}</button></td>
            </tr>
          ))}
        </tbody>
      </table></div>
      <div className="prd-note" style={{ marginTop: 8 }}>{t("dstRetention", "Old versions kept")}: {settings?.retention ?? "—"}</div>
    </>
  );
}

function ServerTab({ canDistribute }: { canDistribute: boolean }) {
  const t = useT();
  const toast = useToast();
  const { data, reload } = useFetch("distribution:settings", getDistSettings);
  const { data: vj } = useFetch("distribution:version-json", getVersionJson);
  const [form, setForm] = useState<DistSettings | null>(null);
  useEffect(() => { if (data) setForm(data); }, [data]);
  const url = useMemo(() => updateSourceUrl(), []);
  if (!form) return <div className="prd-empty">{t("loading", "Loading…")}</div>;
  const num = (k: keyof DistSettings, label: string, fb: string) => (
    <div className="mfg"><label>{t(label, fb)}</label>
      <input type="number" min={1} style={{ width: 100 }} value={form[k] as number} disabled={!canDistribute}
        onChange={e => setForm(f => f && { ...f, [k]: Number(e.target.value) })} /></div>
  );
  const save = async () => {
    if ([form.uploadMb, form.retention, form.nDays, form.mDays].some(n => !Number.isInteger(n) || n < 1))
      return toast.err(t("dstErrNum", "Enter valid numbers (N, M and the upload limit must be 1 or more)."));
    try { setForm(await saveDistSettings(form)); invalidate("distribution"); await reload(); toast.ok(t("dstSaved", "Server settings saved.")); }
    catch (ex) { toast.err(errorText(t, ex)); }
  };
  return (
    <div className="dst-form">
      <div className="mfg"><label>{t("dstSv_mode", "Delivery mode")}</label><b>A-1 {t("dstModeA1", "Built-in HTTP(S) server (standard)")}</b></div>
      <div className="prd-note" style={{ marginBottom: 10 }}>{t("dstNoteA1", "The service answers API-D1 (version.json) and API-D2 (APK, Range). Device versions and check times are recorded. Targets are filtered by the server.")}</div>
      <div className="mfg"><label>{t("dstSv_packageName", "HT app package name")}</label>
        <input style={{ width: 300 }} maxLength={200} value={form.packageName} disabled={!canDistribute}
          onChange={e => setForm(f => f && { ...f, packageName: e.target.value })} /></div>
      {num("retention", "dstSv_retention", "Old versions kept")}
      {num("uploadMb", "dstSv_uploadMb", "Upload limit (MB)")}
      {num("nDays", "dstSv_n", "N days (Unconfirmed)")}
      {num("mDays", "dstSv_m", "M days (Needs check)")}
      <div className="mfg prd-radios"><label><input type="checkbox" checked={form.autoAdd} disabled={!canDistribute}
        onChange={e => setForm(f => f && { ...f, autoAdd: e.target.checked })} /> {t("dstSv_autoAdd", "New devices that match a rule become targets automatically")}</label></div>
      <div className="prd-note" style={{ margin: "8px 0" }}>
        {t("dstSourceUrl", "Update source for the HT initial settings")}: <b>{url}</b>
        {url.startsWith("http:") && <><br /><span className="text-red">⚠ {t("dstAlertPlain", "Plain HTTP/FTP is used. Use HTTPS/FTPS where possible.")}</span></>}
      </div>
      {canDistribute && <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
        <button className="btn btn-primary" onClick={() => void save()}>{t("modalSave", "Save")}</button>
      </div>}
      <div className="prd-h">{t("dstVjTitle", "version.json (current)")}</div>
      <pre className="dst-json">{JSON.stringify(vj ?? { apps: [] }, null, 2)}</pre>
    </div>
  );
}

function UnknownTab() {
  const t = useT();
  const { data, loading } = useFetch("distribution:unknown", listUnknown);
  const rows = data?.data ?? [];
  const { sorted, sort, onSort } = useLocalSort(rows, u => u.deviceCode, UNKNOWN_SORT);
  const ss = { sort, onSort };
  return (
    <>
      <div className="tbl-wrap"><table className="tbl">
        <thead><tr><SortTh field="id" {...ss}>{t("dstColDeviceId", "Device ID")}</SortTh><SortTh field="first" {...ss}>{t("dstColFirst", "First request")}</SortTh><SortTh field="last" {...ss}>{t("dstColLast", "Last request")}</SortTh><SortTh field="count" {...ss}>{t("dstColCount", "Count")}</SortTh></tr></thead>
        <tbody>
          {loading && <TableSkeleton cols={4} />}
          {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={4} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
          {!loading && sorted.map(u => (
            <tr key={u.deviceCode}><td className="text-left">{u.deviceCode}</td><td>{fmtDateTime(u.firstAt)}</td><td>{fmtDateTime(u.lastAt)}</td><td className="text-right">{u.requestCount}</td></tr>
          ))}
        </tbody>
      </table></div>
      <div className="prd-note" style={{ marginTop: 8 }}>{t("dstUnknownNote", "Requests with an unregistered device ID get {\"apps\":[]} and are listed here.")}</div>
    </>
  );
}

function AuditTab() {
  const t = useT();
  const { data, loading } = useFetch("distribution:audit", listAudit);
  const rows = data?.data ?? [];
  const { sorted, sort, onSort } = useLocalSort(rows, a => a.at + a.action + (a.target ?? ""), AUDIT_SORT);
  const ss = { sort, onSort };
  return (
    <div className="tbl-wrap"><table className="tbl">
      <thead><tr><SortTh field="at" {...ss}>{t("prdDateTime", "Date/Time")}</SortTh><SortTh field="who" {...ss}>{t("dstColBy", "Who")}</SortTh><SortTh field="action" {...ss}>{t("dstColAction", "Action")}</SortTh><SortTh field="target" {...ss}>{t("prdTarget", "Target")}</SortTh></tr></thead>
      <tbody>
        {loading && <TableSkeleton cols={4} />}
        {!loading && rows.length === 0 && <tr className="no-results-row"><td colSpan={4} className="text-center">{t("noResults", "No matching records found.")}</td></tr>}
        {!loading && sorted.map((a, i) => (
          <tr key={i}><td>{fmtDateTime(a.at, true)}</td><td className="text-left">{a.who}</td>
            <td className="text-left">{t(`dstAudit_${a.action}`, a.action)}</td><td className="text-left">{a.target ?? "—"}</td></tr>
        ))}
      </tbody>
    </table></div>
  );
}
