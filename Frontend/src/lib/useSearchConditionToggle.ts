import { useEffect } from "react";
import { useT } from "@/i18n/LanguageContext";

export function useSearchConditionToggle(pathname: string) {
  const t = useT();
  useEffect(() => {
    let timer: number | undefined;
    const scan = () => {
      document.querySelectorAll<HTMLElement>(".content .search-box").forEach(box => {
        const rows = Array.from(box.children).filter(x => x.classList.contains("search-row"));
        const buttons = Array.from(box.querySelectorAll<HTMLButtonElement>(":scope > .search-btns > button"));
        if (!rows.length || !buttons.some(b => b.classList.contains("btn-primary") && b.textContent?.trim() === t("search", "Search"))) return;
        let toggle = box.querySelector<HTMLButtonElement>(":scope > .sb-toggle");
        if (!toggle) {
          toggle = document.createElement("button");
          toggle.type = "button";
          toggle.className = "sb-toggle";
          const icon = document.createElement("img");
          icon.className = "icn-img";
          icon.alt = "";
          toggle.append(icon);
          box.prepend(toggle);
          toggle.addEventListener("click", () => {
            box.classList.toggle("sb-collapsed");
            paint(box);
          });
        }
        paint(box);
      });
    };
    const paint = (box: HTMLElement) => {
      const toggle = box.querySelector<HTMLButtonElement>(":scope > .sb-toggle");
      if (!toggle) return;
      const collapsed = box.classList.contains("sb-collapsed");
      const label = collapsed ? t("sbShow", "Show search conditions") : t("sbHide", "Hide search conditions");
      toggle.title = label;
      toggle.setAttribute("aria-label", label);
      toggle.setAttribute("aria-expanded", String(!collapsed));
      const icon = toggle.querySelector("img");
      if (icon) icon.src = `/Images/Icon/expand_${collapsed ? "more" : "less"}_50_000000.svg`;
      let summary = box.querySelector<HTMLElement>(":scope > .sb-summary");
      if (!summary) {
        summary = document.createElement("div");
        summary.className = "sb-summary";
        toggle.after(summary);
      }
      const values = Array.from(box.querySelectorAll<HTMLInputElement | HTMLSelectElement>(":scope > .search-row input, :scope > .search-row select"))
        .filter(el => el instanceof HTMLInputElement ? el.type !== "checkbox" || el.checked : true)
        .map(el => el instanceof HTMLSelectElement ? el.selectedOptions[0]?.textContent?.trim() : el.value.trim())
        .filter(Boolean);
      const summaryText = values.length ? values.join(" · ") : t("sbNoCond", "No search conditions");
      if (summary.textContent !== summaryText) summary.textContent = summaryText;
      summary.hidden = !collapsed;
    };
    scan();
    const observer = new MutationObserver(() => { window.clearTimeout(timer); timer = window.setTimeout(scan, 30); });
    observer.observe(document.querySelector(".content") ?? document.body, { childList: true, subtree: true });
    return () => { observer.disconnect(); window.clearTimeout(timer); };
  }, [pathname, t]);
}
