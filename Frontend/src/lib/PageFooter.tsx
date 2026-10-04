import { Pager, PAGE_SIZES } from "./Pager";
import { useT } from "@/i18n/LanguageContext";

export function PageFooter({
  total, page, pageSize, onGoPage, onChangeSize,
}: {
  total: number; page: number; pageSize: number;
  onGoPage: (p: number) => void;
  onChangeSize: (s: number) => void;
}) {
  const t = useT();
  return (
    <div className="pg-footer">
      <div className="pg-pagesize">
        <span>{t("pgRowsPerPage", "Rows per page:")}</span>
        <select value={pageSize} onChange={e => onChangeSize(Number(e.target.value))}>
          {PAGE_SIZES.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <span className="ml-2.5">{t("pgTotal", "Total:")} {total}</span>
      </div>
      <Pager total={total} page={page} pageSize={pageSize} onGo={onGoPage} />
      <div className="pg-footer-spacer"></div>
    </div>
  );
}
