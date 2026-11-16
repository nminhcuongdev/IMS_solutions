import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { getCurrentPermissions } from "@/api/permissions";
import type { CurrentPermissions } from "@/api/CurrentPermissions";
import { useAuth } from "@/auth/AuthContext";

interface PermissionCtx {
  ready: boolean;
  data: CurrentPermissions | null;
  canOpen: (functionCode: string) => boolean;
  can: (functionCode: string, actionType: string) => boolean;
  refresh: () => Promise<void>;
}

const Ctx = createContext<PermissionCtx | null>(null);

const ALWAYS_OPEN = new Set(["dashboard"]);

export function PermissionProvider({ children }: { children: ReactNode }) {
  const { user } = useAuth();
  const [data, setData] = useState<CurrentPermissions | null>(null);
  const [ready, setReady] = useState(false);

  const refresh = useCallback(async () => {
    if (!user) { setData(null); setReady(false); return; }
    try {
      setData(await getCurrentPermissions());
    } catch {
      setData(prev => prev);
    } finally {
      setReady(true);
    }
  }, [user]);

  useEffect(() => { setReady(false); refresh(); }, [refresh]);

  const value = useMemo<PermissionCtx>(() => {
    const allowed = new Set(data?.permissions ?? []);
    const everything = !!data && (data.isSuperAdmin || data.isBuiltin);
    const can = (fn: string, action: string) => everything || allowed.has(`${fn}.${action}`);
    const canOpen = (fn: string) => {
      if (ALWAYS_OPEN.has(fn)) return true;
      if (fn === "super_admin") return !!data?.isSuperAdmin;
      return can(fn, "Open");
    };
    return { ready, data, can, canOpen, refresh };
  }, [data, ready, refresh]);

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function usePermissions(): PermissionCtx {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("usePermissions must be used inside <PermissionProvider>");
  return ctx;
}

export function useCan(functionCode: string) {
  const { can } = usePermissions();
  return useCallback((actionType: string) => can(functionCode, actionType), [can, functionCode]);
}

export function Can({ fn, action, children }: { fn: string; action: string; children: ReactNode }) {
  const { can } = usePermissions();
  return can(fn, action) ? <>{children}</> : null;
}

const ScreenCtx = createContext<string>("");

export function ScreenProvider({ fn, children }: { fn: string; children: ReactNode }) {
  return <ScreenCtx.Provider value={fn}>{children}</ScreenCtx.Provider>;
}

export function useScreenCan() {
  const fn = useContext(ScreenCtx);
  const { can } = usePermissions();
  return useCallback((actionType: string) => !fn || can(fn, actionType), [can, fn]);
}
