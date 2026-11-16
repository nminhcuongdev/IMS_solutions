export function snapshotOf(fields: Record<string, unknown>): Record<string, string> {
  return Object.fromEntries(Object.entries(fields)
    .filter(([k]) => k !== "Quantiy")
    .map(([k, v]) => [k, v == null ? "" : String(v)]));
}
