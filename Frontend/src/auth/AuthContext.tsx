import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { clearToken, getToken, setToken, setUnauthorizedHandler } from "@/api/client";
import { broadcastClear, requestSession, serveSession } from "@/auth/sessionSync";
import { clearCache } from "@/lib/useFetch";
import { login as apiLogin, logout as apiLogout, type LoginResponse } from "@/api/auth";

const USER_KEY = "ims.user";

type StoredUser = Omit<LoginResponse, "token" | "expiresIn" | "tokenType">;

interface AuthCtx {
  user: StoredUser | null;
  loading: boolean;
  booting: boolean;
  login: (userId: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthCtx | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StoredUser | null>(() => {
    const raw = sessionStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as StoredUser) : null;
  });
  const [loading, setLoading] = useState(false);
  const [booting, setBooting] = useState(() => !sessionStorage.getItem(USER_KEY));

  const clear = useCallback(() => {
    clearToken();
    sessionStorage.removeItem(USER_KEY);
    clearCache();
    setUser(null);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(() => clear());
  }, [clear]);

  useEffect(() => {
    if (!booting) return;
    let alive = true;
    requestSession().then(p => {
      if (!alive) return;
      if (p) {
        setToken(p.token);
        sessionStorage.setItem(USER_KEY, p.user);
        setUser(JSON.parse(p.user) as StoredUser);
      }
      setBooting(false);
    });
    return () => { alive = false; };
  }, [booting]);

  useEffect(() => serveSession(
    () => {
      const token = getToken();
      const u = sessionStorage.getItem(USER_KEY);
      return token && u ? { token, user: u } : null;
    },
    () => clear(),
  ), [clear]);

  const login = useCallback(async (userId: string, password: string) => {
    setLoading(true);
    try {
      const res = await apiLogin(userId, password);
      setToken(res.token);
      const stored: StoredUser = {
        userCode: res.userCode,
        userName: res.userName,
        roleCode: res.roleCode,
        isSuperAdmin: res.isSuperAdmin,
      };
      sessionStorage.setItem(USER_KEY, JSON.stringify(stored));
      setUser(stored);
    } finally {
      setLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    try { await apiLogout(); } catch { }
    clear();
    broadcastClear();
  }, [clear]);

  const value = useMemo(() => ({ user, loading, booting, login, logout }),
    [user, loading, booting, login, logout]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be inside AuthProvider");
  return ctx;
}
