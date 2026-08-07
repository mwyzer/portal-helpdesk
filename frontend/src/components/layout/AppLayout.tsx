import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/button';
import { NotificationBell } from '@/components/feedback/NotificationBell';
import { ToastContainer } from '@/components/feedback/ToastContainer';
import { useSignalR } from '@/lib/useSignalR';
import { useToastStore } from '@/lib/useToast';
import { cn } from '@/lib/utils';
import {
  LayoutDashboard,
  Users,
  Shield,
  Building2,
  LogOut,
  Menu,
  X,
  Calendar,
  CheckSquare,
  FileText,
  FileCode,
  Bot,
  BookOpen,
  MessageSquare,
  UserCog,
  Clock,
  ClipboardCheck,
  Tags,
  Bell,
  Ticket,
  LayoutList,
  UserCheck,
  AlertTriangle,
  Briefcase,
  Users2,
  CalendarClock,
} from 'lucide-react';
import { useState, useEffect, useMemo } from 'react';
import type { LucideIcon } from 'lucide-react';

interface NavItem {
  to: string;
  label: string;
  icon: LucideIcon;
}

interface NavGroup {
  label: string;
  items: NavItem[];
}

// ── All navigation items ──────────────────────────

const allNavItems = {
  dashboard:        { to: '/dashboard',           label: 'Dashboard',        icon: LayoutDashboard },
  users:            { to: '/users',               label: 'Users',            icon: Users },
  roles:            { to: '/roles',               label: 'Roles',            icon: Shield },
  departments:      { to: '/departments',         label: 'Departments',      icon: Building2 },
  meetings:         { to: '/meetings',            label: 'Meetings',         icon: Calendar },
  actionItems:      { to: '/action-items',        label: 'Action Items',     icon: CheckSquare },
  documents:        { to: '/documents/requests',  label: 'Documents',        icon: FileText },
  templates:        { to: '/documents/templates', label: 'Templates',        icon: FileCode },
  aiChat:           { to: '/ai/chat',             label: 'AI Chat',          icon: Bot },
  conversations:    { to: '/ai/conversations',    label: 'Conversations',    icon: MessageSquare },
  knowledgeBase:    { to: '/knowledge-base',      label: 'Knowledge Base',   icon: BookOpen },
  employees:        { to: '/employees',           label: 'Employees',        icon: UserCog },
  leaveTypes:       { to: '/leave-types',         label: 'Leave Types',      icon: Tags },
  leaveRequests:    { to: '/leave-requests',      label: 'Leave Requests',   icon: Clock },
  leaveApprovals:   { to: '/leave-approvals',     label: 'Approvals',        icon: ClipboardCheck },
  notifications:    { to: '/notifications',       label: 'Notifications',    icon: Bell },
  tickets:          { to: '/tickets',             label: 'Tickets',          icon: Ticket },
  ticketCategories: { to: '/tickets/categories',  label: 'Categories',       icon: LayoutList },
  agentWorkload:    { to: '/tickets/agents',      label: 'Agent Workload',   icon: UserCheck },
  escalations:      { to: '/tickets/escalations', label: 'Escalations',      icon: AlertTriangle },
  vacancies:        { to: '/recruitment/vacancies',  label: 'Vacancies',     icon: Briefcase },
  candidates:       { to: '/recruitment/candidates', label: 'Candidates',    icon: Users2 },
  interviews:       { to: '/recruitment/interviews', label: 'Interviews',    icon: CalendarClock },
} as const;

// ── Role-based navigation groups ──────────────────

const adminNav: NavItem[] = [
  allNavItems.dashboard,
  allNavItems.users,
  allNavItems.roles,
  allNavItems.departments,
  allNavItems.employees,
  allNavItems.leaveTypes,
  allNavItems.leaveRequests,
  allNavItems.leaveApprovals,
  allNavItems.meetings,
  allNavItems.actionItems,
  allNavItems.documents,
  allNavItems.templates,
  allNavItems.aiChat,
  allNavItems.conversations,
  allNavItems.knowledgeBase,
  allNavItems.notifications,
  allNavItems.tickets,
  allNavItems.ticketCategories,
  allNavItems.agentWorkload,
  allNavItems.escalations,
  allNavItems.vacancies,
  allNavItems.candidates,
  allNavItems.interviews,
];

const managerNav: NavItem[] = [
  allNavItems.dashboard,
  allNavItems.employees,
  allNavItems.leaveRequests,
  allNavItems.leaveApprovals,
  allNavItems.meetings,
  allNavItems.actionItems,
  allNavItems.documents,
  allNavItems.aiChat,
  allNavItems.notifications,
  allNavItems.tickets,
  allNavItems.escalations,
  allNavItems.vacancies,
  allNavItems.candidates,
  allNavItems.interviews,
];

const secretaryNav: NavItem[] = [
  allNavItems.dashboard,
  allNavItems.meetings,
  allNavItems.actionItems,
  allNavItems.documents,
  allNavItems.templates,
  allNavItems.notifications,
  allNavItems.tickets,
];

const employeeNav: NavItem[] = [
  allNavItems.dashboard,
  allNavItems.leaveRequests,
  allNavItems.aiChat,
  allNavItems.knowledgeBase,
  allNavItems.notifications,
  allNavItems.tickets,
];

// ── Resolve nav items from user roles ─────────────

function resolveNavItems(roles: string[]): NavItem[] {
  if (roles.includes('Super Admin') || roles.includes('HRD')) return adminNav;
  if (roles.includes('Secretary')) return secretaryNav;
  if (roles.includes('Manager')) return managerNav;
  return employeeNav;
}

export function AppLayout() {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const addToast = useToastStore((s) => s.addToast);

  const navItems = useMemo(() => resolveNavItems(user?.roles ?? []), [user?.roles]);

  // Listen for real-time notifications via SignalR
  const { onNotification, onUnreadCount } = useSignalR();

  useEffect(() => {
    onNotification((notification) => {
      addToast({
        title: notification.title,
        message: '',
        type: 'info',
      });
    });
  }, [onNotification, addToast]);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="flex h-screen overflow-hidden">
      {/* Mobile overlay */}
      {sidebarOpen && (
        <div className="fixed inset-0 z-40 bg-black/50 lg:hidden" onClick={() => setSidebarOpen(false)} />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed inset-y-0 left-0 z-50 flex w-64 flex-col bg-card border-r transition-transform lg:static lg:translate-x-0',
          sidebarOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex h-16 items-center gap-2 border-b px-6">
          <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary">
            <span className="text-sm font-bold text-primary-foreground">AI</span>
          </div>
          <span className="font-semibold">AI Helpdesk</span>
          <Button variant="ghost" size="icon" className="ml-auto lg:hidden" onClick={() => setSidebarOpen(false)} aria-label="Close sidebar">
            <X className="h-4 w-4" />
          </Button>
        </div>

        <nav className="min-h-0 flex-1 space-y-1 overflow-y-auto p-4">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              onClick={() => setSidebarOpen(false)}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-primary/10 text-primary'
                    : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                )
              }
            >
              <item.icon className="h-4 w-4" />
              {item.label}
            </NavLink>
          ))}
        </nav>

        <div className="border-t p-4">
          <div className="flex items-center gap-3 rounded-lg px-3 py-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-primary/10 text-sm font-medium text-primary">
              {user?.fullName?.charAt(0) ?? 'U'}
            </div>
            <div className="flex-1 truncate">
              <p className="text-sm font-medium">{user?.fullName}</p>
              <p className="text-xs text-muted-foreground truncate">{user?.email}</p>
            </div>
          </div>
          <Button variant="ghost" className="mt-2 w-full justify-start text-muted-foreground" onClick={handleLogout}>
            <LogOut className="mr-2 h-4 w-4" />
            Sign Out
          </Button>
        </div>
      </aside>

      {/* Main */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Topbar */}
        <header className="flex h-16 items-center gap-4 border-b bg-card px-6">
          <Button variant="ghost" size="icon" className="lg:hidden" onClick={() => setSidebarOpen(true)} aria-label="Open sidebar">
            <Menu className="h-5 w-5" />
          </Button>

          <div className="flex-1" />

          <NotificationBell />
        </header>

        {/* Content */}
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>

      <ToastContainer />
    </div>
  );
}
