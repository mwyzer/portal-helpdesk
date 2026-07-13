import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Spinner } from '@/components/ui/spinner';
import { Pencil, Trash2 } from 'lucide-react';

export interface EmployeeTableRow {
  id: string;
  employeeNo: string;
  fullName: string;
  email: string;
  departmentName?: string;
  positionName?: string;
  employmentStatus: string;
}

const statusBadge = (status: string) => {
  const map: Record<string, string> = {
    Active: 'bg-success/10 text-success',
    Inactive: 'bg-muted text-muted-foreground',
    Resigned: 'bg-destructive/10 text-destructive',
    Terminated: 'bg-warning/10 text-warning',
  };
  return <Badge className={map[status] || ''}>{status}</Badge>;
};

interface EmployeeTableProps {
  data: EmployeeTableRow[] | undefined;
  isLoading: boolean;
  emptyMessage?: string;
  onEdit?: (row: EmployeeTableRow) => void;
  onDelete?: (row: EmployeeTableRow) => void;
  showActions?: boolean;
}

export function EmployeeTable({
  data,
  isLoading,
  emptyMessage = 'No employees found',
  onEdit,
  onDelete,
  showActions = true,
}: EmployeeTableProps) {
  if (isLoading) {
    return (
      <div className="flex justify-center py-12">
        <Spinner />
      </div>
    );
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Employee No</TableHead>
          <TableHead>Name</TableHead>
          <TableHead>Email</TableHead>
          <TableHead>Department</TableHead>
          <TableHead>Position</TableHead>
          <TableHead>Status</TableHead>
          {showActions && <TableHead className="w-24">Actions</TableHead>}
        </TableRow>
      </TableHeader>
      <TableBody>
        {data?.map((emp) => (
          <TableRow key={emp.id}>
            <TableCell className="font-mono text-sm">{emp.employeeNo}</TableCell>
            <TableCell className="font-medium">{emp.fullName}</TableCell>
            <TableCell>{emp.email}</TableCell>
            <TableCell>{emp.departmentName || '-'}</TableCell>
            <TableCell>{emp.positionName || '-'}</TableCell>
            <TableCell>{statusBadge(emp.employmentStatus)}</TableCell>
            {showActions && (
              <TableCell>
                <div className="flex gap-1">
                  {onEdit && (
                    <Button variant="ghost" size="icon" onClick={() => onEdit(emp)}>
                      <Pencil className="h-4 w-4" />
                    </Button>
                  )}
                  {onDelete && (
                    <Button variant="ghost" size="icon" onClick={() => onDelete(emp)}>
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  )}
                </div>
              </TableCell>
            )}
          </TableRow>
        ))}
        {(!data || data.length === 0) && (
          <TableRow>
            <TableCell colSpan={showActions ? 7 : 6} className="text-center py-8 text-muted-foreground">
              {emptyMessage}
            </TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  );
}
