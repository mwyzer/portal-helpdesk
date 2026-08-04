import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useCandidatePortalAuthStore } from '@/store/candidatePortalAuthStore';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { LogOut } from 'lucide-react';

const navItems = [
  { to: '/portal/status', label: 'Application Status' },
  { to: '/portal/documents', label: 'Documents' },
  { to: '/portal/interviews', label: 'Interviews' },
];

export function CandidatePortalLayout() {
  const { profile, logout } = useCandidatePortalAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/portal/login');
  };

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="border-b bg-card">
        <div className="mx-auto flex h-16 max-w-4xl items-center gap-6 px-4">
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary">
              <span className="text-sm font-bold text-primary-foreground">AI</span>
            </div>
            <span className="font-semibold">Candidate Portal</span>
          </div>

          <nav className="flex flex-1 gap-4">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  cn(
                    'text-sm font-medium transition-colors',
                    isActive ? 'text-primary' : 'text-muted-foreground hover:text-foreground',
                  )
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <span className="text-sm text-muted-foreground">{profile?.fullName}</span>
          <Button variant="ghost" size="icon" onClick={handleLogout} aria-label="Log out" title="Log out">
            <LogOut className="h-4 w-4" />
          </Button>
        </div>
      </header>

      <main className="mx-auto max-w-4xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
}
