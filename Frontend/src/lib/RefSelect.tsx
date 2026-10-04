import { useEffect, useState } from "react";
import { useT } from "@/i18n/LanguageContext";

export interface RefOption {
  value: string;
  label: string;
}

export function useRefOptions<T>(
  load: () => Promise<{ data: T[] }>,
  toOption: (row: T) => RefOption,
  deps: unknown[] = [],
): RefOption[] {
  const [opts, setOpts] = useState<RefOption[]>([]);
  useEffect(() => {
    let alive = true;
    load()
      .then(r => { if (alive) setOpts(r.data.map(toOption)); })
      .catch(() => { if (alive) setOpts([]); });
    return () => { alive = false; };
  }, deps);
  return opts;
}

export function RefSelect({
  value, onChange, options, required, disabled, placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  options: RefOption[];
  required?: boolean;
  disabled?: boolean;
  placeholder?: string;
}) {
  const t = useT();
  const orphan = value !== "" && !options.some(o => o.value === value);
  return (
    <select
      required={required}
      disabled={disabled}
      value={value}
      onChange={e => onChange(e.target.value)}
      style={disabled ? { background: "#f7fafc", color: "#718096" } : undefined}
    >
      <option value="">{placeholder ?? t("selectPlaceholder", "— select —")}</option>
      {orphan && (
        <option value={value}>{value} ({t("refMissing", "not in list")})</option>
      )}
      {options.map(o => (
        <option key={o.value} value={o.value}>{o.label}</option>
      ))}
    </select>
  );
}

export const codeName = (code: string, name?: string | null): string =>
  name ? `${code} — ${name}` : code;
