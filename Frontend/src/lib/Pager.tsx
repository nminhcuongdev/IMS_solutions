const BTN_BASE = "h-[30px] min-w-[30px] rounded-lg border px-1.5 text-xs transition-colors";
const BTN = `${BTN_BASE} border-slate-300 bg-white text-slate-700 not-disabled:hover:border-brand-500 not-disabled:hover:bg-brand-50 not-disabled:hover:text-brand-700 disabled:cursor-not-allowed disabled:opacity-40`;
const BTN_ACTIVE = `${BTN_BASE} border-brand-600 bg-brand-600 font-bold text-white`;

export function Pager({
  total, page, pageSize, onGo,
}: { total: number; page: number; pageSize: number; onGo: (p: number) => void }) {
  const last = Math.max(1, Math.ceil(total / pageSize));
  const nums = pageNumbers(page, last);
  return (
    <div className="flex flex-none items-center gap-1">
      <button className={BTN} disabled={page <= 1} onClick={() => onGo(page - 1)}>‹</button>
      {nums.map((p, i) => p === "…"
        ? <span key={"e" + i} className="px-1 text-xs text-slate-400">…</span>
        : <button key={p} className={p === page ? BTN_ACTIVE : BTN} onClick={() => onGo(p)}>{p}</button>)}
      <button className={BTN} disabled={page >= last} onClick={() => onGo(page + 1)}>›</button>
    </div>
  );
}
function pageNumbers(cur: number, last: number): (number | "…")[] {
  if (last <= 7) return Array.from({ length: last }, (_, i) => i + 1);
  const s: (number | "…")[] = [1];
  if (cur > 4) s.push("…");
  for (let p = Math.max(2, cur - 2); p <= Math.min(last - 1, cur + 2); p++) s.push(p);
  if (cur < last - 3) s.push("…");
  s.push(last);
  return s;
}

export const PAGE_SIZES = [20, 50, 100, 200] as const;
