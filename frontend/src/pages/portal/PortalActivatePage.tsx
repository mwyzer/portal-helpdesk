import { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useCandidatePortalAuthStore } from '@/store/candidatePortalAuthStore';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { KeyRound } from 'lucide-react';

const activateSchema = z
  .object({
    newPassword: z.string().min(8, 'Must be at least 8 characters'),
    confirmPassword: z.string(),
  })
  .refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  });

type ActivateForm = z.infer<typeof activateSchema>;

export function PortalActivatePage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const setupToken = searchParams.get('token') ?? '';
  const { activate } = useCandidatePortalAuthStore();
  const [error, setError] = useState('');

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<ActivateForm>({ resolver: zodResolver(activateSchema) });

  const onSubmit = async (data: ActivateForm) => {
    try {
      setError('');
      await activate(setupToken, data.newPassword);
      navigate('/portal/status');
    } catch (err: unknown) {
      const resp = (err as { response?: { data?: { message?: string; error?: string } } })?.response?.data;
      const msg = resp?.message ?? resp?.error ?? 'This activation link is invalid or has expired.';
      setError(msg);
    }
  };

  if (!setupToken) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
        <Card className="w-full max-w-md">
          <CardContent className="pt-6 text-center text-sm text-destructive">
            This activation link is missing its token. Please use the link from your invite email.
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1 text-center">
          <div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-xl bg-primary">
            <KeyRound className="h-6 w-6 text-primary-foreground" />
          </div>
          <CardTitle className="text-2xl">Set Your Password</CardTitle>
          <CardDescription>Create a password to access your candidate portal</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            {error && (
              <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>
            )}
            <div className="space-y-2">
              <Label htmlFor="newPassword">New Password</Label>
              <Input id="newPassword" type="password" placeholder="••••••••" {...register('newPassword')} aria-invalid={!!errors.newPassword} />
              {errors.newPassword && <p role="alert" className="text-xs text-destructive">{errors.newPassword.message}</p>}
            </div>
            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Confirm Password</Label>
              <Input id="confirmPassword" type="password" placeholder="••••••••" {...register('confirmPassword')} aria-invalid={!!errors.confirmPassword} />
              {errors.confirmPassword && <p role="alert" className="text-xs text-destructive">{errors.confirmPassword.message}</p>}
            </div>
            <Button type="submit" className="w-full" disabled={isSubmitting}>
              {isSubmitting && <Spinner className="mr-2" />}
              Activate Account
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
