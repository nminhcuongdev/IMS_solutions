import { Fragment, type ReactNode } from "react";

export function fillText(template: string, ...args: (string | number)[]): string {
  return template.replace(/\{(\d+)\}/g, (m, i) => (Number(i) < args.length ? String(args[Number(i)]) : m));
}

export function fillBold(template: string, ...args: (string | number)[]): ReactNode {
  return template.split(/(\{\d+\})/).map((part, k) => {
    const m = /^\{(\d+)\}$/.exec(part);
    const i = m ? Number(m[1]) : -1;
    return i >= 0 && i < args.length
      ? <b key={k}>{args[i]}</b>
      : <Fragment key={k}>{part}</Fragment>;
  });
}
