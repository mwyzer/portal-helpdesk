import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { RefreshCw, Eye } from 'lucide-react';

interface AuditLogResponse {
  id: string;
  timestamp: string;
  userId: string | null;
  userName: string | null;
  action: 'Create' | 'Update' | 'Delete';
  entityName: string;
  entityId: string;
  changes: string;
  ipAddress: string | null;
}

interface AuditLogListResponse {
  items: AuditLogResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const actionVariant: Record<string, 'success' | 'default' | 'destructive'> = {
  Create: 'success',
  Update: 'default',
  Delete: 'destructive',
};

const PAGE_SIZE = 20;

export function AuditLogPage() {
  const [page, setPage] = useState(1);
  const [entityName, setEntityName] = useState('');
  const [action, setAction] = useState('');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [viewingChanges, setViewingChanges] = useState<AuditLogResponse | null>(null);

  const { data, isLoading, refetch } = useQuery<AuditLogListResponse>({
    queryKey: ['audit-logs', page, entityName, action, from, to],
    queryFn: () =>
      api
        .get('/audit-logs', {
          params: {
            page,
            pageSize: PAGE_SIZE,
            entityName: entityName || undefined,
            action: action || undefined,
            from: from || undefined,
            to: to || undefined,
          },
        })
        .then((r) => r.data),
  });

  const totalCount = data?.totalCount ?? 0;

  const formatChanges = (json: string) => {
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Audit Log</h1>
        <p className="text-muted-foreground">Every recorded create, update, and delete across the system</p>
      </div>

      <Card>
        <CardHeader className="pb-0 flex-row items-center justify-between">
          <CardTitle>Activity</CardTitle>
          <Button variant="outline" size="icon" onClick={() => refetch()} aria-label="Refresh">
            <RefreshCw className="h-4 w-4" />
          </Button>
        </CardHeader>
        <CardContent className="pt-4 space-y-4">
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-4">
            <div className="space-y-1">
              <Label htmlFor="entityName">Entity</Label>
              <Input
                id="entityName"
                placeholder="e.g. Ticket"
                value={entityName}
                onChange={(e) => { setPage(1); setEntityName(e.target.value); }}
              />
            </div>
            <div className="space-y-1">
              <Label>Action</Label>
              <Select value={action || undefined} onValueChange={(v) => { setPage(1); setAction(v === 'all' ? '' : v); }}>
                <SelectTrigger><SelectValue placeholder="All Actions" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Actions</SelectItem>
                  <SelectItem value="Create">Create</SelectItem>
                  <SelectItem value="Update">Update</SelectItem>
                  <SelectItem value="Delete">Delete</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label htmlFor="from">From</Label>
              <Input id="from" type="date" value={from} onChange={(e) => { setPage(1); setFrom(e.target.value); }} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="to">To</Label>
              <Input id="to" type="date" value={to} onChange={(e) => { setPage(1); setTo(e.target.value); }} />
            </div>
          </div>

          {isLoading ? (
            <div className="flex justify-center py-8"><Spinner className="h-8 w-8" /></div>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Timestamp</TableHead>
                    <TableHead>User</TableHead>
                    <TableHead>Action</TableHead>
                    <TableHead>Entity</TableHead>
                    <TableHead>Entity ID</TableHead>
                    <TableHead>IP</TableHead>
                    <TableHead className="w-16">Changes</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data?.items.map((log) => (
                    <TableRow key={log.id}>
                      <TableCell className="whitespace-nowrap">{new Date(log.timestamp).toLocaleString()}</TableCell>
                      <TableCell>{log.userName || '—'}</TableCell>
                      <TableCell><Badge variant={actionVariant[log.action]}>{log.action}</Badge></TableCell>
                      <TableCell className="font-medium">{log.entityName}</TableCell>
                      <TableCell className="font-mono text-xs">{log.entityId.slice(0, 8)}</TableCell>
                      <TableCell>{log.ipAddress || '—'}</TableCell>
                      <TableCell>
                        <Button variant="ghost" size="icon" onClick={() => setViewingChanges(log)} aria-label="View changes">
                          <Eye className="h-4 w-4" />
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                  {data?.items.length === 0 && (
                    <TableRow><TableCell colSpan={7} className="text-center py-8 text-muted-foreground">No audit log entries found</TableCell></TableRow>
                  )}
                </TableBody>
              </Table>

              {totalCount > 0 && (
                <div className="flex items-center justify-between pt-2">
                  <p className="text-sm text-muted-foreground">
                    Showing {(page - 1) * PAGE_SIZE + 1}-{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}
                  </p>
                  <div className="flex gap-2">
                    <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(page - 1)}>Previous</Button>
                    <Button variant="outline" size="sm" disabled={page * PAGE_SIZE >= totalCount} onClick={() => setPage(page + 1)}>Next</Button>
                  </div>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>

      <Dialog open={!!viewingChanges} onOpenChange={() => setViewingChanges(null)}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>{viewingChanges?.entityName} — {viewingChanges?.action}</DialogTitle>
          </DialogHeader>
          <pre className="max-h-96 overflow-auto rounded-md bg-muted p-4 text-xs">
            {viewingChanges ? formatChanges(viewingChanges.changes) : ''}
          </pre>
        </DialogContent>
      </Dialog>
    </div>
  );
}
