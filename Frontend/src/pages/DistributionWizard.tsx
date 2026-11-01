import { useEffect, useMemo, useRef, useState } from "react";
import {
  createRule, getAttributes, previewTargets, uploadPackage,
  type DistDevice, type DistRule, type InspectionCheck, type PackageUploadResult,
} from "@/api/distribution";
import { useT } from "@/i18n/LanguageContext";
import { useConfirm } from "@/lib/Confirm";
import { errorText } from "@/lib/errorText";
import { localIsoDate } from "@/lib/DateRange";
import { fmtDateTime } from "@/lib/dateFormat";

const CHECK_BADGE: Record<string, string> = { ok: "badge-ok", warn: "badge-warn", ng: "badge-ng", na: "badge-cancel" };
const CHECK_MARK: Record<string, string> = { ok: "✓ OK", warn: "△ WARN", ng: "✕ NG", na: "—" };

export function tzLabel() {
  const o = -new Date().getTimezoneOffset();
  const pad = (n: number) => String(Math.floor(n)).padStart(2, "0");
  return `UTC${o >= 0 ? "+" : "-"}${pad(Math.abs(o) / 60)}:${pad(Math.abs(o) % 60)}`;
}

function localStamp(d: Date) {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${localIsoDate(d)}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function CheckList({ name, values, selected, onChange, label }: {
  name: string; values: string[]; selected: string[]; onChange: (v: string[]) => void; label?: (v: string) => string;
}) {
  const t = useT();
  if (values.length === 0) return <span className="dst-sub">—</span>;
  return (
    <div className="dst-chks">
      {values.map(v => (
        <label key={v} className="dst-chk">
          <input type="checkbox" name={name} checked={selected.includes(v)}
            onChange={e => onChange(e.target.checked ? [...selected, v] : selected.filter(x => x !== v))} />
          {label ? label(v) : v || t("dstUnavailable", "Not available")}
        </label>
      ))}
    </div>
  );
}

export function DistributionWizard({ preselect, onClose, onDone }: {
  preselect?: string[]; onClose: () => void; onDone: (rule: DistRule) => void;
}) {
  const t = useT();
  const ask = useConfirm();
  const fileRef = useRef<HTMLInputElement>(null);
  const [fileLabel, setFileLabel] = useState("");
  const [inspecting, setInspecting] = useState(false);
  const [insp, setInsp] = useState<PackageUploadResult | null>(null);
  const [noteJa, setNoteJa] = useState("");
  const [noteEn, setNoteEn] = useState("");
  const [noteVi, setNoteVi] = useState("");
  const [models, setModels] = useState<string[]>([]);
  const [locations, setLocations] = useState<string[]>([]);
  const [include, setInclude] = useState<string[]>([]);
  const [exclude, setExclude] = useState<string[]>([]);
  const [attrs, setAttrs] = useState<{ models: string[]; locations: string[] }>({ models: [], locations: [] });
  const [allDevices, setAllDevices] = useState<DistDevice[]>([]);
  const [targets, setTargets] = useState<DistDevice[]>([]);
  const [when, setWhen] = useState<"now" | "at">("now");
  const [at, setAt] = useState(() => { const d = new Date(Date.now() + 10 * 60000); d.setSeconds(0, 0); return localStamp(d); });
  const [force, setForce] = useState(false);
  const [minCode, setMinCode] = useState("");
  const [end, setEnd] = useState("");
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    getAttributes().then(setAttrs).catch(ex => setErr(errorText(t, ex)));
    previewTargets({}).then(list => {
      setAllDevices(list);
      if (preselect?.length) setExclude(list.map(d => d.deviceCode).filter(c => !preselect.includes(c)));
    }).catch(() => setAllDevices([]));
  }, [t, preselect]);

  useEffect(() => {
    let alive = true;
    previewTargets({ models, locations, include, exclude })
      .then(r => { if (alive) setTargets(r); }).catch(() => { if (alive) setTargets([]); });
    return () => { alive = false; };
  }, [models, locations, include, exclude]);

  const onFile = async (f: File | undefined) => {
    if (!f) return;
    setFileLabel(`${f.name} (${(f.size / 1048576).toFixed(1)} MB)`);
    setInsp(null); setErr(null);
    if (!/\.apk$/i.test(f.name)) {
      setInsp({ ok: false, checks: [{ id: "health", status: "ng", detail: "apk" }], package: null });
      return;
    }
    setInspecting(true);
    try { setInsp(await uploadPackage(f)); }
    catch (ex) { setErr(errorText(t, ex)); }
    finally { setInspecting(false); }
  };

  const atDate = when === "at" && at ? new Date(at) : null;
  const deliverMs = atDate && atDate.getTime() > Date.now() ? atDate.getTime() : Date.now();
  const endMs = end ? new Date(end).getTime() : null;
  const minVersion = minCode.trim() === "" ? null : Number(minCode);
  const errors = useMemo(() => {
    const list: string[] = [];
    if (!insp?.ok || !insp.package) list.push(t("dstErrNoApk", "Choose a valid APK."));
    if (targets.length === 0) list.push(t("dstZeroTargets", "0 devices — distribution is not possible."));
    if (minVersion !== null && (!Number.isInteger(minVersion) || minVersion < 0))
      list.push(t("dstErrMinCode", "Minimum version must be a versionCode (a whole number)."));
    if (endMs !== null && endMs <= deliverMs) list.push(t("dstErrEnd", "The end date/time must be after the delivery date/time."));
    return list;
  }, [insp, targets.length, minVersion, endMs, deliverMs, t]);

  const submit = async () => {
    if (errors.length || !insp?.package) return;
    const p = insp.package;
    if (!await ask({
      message: t("dstConfirmGo", "Distribute this version?"),
      detail: `${p.versionName} (${p.versionCode}) · ${t("dstTargetCount", "Targets")}: ${targets.length} · ${when === "now" ? t("dstNow", "Now") : fmtDateTime(at)}`,
    })) return;
    setSaving(true); setErr(null);
    try {
      const rule = await createRule({
        packageId: p.id,
        models, locations, include, exclude,
        deliverAt: when === "at" && at ? at : null, endAt: end || null, force,
        minVersionCode: minVersion, noteJa, noteEn, noteVi,
      });
      onDone(rule);
    } catch (ex) { setErr(errorText(t, ex)); }
    finally { setSaving(false); }
  };

  const deviceLabel = (code: string) => {
    const d = allDevices.find(x => x.deviceCode === code);
    return d ? `${d.deviceName} (${d.deviceCode})` : code;
  };
  const checkLabel = (c: InspectionCheck) => t(`dstChk_${c.id}`, c.id);

  return (
    <div className="modal-overlay" onMouseDown={e => { if (e.target === e.currentTarget && !saving) onClose(); }}>
      <div className="modal-box prd-wide">
        <div className="modal-head">
          <span>{t("dstWizTitle", "Data Distribution")}</span>
          <span className="modal-close" onClick={() => !saving && onClose()}>&times;</span>
        </div>
        <div className="modal-body">
          {err && <div className="mfg" style={{ background: "#FFF0F0", color: "#DC3545", padding: "8px 10px", borderRadius: 4, fontSize: 12 }}>{err}</div>}

          <div className="prd-h"><b>1.</b> {t("dstSecFile", "APK file")}</div>
          <div className="mfg">
            <input ref={fileRef} type="file" accept=".apk" style={{ display: "none" }}
              onChange={e => { void onFile(e.target.files?.[0]); e.target.value = ""; }} />
            <button type="button" className="btn btn-outline" disabled={inspecting} onClick={() => fileRef.current?.click()}>
              {t("dstChooseFile", "Choose file…")}
            </button>{" "}
            <span className="dst-sub">{inspecting ? t("loading", "Loading…") : fileLabel}</span>
          </div>
          {!insp && <div className="prd-note">{t("dstInspectHint", "Choose an APK file. It is inspected automatically (package, version, signature, SHA-256).")}</div>}
          {insp && (
            <>
              <div className="tbl-wrap">
                <table className="tbl">
                  <thead><tr>
                    <th>{t("dstChkItem", "Check")}</th><th>{t("dstChkResult", "Result")}</th><th>{t("dstChkDetail", "Detail")}</th>
                  </tr></thead>
                  <tbody>
                    {insp.checks.map(c => (
                      <tr key={c.id}>
                        <td className="text-left">{checkLabel(c)}</td>
                        <td><span className={"badge " + CHECK_BADGE[c.status]}>{CHECK_MARK[c.status]}</span></td>
                        <td className="text-left" style={{ wordBreak: "break-all" }}>{checkDetail(t, c)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <div className={"prd-note" + (insp.ok ? "" : " dst-warn")} style={{ marginTop: 6 }}>
                {insp.ok ? t("dstInspectOk", "All checks passed. The APK is copied to the server storage when you distribute.")
                  : t("dstInspectNg", "At least one check failed. Distribution is disabled.")}
              </div>
            </>
          )}

          <div className="prd-h"><b>2.</b> {t("dstSecNotes", "Update notes (optional)")}</div>
          <div className="mfg"><label>{t("langJa", "Japanese")}</label><textarea rows={2} maxLength={600} style={{ width: "100%" }} value={noteJa} onChange={e => setNoteJa(e.target.value)} /></div>
          <div className="mfg"><label>{t("langEn", "English")}</label><textarea rows={2} maxLength={600} style={{ width: "100%" }} value={noteEn} onChange={e => setNoteEn(e.target.value)} /></div>
          <div className="mfg"><label>{t("langVi", "Vietnamese")}</label><textarea rows={2} maxLength={600} style={{ width: "100%" }} value={noteVi} onChange={e => setNoteVi(e.target.value)} /></div>

          <div className="prd-h"><b>3.</b> {t("dstSecTargets", "Targets (model AND location)")}</div>
          <div className="mfg"><label>{t("dstModels", "Model")}</label><CheckList name="dstModel" values={attrs.models} selected={models} onChange={setModels} /></div>
          <div className="mfg"><label>{t("dstLocations", "Installation location")}</label><CheckList name="dstLoc" values={attrs.locations} selected={locations} onChange={setLocations} /></div>
          <div className="mfg"><label>{t("dstInclude", "Add devices")}</label><CheckList name="dstInc" values={allDevices.map(d => d.deviceCode)} selected={include} onChange={setInclude} label={deviceLabel} /></div>
          <div className="mfg"><label>{t("dstExclude", "Exclude devices")}</label><CheckList name="dstExc" values={allDevices.map(d => d.deviceCode)} selected={exclude} onChange={setExclude} label={deviceLabel} /></div>
          <div className="prd-lotbox">
            <b>{t("dstTargetCount", "Targets")}: {targets.length}</b>
            {targets.length > 0
              ? <div className="dst-sub">{targets.map(d => d.deviceCode).join(", ")}</div>
              : <div className="text-red">{t("dstZeroTargets", "0 devices — distribution is not possible.")}</div>}
          </div>

          <div className="prd-h"><b>4.</b> {t("dstSecWhen", "Delivery date/time")} <span className="dst-sub">({tzLabel()})</span></div>
          <div className="mfg prd-radios">
            <label><input type="radio" name="dstWhen" checked={when === "now"} onChange={() => setWhen("now")} /> {t("dstNow", "Now")}</label>
            <label><input type="radio" name="dstWhen" checked={when === "at"} onChange={() => setWhen("at")} /> {t("dstAtTime", "Specify date/time")}</label>
            <input type="datetime-local" value={at} style={{ width: 220 }} onChange={e => { setAt(e.target.value); setWhen("at"); }} />
          </div>

          <div className="prd-h"><b>5.</b> {t("dstSecOptions", "Options")}</div>
          <div className="mfg prd-radios"><label><input type="checkbox" checked={force} onChange={e => setForce(e.target.checked)} /> {t("dstForce", "Force update (no \"Later\" button)")}</label></div>
          <div className="mfg"><label>{t("dstMinVerCode", "Minimum version (versionCode)")}</label>
            <input type="number" min={0} step={1} style={{ width: 140 }} value={minCode} onChange={e => setMinCode(e.target.value)} /></div>
          <div className="mfg"><label>{t("dstEndDate", "End date/time")}</label>
            <input type="datetime-local" style={{ width: 220 }} value={end} onChange={e => setEnd(e.target.value)} />{" "}
            <span className="dst-sub">{t("dstEndHint", "Empty = continues until replaced")}</span></div>
          <div className="prd-note">{t("dstRollbackNote", "Android does not install a lower versionCode. To go back to an older release, rebuild it with a higher versionCode and register that APK.")}</div>

          <div className="prd-h"><b>6.</b> {t("dstSecConfirm", "Confirm")}</div>
          <div className="prd-note">
            <div>{t("dstSumVersion", "Version")}: <b>{insp?.package ? `${insp.package.versionName} (${insp.package.versionCode})` : "—"}</b></div>
            <div>{t("dstTargetCount", "Targets")}: <b>{targets.length}</b></div>
            <div>{t("dstSecWhen", "Delivery date/time")}: <b>{when === "now" ? t("dstNow", "Now") : fmtDateTime(at)}</b> <span className="dst-sub">({tzLabel()})</span></div>
            <div>{t("dstForce", "Force update (no \"Later\" button)")}: <b>{force || minVersion ? t("dstYes", "Yes") : t("dstNo", "No")}</b></div>
            {errors.length > 0 && <div className="text-red" style={{ marginTop: 4 }}>{errors.map(e => <div key={e}>{e}</div>)}</div>}
          </div>
        </div>
        <div className="modal-actions">
          <button type="button" className="btn btn-cancel" disabled={saving} onClick={onClose}>{t("modalCancel", "Cancel")}</button>
          <button type="button" className="btn btn-special" disabled={saving || errors.length > 0}
            style={errors.length ? { opacity: 0.5 } : undefined} onClick={() => void submit()}>
            {saving ? t("saving", "Saving…") : t("dstBtnGo", "Distribute")}
          </button>
        </div>
      </div>
    </div>
  );
}

function checkDetail(t: (k: string, f: string) => string, c: InspectionCheck): string {
  if (c.status === "na") return "—";
  if (c.id === "health" && c.status === "ng") {
    if (c.detail === "empty") return t("dstDetEmpty", "The file is empty.");
    if (c.detail === "size") return t("dstDetSize", "The file is larger than the upload limit.");
    if (c.detail === "apk") return t("dstExtNg", "Only .apk files can be distributed.");
    return t("dstDetNotApk", "The file could not be read as an APK (ZIP / AndroidManifest).");
  }
  if (c.id === "package" && c.status === "ng") return `${c.detail ?? ""} — ${t("dstDetPkgNg", "not the HT app package")}`;
  if (c.id === "version" && c.status === "warn") return `${c.detail ?? ""} — ${t("dstDetVerWarn", "not newer than the active version; devices will not update")}`;
  if (c.id === "signature" && c.status === "ng")
    return c.detail === "unsigned" ? t("dstDetUnsigned", "No APK Signature Scheme v2/v3 block was found.")
      : `${c.detail ?? ""} — ${t("dstDetSigNg", "Different from the active APK key (INV-4)")}`;
  if (c.id === "signature") return `${c.detail ?? ""} — ${t("dstDetSigOk", "Same signing key as the active APK")}`;
  return c.detail ?? "—";
}
