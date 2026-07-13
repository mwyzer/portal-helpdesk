import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import api from '@/lib/axios';
import { useAuthStore } from '@/store/authStore';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { LeaveBalanceCard } from '@/components/LeaveBalanceCard';
import {
  Users,
  Clock,
  CheckCircle,
  AlertCircle,
  UserCheck,
  Plus,
  ArrowRight,
  TrendingUp,
  Bell,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from 'recharts';

// ── Types ──────────────────────────────────────────

interface EmployeeListResponse {
  items: { id: string }[];
  totalCount: number;
}

interface LeaveRequestResponse {
  id: string;
  employeeName: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  totalDays: number;
  status: string;
}

interface LeaveRequestListResponse {
  items: LeaveRequestResponse[];
  totalCount: number;
}

interface LeaveBalanceResponse {
  leaveTypeId: string;
  leaveTypeName: string;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
}

// ── Helpers ────────────────────────────────────────

const PIE_COLORS = ['#22c55e', '#ef4444', '#f59e0b', '#8b5cf6', '#3b82f6', '#ec4899'];

const statusColor = (status: string) => {
  const map: Record<string, string> = {
    Approved: 'bg-green-100 text-green-800',
    Rejected: 'bg-red-100 text-red-800',
    Submitted: 'bg-blue-100 text-blue-800',
    WaitingForManager: 'bg-yellow-100 text-yellow-800',
    WaitingForHR: 'bg-purple-100 text-purple-800',
    Draft: 'bg-gray-100 text-gray-800',
    Cancelled: 'bg-orange-100 text-orange-800',
  };
  return map[status] || 'bg-gray-100 text-gray-800';
};

// ── Component ──────────────────────────────────────

export function DashboardPage() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const isHRD = user?.roles?.includes('HRD') || user?.roles?.includes('SuperAdmin');
  const isManager = user?.roles?.includes('Manager');
  const isAdmin = isHRD || isManager;

  const { data: employeesData } = useQuery<EmployeeListResponse>({
    queryKey: ['employees', 'dashboard'],
    queryFn: () => api.get('/employees?page=1&pageSize=1').then((r) => r.data),
    enabled: isAdmin,
  });

  const { data: pendingData } = useQuery<LeaveRequestListResponse>({
    queryKey: ['leave-requests', 'pending-approval'],
    queryFn: () => api.get('/leave-requests/pending-approval?page=1&pageSize=1').then((r) => r.data),
    enabled: isAdmin,
  });

  const { data: allLeaveData } = useQuery<LeaveRequestListResponse>({
    queryKey: ['leave-requests', 'all', 'dashboard'],
    queryFn: () => api.get('/leave-requests?page=1&pageSize=100').then((r) => r.data),
    enabled: isAdmin,
  });

  const { data: myLeaveData } = useQuery<LeaveRequestListResponse>({
    queryKey: ['leave-requests', 'my', 'dashboard'],
    queryFn: () => api.get('/leave-requests?page=1&pageSize=5').then((r) => r.data),
    enabled: !isAdmin,
  });

  const { data: balances } = useQuery<LeaveBalanceResponse[]>({
    queryKey: ['leave-balances', 'my'],
    queryFn: () => api.get('/leave-balances/my').then((r) => r.data),
    enabled: !isAdmin,
  });

  const { data: unreadData } = useQuery<number>({
    queryKey: ['notifications', 'unread-count'],
    queryFn: () => api.get('/notifications/unread-count').then((r) => r.data),
  });

  const statusChart = useMemo(() => {
    if (!allLeaveData?.items) return [];
    const counts: Record<string, number> = {};
    allLeaveData.items.forEach((r) => {
      counts[r.status] = (counts[r.status] || 0) + 1;
    });
    return Object.entries(counts).map(([name, value]) => ({ name, value }));
  }, [allLeaveData]);

  const recentLeaveItems = (isAdmin ? allLeaveData?.items : myLeaveData?.items)?.slice(0, 5) ?? [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
          <p className="text-muted-foreground">
            {isAdmin ? 'HR & Employee overview' : 'Your workspace overview'}
          </p>
        </div>
        {!isAdmin && (
          <Button onClick={() => navigate('/leave-requests')}>
            <Plus className="mr-2 h-4 w-4" /> Submit Leave
          </Button>
        )}
      </div>

      {/* Admin Stat Cards */}
      {isAdmin && (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Total Employees</CardTitle>
              <Users className="h-4 w-4 text-blue-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{employeesData?.totalCount ?? '—'}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Pending Approvals</CardTitle>
              <Clock className="h-4 w-4 text-amber-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{pendingData?.totalCount ?? '—'}</div>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Approved This Month</CardTitle>
              <CheckCircle className="h-4 w-4 text-green-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">
                {allLeaveData?.items.filter((r) => r.status === 'Approved').length ?? '—'}
              </div>
            </CardContent>
          </Card>
          <Card
            className="cursor-pointer hover:border-primary/50 transition-colors"
            onClick={() => navigate('/leave-approvals')}
          >
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Needs Your Action</CardTitle>
              <AlertCircle className="h-4 w-4 text-red-600" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{pendingData?.totalCount ?? '—'}</div>
              <p className="text-xs text-muted-foreground mt-1">Click to review →</p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Employee Leave Balance Cards */}
      {!isAdmin && balances && balances.length > 0 && (
        <div>
          <h2 className="text-lg font-semibold mb-3">Your Leave Balances</h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {balances.map((b) => (
                <LeaveBalanceCard
                  key={b.leaveTypeId}
                  leaveTypeId={b.leaveTypeId}
                  leaveTypeName={b.leaveTypeName}
                  totalDays={b.totalDays}
                  usedDays={b.usedDays}
                  remainingDays={b.remainingDays}
                />
              ))}
          </div>
        </div>
      )}

      {/* Charts Row (Admin) */}
      {isAdmin && statusChart.length > 0 && (
        <div className="grid gap-6 lg:grid-cols-2">
          <Card>
            <CardHeader>
              <CardTitle>Leave Requests by Status</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={250}>
                <PieChart>
                  <Pie
                    data={statusChart}
                    cx="50%"
                    cy="50%"
                    outerRadius={80}
                    dataKey="value"
                    label={({ name, value }) => `${name}: ${value}`}
                  >
                    {statusChart.map((_, idx) => (
                      <Cell key={idx} fill={PIE_COLORS[idx % PIE_COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Status Overview</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponsiveContainer width="100%" height={250}>
                <BarChart data={statusChart}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                  <YAxis allowDecimals={false} />
                  <Tooltip />
                  <Bar dataKey="value" fill="#3b82f6" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Recent Leave Requests */}
      {recentLeaveItems.length > 0 && (
        <Card>
          <CardHeader className="flex flex-row items-center justify-between">
            <CardTitle>Recent Leave Requests</CardTitle>
            <Button variant="ghost" size="sm" onClick={() => navigate('/leave-requests')}>
              View All <ArrowRight className="ml-1 h-4 w-4" />
            </Button>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {recentLeaveItems.map((r) => (
                <div
                  key={r.id}
                  className="flex items-center justify-between rounded-lg border p-3 hover:bg-muted/50 transition-colors cursor-pointer"
                  onClick={() => navigate('/leave-requests')}
                >
                  <div className="flex items-center gap-3">
                    <div className="flex h-9 w-9 items-center justify-center rounded-full bg-primary/10">
                      <UserCheck className="h-4 w-4 text-primary" />
                    </div>
                    <div>
                      <p className="text-sm font-medium">
                        {isAdmin ? r.employeeName : r.leaveTypeName}
                      </p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(r.startDate).toLocaleDateString()} –{' '}
                        {new Date(r.endDate).toLocaleDateString()} · {r.totalDays} day
                        {r.totalDays !== 1 ? 's' : ''}
                      </p>
                    </div>
                  </div>
                  <Badge className={statusColor(r.status)}>{r.status}</Badge>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Welcome Card (fallback for empty state) */}
      {!isAdmin && (!balances || balances.length === 0) && recentLeaveItems.length === 0 && (
        <Card>
          <CardHeader>
            <CardTitle>Welcome to AI Helpdesk</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-muted-foreground">
              Your Digital Secretary & HR Assistant. Here's what you can do:
            </p>
            <div className="grid gap-3 sm:grid-cols-2">
              <Button
                variant="outline"
                className="justify-start h-auto py-3"
                onClick={() => navigate('/leave-requests')}
              >
                <TrendingUp className="mr-2 h-4 w-4" />
                <div className="text-left">
                  <p className="font-medium">Submit Leave</p>
                  <p className="text-xs text-muted-foreground">Request time off</p>
                </div>
              </Button>
              <Button
                variant="outline"
                className="justify-start h-auto py-3"
                onClick={() => navigate('/notifications')}
              >
                <Bell className="mr-2 h-4 w-4" />
                <div className="text-left">
                  <p className="font-medium">Notifications</p>
                  <p className="text-xs text-muted-foreground">
                    {unreadData ? `${unreadData} unread` : 'View alerts'}
                  </p>
                </div>
              </Button>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
