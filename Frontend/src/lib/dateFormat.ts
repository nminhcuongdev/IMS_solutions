const slash = (v: string) => v.replace(/-/g, "/");

export const fmtDate = (v: string | null | undefined): string =>
  v ? slash(v.slice(0, 10)) : "—";

export const fmtDateTime = (v: string | null | undefined, seconds = false): string =>
  v ? slash(v.replace("T", " ").slice(0, seconds ? 19 : 16)) : "—";
