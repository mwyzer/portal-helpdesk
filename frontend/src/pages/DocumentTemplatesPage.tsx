import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Plus, RefreshCw, Pencil, Trash2 } from 'lucide-react';

interface DocumentTemplateResponse {
  id: string;
  name: string;
  code: string;
  category: string;
  contentTemplate: string;
  variables: string;
  isActive: boolean;
  createdAt: string;
}

const templateSchema = z.object({
  name: z.string().min(3, 'Min 3 characters'),
  code: z.string().min(2, 'Min 2 characters').max(10),
  category: z.string().min(1, 'Category is required'),
  contentTemplate: z.string().min(10, 'Min 10 characters'),
  variables: z.string(),
});

type TemplateForm = z.infer<typeof templateSchema>;

const categories = ['HR', 'Operasional', 'Umum'];

export function DocumentTemplatesPage() {
  const queryClient = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<DocumentTemplateResponse | null>(null);

  const { data, isLoading } = useQuery<DocumentTemplateResponse[]>({
    queryKey: ['document-templates'],
    queryFn: () => api.get('/document-templates').then((r) => r.data),
  });

  const { register, handleSubmit, reset, setValue, formState: { isSubmitting } } = useForm<TemplateForm>({
    resolver: zodResolver(templateSchema),
  });

  const createMutation = useMutation({
    mutationFn: (formData: TemplateForm) => api.post('/document-templates', formData),
    onSuccess: () => { setShowCreate(false); reset(); queryClient.invalidateQueries({ queryKey: ['document-templates'] }); },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: TemplateForm & { isActive: boolean } }) =>
      api.put(`/document-templates/${id}`, data),
    onSuccess: () => { setEditingTemplate(null); reset(); queryClient.invalidateQueries({ queryKey: ['document-templates'] }); },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => api.delete(`/document-templates/${id}`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['document-templates'] }),
  });

  const onCreate = (formData: TemplateForm) => createMutation.mutate(formData);
  const onUpdate = (formData: TemplateForm) => {
    if (!editingTemplate) return;
    updateMutation.mutate({ id: editingTemplate.id, data: { ...formData, isActive: editingTemplate.isActive } });
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Document Templates</h1>
          <p className="text-muted-foreground">Manage reusable letter templates</p>
        </div>
        <Button onClick={() => { reset({ variables: '[]' }); setShowCreate(true); }}>
          <Plus className="mr-2 h-4 w-4" /> Add Template
        </Button>
      </div>

      <Card>
        <CardHeader className="pb-0 flex-row items-center justify-between">
          <CardTitle>All Templates</CardTitle>
          <Button variant="outline" size="icon" onClick={() => queryClient.invalidateQueries({ queryKey: ['document-templates'] })}>
            <RefreshCw className="h-4 w-4" />
          </Button>
        </CardHeader>
        <CardContent className="pt-4">
          {isLoading ? (
            <div className="flex justify-center py-8"><Spinner className="h-8 w-8" /></div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Code</TableHead>
                  <TableHead>Category</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="w-24">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data?.map((t) => (
                  <TableRow key={t.id}>
                    <TableCell className="font-medium">{t.name}</TableCell>
                    <TableCell><Badge variant="outline">{t.code}</Badge></TableCell>
                    <TableCell><Badge variant="secondary">{t.category}</Badge></TableCell>
                    <TableCell>
                      <Badge variant={t.isActive ? 'success' : 'secondary'}>{t.isActive ? 'Active' : 'Inactive'}</Badge>
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button variant="ghost" size="icon" onClick={() => {
                          reset({
                            name: t.name, code: t.code, category: t.category,
                            contentTemplate: t.contentTemplate, variables: t.variables,
                          });
                          setEditingTemplate(t);
                        }}><Pencil className="h-4 w-4" /></Button>
                        <Button variant="ghost" size="icon" onClick={() => { if (confirm('Delete this template?')) deleteMutation.mutate(t.id); }}>
                          <Trash2 className="h-4 w-4 text-destructive" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
                {data?.length === 0 && (
                  <TableRow><TableCell colSpan={5} className="text-center py-8 text-muted-foreground">No templates found</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Create Template</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onCreate)} className="space-y-4">
            <div className="space-y-2"><Label>Name *</Label><Input {...register('name')} placeholder="Surat Keterangan Kerja" /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label>Code *</Label><Input {...register('code')} placeholder="SKK" maxLength={10} /></div>
              <div className="space-y-2">
                <Label>Category *</Label>
                <Select onValueChange={(v: string) => setValue('category', v)}>
                  <SelectTrigger><SelectValue placeholder="Select" /></SelectTrigger>
                  <SelectContent>
                    {categories.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label>Template Content *</Label>
              <textarea {...register('contentTemplate')} rows={6} className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm" placeholder="Use {variable_name} for placeholders..." />
            </div>
            <div className="space-y-2"><Label>Variables (JSON)</Label><Input {...register('variables')} placeholder='["employee_name","date"]' /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Create Template</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editingTemplate} onOpenChange={() => setEditingTemplate(null)}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader><DialogTitle>Edit Template</DialogTitle></DialogHeader>
          <form onSubmit={handleSubmit(onUpdate)} className="space-y-4">
            <div className="space-y-2"><Label>Name *</Label><Input {...register('name')} /></div>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2"><Label>Code *</Label><Input {...register('code')} /></div>
              <div className="space-y-2">
                <Label>Category *</Label>
                <Select onValueChange={(v: string) => setValue('category', v)} defaultValue={editingTemplate?.category}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {categories.map((c) => <SelectItem key={c} value={c}>{c}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="space-y-2"><Label>Template Content *</Label>
              <textarea {...register('contentTemplate')} rows={6} className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm" />
            </div>
            <div className="space-y-2"><Label>Variables (JSON)</Label><Input {...register('variables')} /></div>
            <DialogFooter><Button type="submit" disabled={isSubmitting}>Save Changes</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
