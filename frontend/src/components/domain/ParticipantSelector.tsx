import { useState, useEffect, useRef } from 'react';
import { useQuery } from '@tanstack/react-query';
import api from '@/lib/axios';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { X, Search, Users as UsersIcon } from 'lucide-react';

interface Employee {
  id: string;
  fullName: string;
  email?: string;
}

interface ParticipantSelectorProps {
  selectedIds: string[];
  onChange: (ids: string[]) => void;
  excludeIds?: string[];
  placeholder?: string;
}

export function ParticipantSelector({
  selectedIds,
  onChange,
  excludeIds = [],
  placeholder = 'Search employees...',
}: ParticipantSelectorProps) {
  const [query, setQuery] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const wrapperRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  const { data: employeesData } = useQuery<{ items: Employee[] }>({
    queryKey: ['employees-dropdown'],
    queryFn: () => api.get('/employees', { params: { pageSize: 200 } }).then((r) => r.data),
  });

  const allEmployees = employeesData?.items ?? [];

  // Filter: exclude already selected + excluded IDs, and match search query
  const available = allEmployees.filter(
    (e) =>
      !selectedIds.includes(e.id) &&
      !excludeIds.includes(e.id) &&
      (query === '' ||
        e.fullName.toLowerCase().includes(query.toLowerCase()) ||
        e.email?.toLowerCase().includes(query.toLowerCase()))
  );

  // Selected employee details
  const selectedEmployees = allEmployees.filter((e) => selectedIds.includes(e.id));

  // Close dropdown on outside click
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const addEmployee = (employee: Employee) => {
    onChange([...selectedIds, employee.id]);
    setQuery('');
    inputRef.current?.focus();
  };

  const removeEmployee = (employeeId: string) => {
    onChange(selectedIds.filter((id) => id !== employeeId));
  };

  return (
    <div ref={wrapperRef} className="relative">
      {/* Selected badges */}
      {selectedEmployees.length > 0 && (
        <div className="flex flex-wrap gap-1.5 mb-2">
          {selectedEmployees.map((emp) => (
            <Badge key={emp.id} variant="secondary" className="gap-1 pr-1">
              {emp.fullName}
              <button
                type="button"
                onClick={() => removeEmployee(emp.id)}
                className="ml-0.5 rounded-full hover:bg-muted p-0.5"
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}

      {/* Search input */}
      <div className="relative">
        <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
        <Input
          ref={inputRef}
          type="text"
          placeholder={selectedEmployees.length > 0 ? 'Add more...' : placeholder}
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setIsOpen(true);
          }}
          onFocus={() => setIsOpen(true)}
          className="pl-8"
        />
      </div>

      {/* Dropdown */}
      {isOpen && (
        <div className="absolute z-50 mt-1 w-full rounded-md border bg-background shadow-lg max-h-48 overflow-y-auto">
          {available.length === 0 ? (
            <div className="flex items-center justify-center gap-2 px-3 py-6 text-sm text-muted-foreground">
              <UsersIcon className="h-4 w-4" />
              {query ? 'No employees match your search' : 'All employees selected'}
            </div>
          ) : (
            available.map((emp) => (
              <button
                key={emp.id}
                type="button"
                className="w-full text-left px-3 py-2 text-sm hover:bg-accent hover:text-accent-foreground transition-colors flex items-center justify-between"
                onClick={() => addEmployee(emp)}
              >
                <span>{emp.fullName}</span>
                {emp.email && (
                  <span className="text-xs text-muted-foreground">{emp.email}</span>
                )}
              </button>
            ))
          )}
        </div>
      )}
    </div>
  );
}
