import { useAuth } from "@/auth/AuthContext";

export function DashboardPage() {
  const { user } = useAuth();
  const cards = [
    { title: "Menu", items: ["Welcome " + (user?.userName ?? "")] },
    { title: "Role", items: [user?.roleCode ?? "-", user?.isSuperAdmin ? "SuperAdmin" : "Normal user"] },
    { title: "Phase", items: ["Phase 1", "Master layer + Auth + Menu"] },
    { title: "Backend", items: [".NET 10 + EF Core", "SQL Server (Ims_Dev)"] },
  ];
  return (
    <div className="grid grid-cols-[repeat(auto-fill,minmax(280px,1fr))] gap-4 max-md:grid-cols-1">
      {cards.map(c => (
        <div
          className="cursor-pointer overflow-hidden rounded-xl bg-white shadow-sm ring-1 ring-slate-200 transition hover:-translate-y-0.5 hover:shadow-md"
          key={c.title}>
          <div className="flex items-center gap-2 border-b border-slate-100 px-4 py-3 text-sm font-semibold text-brand-700">
            <span className="size-2 rounded-full bg-brand-500" />
            {c.title}
          </div>
          <div className="px-4 py-3.5">
            <ul>
              {c.items.map((it, i) => (
                <li key={i} className="border-b border-slate-100 py-1.5 text-[13px] last:border-0 hover:text-brand-700">{it}</li>
              ))}
            </ul>
          </div>
        </div>
      ))}
    </div>
  );
}
