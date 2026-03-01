import { NavLink } from 'react-router';
import {
  LayoutDashboard,
  FileText,
  Plane,
  Calendar,
  Users,
  BarChart3,
  Settings,
  LogOut,
} from 'lucide-react';
import { useAuth } from '@/features/auth/AuthContext';
import { Button } from '@/components/ui/button';
import { ThemeToggle } from '@/components/shared/ThemeToggle';
import { cn } from '@/lib/utils';

const navItems = [
  { to: '/', icon: LayoutDashboard, label: 'Tableau de bord' },
  { to: '/scenarios', icon: FileText, label: 'Scenarios' },
  { to: '/programme/vols', icon: Plane, label: 'Vols' },
  { to: '/programme/blocs', icon: Plane, label: 'Blocs' },
  { to: '/programme/semaines', icon: Calendar, label: 'Semaines types' },
  { to: '/programme/calendrier', icon: Calendar, label: 'Calendrier' },
  { to: '/equipage', icon: Users, label: 'Equipage' },
  { to: '/resultats', icon: BarChart3, label: 'Resultats' },
  { to: '/parametres', icon: Settings, label: 'Parametres' },
];

export function Sidebar() {
  const { user, logout } = useAuth();

  return (
    <aside className="flex h-screen w-60 flex-col border-r border-sidebar-border bg-sidebar text-sidebar-foreground">
      <div className="flex h-14 items-center gap-2 px-4">
        <Plane className="h-5 w-5 text-primary" />
        <span className="font-data text-lg font-semibold tracking-tight text-primary">
          CrewSizer
        </span>
      </div>
      <div className="mx-3 h-px bg-sidebar-border" />
      <nav className="flex-1 space-y-0.5 p-2">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.to === '/'}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors',
                isActive
                  ? 'bg-sidebar-accent text-primary font-medium'
                  : 'text-sidebar-foreground hover:bg-sidebar-accent/50 hover:text-sidebar-accent-foreground'
              )
            }
          >
            <item.icon className="h-4 w-4" />
            {item.label}
          </NavLink>
        ))}
      </nav>
      <div className="mx-3 h-px bg-sidebar-border" />
      <div className="p-4">
        <div className="mb-2 flex items-center justify-between">
          <p className="truncate text-xs text-muted-foreground font-data">
            {user?.nomComplet ?? user?.userName}
          </p>
          <ThemeToggle />
        </div>
        <Button
          variant="ghost"
          size="sm"
          className="w-full justify-start text-muted-foreground hover:text-foreground"
          onClick={logout}
        >
          <LogOut className="mr-2 h-4 w-4" />
          Deconnexion
        </Button>
      </div>
    </aside>
  );
}
