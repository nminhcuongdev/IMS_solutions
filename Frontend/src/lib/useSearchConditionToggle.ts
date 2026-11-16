import { useEffect } from "react";
import { useT } from "@/i18n/LanguageContext";

const AREA = ".content .search-box";

function isSearchArea(box: HTMLElement): boolean {
  const rows = Array.from(box.children).some(x => x.classList.contains("search-row"));
  if (!rows) return false;
  return box.hasAttribute("data-search-area") || !!box.querySelector(":scope > .search-btns [data-role='search']");
}

export function useSearchConditionToggle(pathname: string) {
  const t = useT();
  useEffect(() => {
    let timer: number | undefined;
    const paint = (box: HTMLElement) => {
      const toggle = box.querySelector<HTMLButtonElement>(":scope > .sb-toggle");
      if (!toggle) return;
      const collapsed = box.classList.contains("sb-collapsed");
      const label = collapsed ? t("sbShow", "Show search conditions") : t("sbHide", "Hide search conditions");
      if (toggle.title !== label) toggle.title = label;
      toggle.setAttribute("aria-label", label);
      toggle.setAttribute("aria-expanded", String(!collapsed));
      const icon = toggle.querySelector("img");
      const src = `/Images/Icon/expand_${collapsed ? "more" : "less"}_50_000000.svg`;
      if (icon && icon.getAttribute("src") !== src) icon.setAttribute("src", src);
    };
    const scan = () => {
      document.querySelectorAll<HTMLElement>(AREA).forEach(box => {
        if (!isSearchArea(box)) return;
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
    scan();
    const observer = new MutationObserver(() => { window.clearTimeout(timer); timer = window.setTimeout(scan, 30); });
    observer.observe(document.querySelector(".content") ?? document.body, { childList: true, subtree: true });
    return () => { observer.disconnect(); window.clearTimeout(timer); };
  }, [pathname, t]);
}
