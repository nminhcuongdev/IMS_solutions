import { ApiCallError } from "@/api/types";
import { MlEngineError } from "@/lib/printErrors";

type Translate = (key: string, fallback: string) => string;

const BAD_CHARS = "/ ? & = # %";
const NOT_FOUND_TAIL = " が見つかりません。";

function fill(text: string, detail: string | null): string {
  if (!text.includes("{0}")) return text;
  if (detail) return text.replace("{0}", detail);
  return text.replace(/[:：]?\s*\{0\}/, "").trim();
}

function fillAll(text: string, args: string[]): string {
  return args.reduce((acc, arg, i) => acc.split(`{${i}}`).join(arg), text);
}

export function errorText(t: Translate, ex: unknown): string {
  if (ex instanceof ApiCallError) {
    if (ex.messageKey) return fillAll(t(ex.messageKey, ex.message), ex.args.map(a => t(a, a)));
    if (ex.code === "VAL-005" && !ex.message.includes(BAD_CHARS))
      return t("valTooLong", ex.message);
    if (ex.code === "SYS-404") {
      const detail = ex.message.endsWith(NOT_FOUND_TAIL)
        ? ex.message.slice(0, -NOT_FOUND_TAIL.length)
        : null;
      return fill(t("SYS-404", ex.message), detail);
    }
    const arg = ex.args[0];
    return fill(t(ex.code, ex.message), arg ? t(arg, arg) : null);
  }
  if (ex instanceof MlEngineError) return t(ex.code, ex.fallback);
  if (ex instanceof TypeError && /fetch|network/i.test(ex.message)) return t("NET-002", "Network connection lost.");
  if (ex instanceof Error) return ex.message;
  return String(ex);
}
