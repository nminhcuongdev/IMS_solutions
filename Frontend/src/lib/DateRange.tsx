import { useState } from "react";
import { useT } from "@/i18n/LanguageContext";

export function DateRange({
  enabled, from, to, onToggle, onFrom, onTo, label,
}: {
  enabled: boolean;
  from: string;
  to: string;
  onToggle: (v: boolean) => void;
  onFrom: (v: string) => void;
  onTo: (v: string) => void;
  label?: string;
}) {
  const t = useT();
  return (
    <>
      <input type="checkbox" checked={enabled} onChange={e => onToggle(e.target.checked)} />
      <label>{label ?? t("thWorkedDate", "Worked Date")}</label>
      <DateInput value={from} disabled={!enabled} onChange={onFrom} />
      <span className="search-sep">～</span>
      <DateInput value={to} disabled={!enabled} onChange={onTo} />
    </>
  );
}

function todayText(): string {
  return new Date().toLocaleDateString(undefined, { year: "numeric", month: "2-digit", day: "2-digit" });
}

function DateInput({
  value, disabled, onChange,
}: {
  value: string;
  disabled?: boolean;
  onChange: (v: string) => void;
}) {
  const [focused, setFocused] = useState(false);
  const showPh = !value && !focused;
  return (
    <span className="relative inline-flex items-center">
      <input type="date" className={`w-[145px] ${showPh ? "[&::-webkit-datetime-edit]:text-transparent" : ""}`}
        value={value} disabled={disabled}
        onFocus={() => setFocused(true)} onBlur={() => setFocused(false)}
        onChange={e => onChange(e.target.value)} />
      {showPh && <span className="pointer-events-none absolute top-1/2 left-[9px] -translate-y-1/2 text-xs text-slate-400">{todayText()}</span>}
    </span>
  );
}

export function defaultDateRange(): { from: string; to: string } {
  return { from: "", to: "" };
}

function iso(d: Date): string {
  const p = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

export function currentMonthRange(): { from: string; to: string } {
  const now = new Date();
  return {
    from: iso(new Date(now.getFullYear(), now.getMonth(), 1)),
    to: iso(new Date(now.getFullYear(), now.getMonth() + 1, 0)),
  };
}

export function useDateDraft(
  on: boolean, from: string, to: string, def: { from: string; to: string },
) {
  const [dOn, setOn] = useState(on);
  const [dFrom, setFrom] = useState(from);
  const [dTo, setTo] = useState(to);
  return {
    props: { enabled: dOn, from: dFrom, to: dTo, onToggle: setOn, onFrom: setFrom, onTo: setTo },
    patch: (): Record<string, string | null> => ({
      dates: dOn ? null : "0",
      from: dFrom === def.from ? null : dFrom,
      to: dTo === def.to ? null : dTo,
    }),
  };
}
