import { useCallback, useMemo, useState } from "react";
import type { SortState } from "./SortableTh";

export type SortValue = string | number | boolean | null | undefined;

const isBlank = (v: SortValue) => v === null || v === undefined || v === "";

export function compareSortValues(a: SortValue, b: SortValue): number {
  if (isBlank(a) || isBlank(b)) return isBlank(a) === isBlank(b) ? 0 : isBlank(a) ? -1 : 1;
  if (typeof a === "number" && typeof b === "number") return a - b;
  if (typeof a === "boolean" && typeof b === "boolean") return Number(a) - Number(b);
  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: "base" });
}

const NONE: SortState = { field: "", dir: "asc", value: "" };

export function useLocalSort<T>(
  rows: T[],
  keyOf: (row: T) => string,
  columns: Record<string, (row: T) => SortValue>,
) {
  const [sort, setSort] = useState<SortState>(NONE);
  const [order, setOrder] = useState<Map<string, number> | null>(null);

  const onSort = useCallback((field: string) => {
    const get = columns[field];
    if (!get) return;
    const dir = sort.field === field && sort.dir === "asc" ? "desc" : "asc";
    const sign = dir === "asc" ? 1 : -1;
    const ranked = [...rows].sort((a, b) => sign * compareSortValues(get(a), get(b)));
    setOrder(new Map(ranked.map((r, i) => [keyOf(r), i])));
    setSort({ field, dir, value: dir === "desc" ? `-${field}` : field });
  }, [columns, keyOf, rows, sort]);

  const sorted = useMemo(() => {
    if (!order) return rows;
    const last = order.size;
    const pos = (r: T) => order.get(keyOf(r)) ?? last;
    return [...rows].sort((a, b) => pos(a) - pos(b));
  }, [rows, order, keyOf]);

  return { sorted, sort, onSort };
}
