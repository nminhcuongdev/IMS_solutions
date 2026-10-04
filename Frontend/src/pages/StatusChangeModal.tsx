import { useState } from "react";
import { useT } from "@/i18n/LanguageContext";
import { useConfirm } from "@/lib/Confirm";
import { Field, ModalShell } from "@/lib/ModalShell";

export type StatusChangeAction = "done" | "notDone" | "cancelled";

export interface StatusChangeOutcome {
  updated: number;
  skipped: string[];
}

interface StatusLabel {
  key: string;
  fallback: string;
}

const RO = { width: "auto", background: "#f7fafc", color: "#4a5568" } as const;
const OPTION = {
  display: "flex", alignItems: "center", gap: 8, fontSize: 13, fontWeight: 400,
  color: "#2d3748", cursor: "pointer",
} as const;

export function StatusChangeModal({ count, done, notDone, onApply, onClose, onDone }: {
  count: number;
  done: StatusLabel;
  notDone: StatusLabel;
  onApply: (action: StatusChangeAction) => Promise<StatusChangeOutcome>;
  onClose: () => void;
  onDone: (outcome: StatusChangeOutcome) => void;
}) {
  const t = useT();
  const ask = useConfirm();
  const [action, setAction] = useState<StatusChangeAction | null>(null);
  const [saving, setSaving] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  const options: { value: StatusChangeAction; label: string }[] = [
    { value: "done", label: t(done.key, done.fallback) },
    { value: "notDone", label: t(notDone.key, notDone.fallback) },
    { value: "cancelled", label: t("statusCancelled", "Cancelled") },
  ];

  return (
    <ModalShell title={t("btnStatusChange", "Status Change")} onClose={onClose}
      cancelLabel={t("modalClose", "Close")} saving={saving} err={err}
      onSubmit={async e => {
        e.preventDefault();
        if (!action) { setErr(t("alertSelectStatus", "Please select a status.")); return; }
        const ok = await ask({
          message: t("confirmStatusChangeBulk", "Change the status of the selected rows?"),
          confirmLabel: t("modalSave", "Save"),
        });
        if (!ok) return;
        setSaving(true); setErr(null);
        try { onDone(await onApply(action)); }
        catch (ex: any) { setErr(ex?.code ? t(ex.code, ex.message) : (ex?.message ?? String(ex))); }
        finally { setSaving(false); }
      }}>
      <Field label={t("scmSelectedCount", "Selected Rows")}>
        <input type="text" readOnly value={String(count)} style={RO} />
      </Field>
      <Field label={t("scmSelectStatus", "New Status")}>
        <div className="flex flex-col gap-2.5 py-1 px-0.5">
          {options.map(o => (
            <label key={o.value} style={OPTION}>
              <input className="w-auto p-0" type="radio" name="scmStatus" value={o.value} checked={action === o.value}
                onChange={() => setAction(o.value)} />
              <span>{o.label}</span>
            </label>
          ))}
        </div>
      </Field>
    </ModalShell>
  );
}
