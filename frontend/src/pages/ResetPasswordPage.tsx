import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import api from '@/lib/axios';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Spinner } from '@/components/ui/spinner';
import { CheckCircle } from 'lucide-react';

const schema = z
  .object({
    email: z.string().email(),
    token: z.string().min(1, 'Token is required'),
    newPassword: z.string().min(8, 'Minimum 8 characters'),
    confirmPassword: z.string(),
  })
  .refine((d) => d.newPassword === d.confirmPassword, { message: 'Passwords do not match', path: ['confirmPassword'] });

export function ResetPasswordPage() {
  const navigate = useNavigate();
  const [success, setSuccess] = useState(false);
  const [error, setError] = useState('');

  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<z.infer<typeof schema>>({ resolver: zodResolver(schema) });

  const onSubmit = async (data: z.infer<typeof schema>) => {
    try {
      setError('');
      await api.post('/auth/reset-password', data);
      setSuccess(true);
    } catch {
      setError('Reset failed. The link may be expired.');
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-muted/30 p-4">
      <Card className="w-full max-w-md">
        <CardHeader className="space-y-1 text-center">
          <CardTitle className="text-2xl">Set new password</CardTitle>
          <CardDescription>Enter the token from your email and a new password</CardDescription>
        </CardHeader>
        <CardContent>
          {success ? (
            <div className="text-center space-y-4">
              <CheckCircle className="mx-auto h-12 w-12 text-success" />
              <p className="text-sm text-muted-foreground">Password reset successful!</p>
              <Button onClick={() => navigate('/login')} className="w-full">Sign In</Button>
            </div>
          ) : (
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              {error && <div className="rounded-md bg-destructive/10 p-3 text-sm text-destructive">{error}</div>}
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" {...register('email')} aria-invalid={!!errors.email} aria-describedby={errors.email ? 'email-error' : undefined} />
                {errors.email && <p id="email-error" role="alert" className="text-xs text-destructive">{String(errors.email.message)}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="token">Reset Token</Label>
                <Input id="token" {...register('token')} aria-invalid={!!errors.token} aria-describedby={errors.token ? 'token-error' : undefined} />
                {errors.token && <p id="token-error" role="alert" className="text-xs text-destructive">{String(errors.token.message)}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="newPassword">New Password</Label>
                <Input id="newPassword" type="password" {...register('newPassword')} aria-invalid={!!errors.newPassword} aria-describedby={errors.newPassword ? 'newPassword-error' : undefined} />
                {errors.newPassword && <p id="newPassword-error" role="alert" className="text-xs text-destructive">{String(errors.newPassword.message)}</p>}
              </div>
              <div className="space-y-2">
                <Label htmlFor="confirmPassword">Confirm Password</Label>
                <Input id="confirmPassword" type="password" {...register('confirmPassword')} aria-invalid={!!errors.confirmPassword} aria-describedby={errors.confirmPassword ? 'confirmPassword-error' : undefined} />
                {errors.confirmPassword && <p id="confirmPassword-error" role="alert" className="text-xs text-destructive">{String(errors.confirmPassword.message)}</p>}
              </div>
              <Button type="submit" className="w-full" disabled={isSubmitting}>
                {isSubmitting ? <Spinner className="mr-2" /> : null} Reset Password
              </Button>
            </form>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
