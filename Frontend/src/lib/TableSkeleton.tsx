import { useCallback, useRef, useState } from "react";

export function TableSkeleton({ cols, rows = 8 }: { cols: number; rows?: number }) {
  return (
    <>
      {Array.from({ length: rows }, (_, r) => (
        <tr key={r} aria-hidden="true">
          {Array.from({ length: cols }, (_, c) => (
            <td key={c}>
              <span className="sk" style={{ width: `${55 + ((r * 7 + c * 23) % 40)}%` }} />
            </td>
          ))}
        </tr>
      ))}
    </>
  );
}

export function useRowFlash(ms = 1600) {
  const [code, setCode] = useState<string | null>(null);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const flash = useCallback((c: string | null | undefined) => {
    if (!c) return;
    if (timer.current) clearTimeout(timer.current);
    setCode(c);
    timer.current = setTimeout(() => setCode(null), ms);
  }, [ms]);

  const flashCls = useCallback(
    (c: string) => (c === code ? "is-flash" : undefined), [code]);

  return { flash, flashCls };
}
