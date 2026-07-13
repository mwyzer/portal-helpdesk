import { Users, Shield, Building2, Activity } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';

const stats = [
  { title: 'Total Users', value: '—', icon: Users, color: 'text-blue-600' },
  { title: 'Active Roles', value: '—', icon: Shield, color: 'text-purple-600' },
  { title: 'Departments', value: '—', icon: Building2, color: 'text-emerald-600' },
  { title: 'Active Now', value: '—', icon: Activity, color: 'text-amber-600' },
];

export function DashboardPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Overview of your helpdesk system</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {stats.map((stat) => (
          <Card key={stat.title}>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">{stat.title}</CardTitle>
              <stat.icon className={`h-4 w-4 ${stat.color}`} />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{stat.value}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Welcome to AI Helpdesk</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-muted-foreground">
            Your Digital Secretary & HR Assistant. Use the sidebar to manage users, roles, and departments.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
