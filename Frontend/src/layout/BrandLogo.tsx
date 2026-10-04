import { BRAND } from "@/brand";

export function BrandMark({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 32 32" aria-hidden="true">
      <rect x="1" y="1" width="30" height="30" rx="8" fill="#2a7f9e" />
      <rect x="8" y="8.5" width="16" height="3.2" rx="1.6" fill="#fff" />
      <rect x="8" y="14.4" width="16" height="3.2" rx="1.6" fill="#fff" fillOpacity="0.72" />
      <rect x="8" y="20.3" width="9.5" height="3.2" rx="1.6" fill="#fff" fillOpacity="0.48" />
      <rect x="19.6" y="19.2" width="5.4" height="5.4" rx="1.3" fill="#8fe3cf" />
    </svg>
  );
}

export function BrandLogo({
  tone = "dark", compact = false, size = "md",
}: { tone?: "dark" | "light"; compact?: boolean; size?: "md" | "lg" }) {
  const mark = size === "lg" ? "size-12" : "size-8";
  const name = size === "lg" ? "text-3xl" : "text-lg";
  const sub = size === "lg" ? "text-xs" : "text-[10px]";
  return (
    <span className="inline-flex items-center gap-2.5">
      <BrandMark className={`${mark} shrink-0`} />
      {!compact && (
        <span className="flex flex-col leading-none">
          <span className={`${name} font-extrabold tracking-[0.12em] ${tone === "light" ? "text-white" : "text-slate-900"}`}>
            {BRAND.name}
          </span>
          <span className={`${sub} mt-1 font-medium tracking-wide ${tone === "light" ? "text-slate-400" : "text-slate-500"}`}>
            {BRAND.fullName}
          </span>
        </span>
      )}
    </span>
  );
}
