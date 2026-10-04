import { useCallback } from "react";
import type { MouseEvent } from "react";


const INTERACTIVE = "input, button, a, select, textarea, label, [role='button']";

export function useRowSelect(
  toggle: (code: string) => void,
  setCursorTo?: (code: string) => void,
) {
  return useCallback((code: string) => ({
    onClick: (e: MouseEvent<HTMLTableRowElement>) => {
      if (e.button !== 0) return;
      if ((e.target as HTMLElement).closest(INTERACTIVE)) return;
      const sel = window.getSelection();
      if (sel && !sel.isCollapsed && sel.toString().trim() !== "") return;
      setCursorTo?.(code);
      toggle(code);
    },
  }), [toggle, setCursorTo]);
}

export function rowClickProps(onSelect: () => void) {
  return {
    onClick: (e: MouseEvent<HTMLTableRowElement>) => {
      if (e.button !== 0) return;
      if ((e.target as HTMLElement).closest(INTERACTIVE)) return;
      const sel = window.getSelection();
      if (sel && !sel.isCollapsed && sel.toString().trim() !== "") return;
      onSelect();
    },
  };
}
