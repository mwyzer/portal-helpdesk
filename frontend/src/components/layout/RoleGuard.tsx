import { Navigate } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';

interface RoleGuardProps {
  children: React.ReactNode;
  /** Allowed roles for this route */
  allowedRoles: string[];
}

/**
 * Guards routes by both authentication and role.
 * Falls back to ProtectedRoute for auth check, then verifies role membership.
 * Redirects to /dashboard if the user lacks the required role.
 */
export function RoleGuard({ children, allowedRoles }: RoleGuardProps) {
  const { isAuthenticated, user } = useAuthStore();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const hasRole = user?.roles?.some((r) => allowedRoles.includes(r));
  if (!hasRole) {
    return <Navigate to="/dashboard" replace />;
  }

  return <>{children}</>;
}
