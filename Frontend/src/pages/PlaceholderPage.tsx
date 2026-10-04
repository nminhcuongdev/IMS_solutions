import { useParams } from "react-router-dom";
import { useT } from "@/i18n/LanguageContext";

export function PlaceholderPage() {
  const t = useT();
  const { code } = useParams();
  return (
    <div className="page-info">
      <svg className="ic w-5 h-5" viewBox="0 0 24 24">
        <path d="M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20zm0 5a1.4 1.4 0 1 1 0 2.8A1.4 1.4 0 0 1 12 7zm1.1 10H10.9v-6h2.2v6z" />
      </svg>
      <div>
        <b>{t("comingSoon", "Coming soon")}</b> — UI cho function <code>{code ?? "?"}</code> chưa được triển khai.
        Backend đã có endpoint; frontend đang nhân bản pattern Zone Master.
      </div>
    </div>
  );
}
