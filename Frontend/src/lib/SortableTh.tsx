import { useCallback, type ReactNode } from "react";
import { useT } from "@/i18n/LanguageContext";
import { useUrlPatch } from "./useUrlState";
import { useSearchParams } from "react-router-dom";

export type SortDir = "asc" | "desc";

export interface SortState {
  field: string;
  dir: SortDir;
  value: string;
}

export function useSort(defaultField: string, defaultDir: SortDir = "asc") {
  const def = defaultDir === "desc" ? `-${defaultField}` : defaultField;
  const [sp] = useSearchParams();
  const patch = useUrlPatch();
  const value = sp.get("sort") ?? def;

  const desc = value.startsWith("-");
  const field = desc ? value.slice(1) : value;
  const dir: SortDir = desc ? "desc" : "asc";

  const toggle = useCallback((next: string) => {
    const v = next === field ? (desc ? next : `-${next}`) : next;
    patch({ sort: v === def ? null : v, page: null });
  }, [field, desc, def, patch]);

  const sort: SortState = { field, dir, value };
  return { sort, toggleSort: toggle };
}

export function SortTh({
  field, sort, onSort, className, children,
}: {
  field: string;
  sort: SortState;
  onSort: (field: string) => void;
  className?: string;
  children: ReactNode;
}) {
  const t = useT();
  const active = sort.field === field;
  const cls = ["sortable", active ? (sort.dir === "asc" ? "sort-asc" : "sort-desc") : "", className ?? ""]
    .filter(Boolean).join(" ");
  return (
    <th
      className={cls}
      onClick={() => onSort(field)}
      role="button"
      tabIndex={0}
      onKeyDown={e => { if (e.key === "Enter" || e.key === " ") { e.preventDefault(); onSort(field); } }}
      aria-sort={active ? (sort.dir === "asc" ? "ascending" : "descending") : "none"}
      title={active && sort.dir === "asc" ? t("sortDesc", "Sort descending") : t("sortAsc", "Sort ascending")}
    >
      {children}
    </th>
  );
}
