import { useCallback, useEffect, useRef, useState } from "react";


export function isTypingTarget(el: EventTarget | null): boolean {
  const n = el as HTMLElement | null;
  if (!n || !n.tagName) return false;
  const tag = n.tagName.toUpperCase();
  if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT") {
    const type = (n as HTMLInputElement).type;
    return !(tag === "INPUT" && (type === "checkbox" || type === "radio"));
  }
  return n.isContentEditable === true;
}

export interface ShortcutMap {
  [key: string]: ((e: KeyboardEvent) => void) | undefined;
}

export function useShortcuts(map: ShortcutMap, enabled = true) {
  const ref = useRef(map);
  ref.current = map;

  useEffect(() => {
    if (!enabled) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.ctrlKey || e.altKey || e.metaKey) return;
      if (e.key !== "Escape") {
        if (isTypingTarget(e.target)) return;
        if (document.querySelector(".modal-overlay")) return;
      }

      const fn = ref.current[e.key];
      if (!fn) return;
      e.preventDefault();
      fn(e);
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [enabled]);
}

export function useRowNav({
  codes, setSelected,
}: {
  codes: string[];
  setSelected: (fn: (prev: Set<string>) => Set<string>) => void;
}): { keys: ShortcutMap; cursorCode: string | null; setCursorTo: (code: string) => void } {
  const [cursor, setCursor] = useState(-1);

  const sig = codes.join("\u0000");
  const lastSig = useRef(sig);
  if (lastSig.current !== sig) { lastSig.current = sig; if (cursor >= codes.length) setCursor(-1); }

  const move = (delta: number, extend: boolean) => {
    if (codes.length === 0) return;
    setCursor(c => {
      const next = c < 0
        ? (delta > 0 ? 0 : codes.length - 1)
        : Math.min(codes.length - 1, Math.max(0, c + delta));
      setSelected(prev => extend ? new Set([...prev, codes[next]]) : new Set([codes[next]]));
      requestAnimationFrame(() => {
        document.querySelector(`[data-row-code="${CSS.escape(codes[next])}"]`)
          ?.scrollIntoView({ block: "nearest" });
      });
      return next;
    });
  };

  const keys: ShortcutMap = {
    ArrowDown: e => move(1, e.shiftKey),
    ArrowUp: e => move(-1, e.shiftKey),
    " ": () => {
      if (cursor < 0 || cursor >= codes.length) return;
      const code = codes[cursor];
      setSelected(prev => {
        const s = new Set(prev);
        s.has(code) ? s.delete(code) : s.add(code);
        return s;
      });
    },
  };

  const setCursorTo = useCallback((code: string) => {
    const i = codes.indexOf(code);
    if (i >= 0) setCursor(i);
  }, [sig]);

  return { keys, cursorCode: cursor >= 0 && cursor < codes.length ? codes[cursor] : null, setCursorTo };
}
