import { useCallback, useEffect, useRef, useState } from "react";


const MAX_SCROLL_PX = 33_000_000;

export interface VirtualWindow<T> {
  start: number;
  items: (T | undefined)[];
  padTop: number;
  padBottom: number;
  scrollRef: (node: HTMLDivElement | null) => void;
  pendingChunks: number;
  scrollToRow: (index: number) => void;
}

export function useVirtualRows<T>({
  total, rowHeight, overscan = 6, chunkSize = 100, fetchChunk, resetKey,
}: {
  total: number;
  rowHeight: number;
  overscan?: number;
  chunkSize?: number;
  fetchChunk: (offset: number, count: number) => Promise<T[]>;
  resetKey: string;
}): VirtualWindow<T> {
  const [el, setEl] = useState<HTMLDivElement | null>(null);
  const scrollRef = useCallback((node: HTMLDivElement | null) => setEl(node), []);
  const [scrollTop, setScrollTop] = useState(0);
  const [viewH, setViewH] = useState(0);
  const [, forceRender] = useState(0);

  const chunks = useRef(new Map<number, T[]>());
  const pending = useRef(new Set<number>());
  const alive = useRef(true);
  useEffect(() => () => { alive.current = false; }, []);

  useEffect(() => {
    chunks.current.clear();
    pending.current.clear();
    if (el) el.scrollTop = 0;
    setScrollTop(0);
    forceRender(n => n + 1);
  }, [resetKey, el]);

  useEffect(() => {
    if (!el) { setViewH(0); return; }
    const update = () => setViewH(el.clientHeight);
    update();
    const ro = new ResizeObserver(update);
    ro.observe(el);
    return () => ro.disconnect();
  }, [el]);

  useEffect(() => {
    if (!el) return;
    const onScroll = () => setScrollTop(el.scrollTop);
    onScroll();
    el.addEventListener("scroll", onScroll, { passive: true });
    return () => el.removeEventListener("scroll", onScroll);
  }, [el]);

  const virtualH = total * rowHeight;
  const physicalH = Math.min(virtualH, MAX_SCROLL_PX);
  const scale = virtualH > 0 ? physicalH / virtualH : 1;

  const toRow = useCallback(
    (px: number) => Math.floor(px / scale / rowHeight), [scale, rowHeight]);
  const toPixel = useCallback(
    (row: number) => row * rowHeight * scale, [scale, rowHeight]);

  const visibleCount = Math.max(1, Math.ceil(viewH / rowHeight));
  const start = Math.max(0, Math.min(Math.max(0, total - visibleCount), toRow(scrollTop) - overscan));
  const end = Math.min(total, start + visibleCount + overscan * 2);

  useEffect(() => {
    if (total === 0 || viewH === 0) return;
    const first = Math.floor(start / chunkSize);
    const last = Math.floor(Math.max(start, end - 1) / chunkSize);
    for (let ci = first; ci <= last; ci++) {
      if (chunks.current.has(ci) || pending.current.has(ci)) continue;
      pending.current.add(ci);
      forceRender(n => n + 1);
      const offset = ci * chunkSize;
      fetchChunk(offset, Math.min(chunkSize, total - offset))
        .then(rows => { if (alive.current) chunks.current.set(ci, rows); })
        .catch(() => { })
        .finally(() => {
          if (!alive.current) return;
          pending.current.delete(ci);
          forceRender(n => n + 1);
        });
    }
  }, [start, end, total, viewH, chunkSize, fetchChunk, resetKey]);

  const items: (T | undefined)[] = [];
  for (let i = start; i < end; i++) {
    const rows = chunks.current.get(Math.floor(i / chunkSize));
    items.push(rows ? rows[i % chunkSize] : undefined);
  }

  const scrollToRow = useCallback((index: number) => {
    if (el) el.scrollTop = toPixel(Math.max(0, index));
  }, [el, toPixel]);

  const realRowsH = (end - start) * rowHeight;
  const slack = Math.max(0, physicalH - realRowsH);
  const padTop = total > visibleCount
    ? Math.round(slack * (start / Math.max(1, total - (end - start))))
    : 0;

  return {
    start,
    items,
    padTop,
    padBottom: Math.max(0, slack - padTop),
    scrollRef,
    pendingChunks: pending.current.size,
    scrollToRow,
  };
}

export function Spacer({ height, cols }: { height: number; cols: number }) {
  if (height <= 0) return null;
  return <tr aria-hidden="true" style={{ height }}><td colSpan={cols} className="border-0 p-0" /></tr>;
}
