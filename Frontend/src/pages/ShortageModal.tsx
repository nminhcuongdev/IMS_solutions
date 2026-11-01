import { useEffect, useState } from "react";
import { listReasons, type Reason } from "@/api/reasons";
import { useT } from "@/i18n/LanguageContext";
import { Field, ModalShell } from "@/lib/ModalShell";
import { useFetch } from "@/lib/useFetch";
import { errorText } from "@/lib/errorText";


export function useShortageReasons(): Reason[] {
  const { data } = useFetch("reasons:Shortage:active",
    () => listReasons({ pageSize: 500, category: "Shortage", isActive: true }));
  return data?.data ?? [];
}

export function ShortageReasonCell({ code, reasons }: { code: string | null; reasons: Reason[] }) {
  const t = useT();
  if (!code) return <>—</>;
  const name = reasons.find(r => r.reasonCode === code)?.reasonName ?? code;
  return <><span className="badge badge-warn">{t("badgeShortage", "Shortage")}</span> {name}</>;
}

const RO = { background: "#f7fafc", color: "#4a5568" } as const;

export function ShortageModal({ docLabel, docNo, itemCode, itemName, planned, actual, onSave, onClose, onSaved }: {
  docLabel: string;
  docNo: string;
  itemCode: string;
  itemName: string | null;
  planned: number;
  actual: number;
  onSave: (qty: number, reasonCode: string) => Promise<unknown>;
  onClose: () => void;
  onSaved: () => void;
}) {
  const t = useT();
  const reasons = useShortageReasons();
  const [qty, setQty] = useState(String(Math.max(0, planned - actual)));
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    if (!reason && reasons.length) setReason(reasons[0].reasonCode);
  }, [reasons, reason]);

  return (
    <ModalShell title={t("btnShortage", "Shortage Registration")} onClose={onClose}
      saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault();
        const n = Number(qty);
        if (qty.trim() === "" || !Number.isInteger(n) || n < 0) {
          setErr(t("alertInvalidQty", "Please enter a valid quantity (0 or more).")); return;
        }
        if (!reason) { setErr(t("alertReasonRequired", "Please enter a shortage reason.")); return; }
        setSaving(true); setErr(null);
        try { await onSave(n, reason); onSaved(); }
        catch (ex: any) { setErr(ex?.code ? t(ex.code, ex.message).replace("{0}", docNo) : (errorText(t, ex))); }
        finally { setSaving(false); }
      }}>
      <Field label={docLabel}><input type="text" readOnly value={docNo} style={RO} /></Field>
      <Field label={t("thItemCode", "Item Code")}><input type="text" readOnly value={itemCode} style={RO} /></Field>
      <Field label={t("thItemName", "Item Name")}><input type="text" readOnly value={itemName ?? ""} style={RO} /></Field>
      <Field label={t("thPlannedQty", "Expected/Requested Qty")}>
        <input type="text" readOnly value={String(planned)} style={RO} />
      </Field>
      <Field label={t("thActualQty", "Received/Shipped Qty")}>
        <input type="text" readOnly value={String(actual)} style={RO} />
      </Field>
      <Field label={t("thShortageQty", "Shortage Qty")}>
        <input type="number" value={qty} onChange={e => setQty(e.target.value)} />
      </Field>
      <Field label={t("thShortageReason", "Shortage Reason")}>
        <select value={reason} onChange={e => setReason(e.target.value)}>
          {reasons.map(r => <option key={r.reasonCode} value={r.reasonCode}>{r.reasonName}</option>)}
        </select>
      </Field>
    </ModalShell>
  );
}
