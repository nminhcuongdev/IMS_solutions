import { useEffect, useRef, useState } from "react";
import { useLang } from "./LanguageContext";
import type { LanguageOption } from "@/api/language";

function LangIcon({ option }: { option: LanguageOption }) {
  if (option.iconImage) return <img src={option.iconImage} alt={option.languageCode.toUpperCase()} />;
  return <span className="lang-code-icon">{option.languageCode.toUpperCase()}</span>;
}

export function LanguageSelect() {
  const { lang, setLang, languages } = useLang();
  const [open, setOpen] = useState(false);
  const root = useRef<HTMLDivElement>(null);

  const current = languages.find(l => l.languageCode === lang)
    ?? { languageCode: lang, languageName: lang, iconImage: null };

  useEffect(() => {
    if (!open) return;
    const close = (e: MouseEvent) => {
      if (root.current && !root.current.contains(e.target as Node)) setOpen(false);
    };
    const escape = (e: KeyboardEvent) => { if (e.key === "Escape") setOpen(false); };
    document.addEventListener("click", close);
    document.addEventListener("keydown", escape);
    return () => {
      document.removeEventListener("click", close);
      document.removeEventListener("keydown", escape);
    };
  }, [open]);

  if (languages.length === 0) return null;

  return (
    <div className={"lang-dropdown" + (open ? " open" : "")} ref={root}>
      <button
        type="button"
        className="lang-dropdown-toggle"
        title={current.languageName}
        aria-haspopup="listbox"
        aria-expanded={open}
        onClick={() => setOpen(o => !o)}
      >
        <LangIcon option={current} />
        <span className="chevron">&#9660;</span>
      </button>
      <div className="lang-dropdown-menu" role="listbox">
        {languages.map(l => (
          <div
            key={l.languageCode}
            role="option"
            aria-selected={l.languageCode === lang}
            className={"lang-dropdown-item" + (l.languageCode === lang ? " active" : "")}
            onClick={() => { setLang(l.languageCode); setOpen(false); }}
          >
            <LangIcon option={l} />
            {l.languageName}
          </div>
        ))}
      </div>
    </div>
  );
}
