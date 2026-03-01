import { createContext, useContext, useState, useCallback, type ReactNode } from 'react';
import { authApi } from '@/api/auth';
import type { LoginRequest } from '@/types/auth';

interface AuthUser {
  userName: string;
  nomComplet: string | null;
}

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (data: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

function loadUser(): AuthUser | null {
  const token = localStorage.getItem('accessToken');
  const userName = localStorage.getItem('userName');
  if (token && userName) {
    return { userName, nomComplet: localStorage.getItem('nomComplet') };
  }
  return null;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(loadUser);

  const login = useCallback(async (data: LoginRequest) => {
    const response = await authApi.login(data);
    localStorage.setItem('accessToken', response.accessToken);
    localStorage.setItem('refreshToken', response.refreshToken);
    localStorage.setItem('userName', response.userName);
    if (response.nomComplet) {
      localStorage.setItem('nomComplet', response.nomComplet);
    }
    setUser({ userName: response.userName, nomComplet: response.nomComplet });
  }, []);

  const logout = useCallback(() => {
    authApi.logout().catch(() => {});
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('userName');
    localStorage.removeItem('nomComplet');
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, isAuthenticated: !!user, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
