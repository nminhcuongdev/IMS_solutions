import { useEffect } from "react";
import { useT } from "@/i18n/LanguageContext";

type Field = HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement;

function isField(el: EventTarget | null): el is Field {
  return el instanceof HTMLInputElement || el instanceof HTMLSelectElement || el instanceof HTMLTextAreaElement;
}

export function NativeValidationText() {
  const t = useT();

  useEffect(() => {
    function onInvalid(e: Event) {
      const el = e.target;
      if (!isField(el)) return;
      el.setCustomValidity("");
      const v = el.validity;
      if (v.valid) return;
      if (v.valueMissing) {
        el.setCustomValidity(t("valFieldRequired", "Please fill in this field."));
      } else if (el instanceof HTMLInputElement && el.type === "number"
        && (v.rangeUnderflow || v.rangeOverflow || v.stepMismatch || v.badInput)) {
        el.setCustomValidity(t("QTY-001", "Please enter a valid quantity (0 or more)."));
      } else {
        el.setCustomValidity(t("valFieldInvalid", "Please enter a valid value."));
      }
    }
    function onEdit(e: Event) {
      if (isField(e.target)) e.target.setCustomValidity("");
    }
    document.addEventListener("invalid", onInvalid, true);
    document.addEventListener("input", onEdit, true);
    document.addEventListener("change", onEdit, true);
    return () => {
      document.removeEventListener("invalid", onInvalid, true);
      document.removeEventListener("input", onEdit, true);
      document.removeEventListener("change", onEdit, true);
    };
  }, [t]);

  return null;
}
