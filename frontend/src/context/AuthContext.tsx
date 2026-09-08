import { createContext, useContext, useState, type ReactNode } from "react";
import type { AuthResponse, UserRole } from "../types";
import { logoutRequest } from "../api/auth";

interface AuthUser {
  userId: number;
  fullName: string;
  email: string;
  role: UserRole;
  avatarUrl: string | null;
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  setSession: (auth: AuthResponse) => void;
  updateAvatarUrl: (avatarUrl: string | null) => void;
  updateFullName: (fullName: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredUser(): AuthUser | null {
  const stored = localStorage.getItem("user");
  if (!stored) return null;
  try {
    return JSON.parse(stored) as AuthUser;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(readStoredUser);

  function setSession(auth: AuthResponse) {
    const authUser: AuthUser = {
      userId: auth.userId,
      fullName: auth.fullName,
      email: auth.email,
      role: auth.role,
      avatarUrl: auth.avatarUrl ?? null,
    };
    localStorage.setItem("token", auth.token);
    localStorage.setItem("refreshToken", auth.refreshToken);
    localStorage.setItem("user", JSON.stringify(authUser));
    setUser(authUser);
  }

  function updateAvatarUrl(avatarUrl: string | null) {
    setUser((prev) => {
      if (!prev) return prev;
      const next = { ...prev, avatarUrl };
      localStorage.setItem("user", JSON.stringify(next));
      return next;
    });
  }

  function updateFullName(fullName: string) {
    setUser((prev) => {
      if (!prev) return prev;
      const next = { ...prev, fullName };
      localStorage.setItem("user", JSON.stringify(next));
      return next;
    });
  }

  function logout() {
    const storedRefreshToken = localStorage.getItem("refreshToken");
    if (storedRefreshToken) {
      // Best-effort — the client-side session is cleared regardless of whether this succeeds.
      logoutRequest(storedRefreshToken).catch(() => {});
    }
    localStorage.removeItem("token");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("user");
    setUser(null);
  }

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, setSession, updateAvatarUrl, updateFullName, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
