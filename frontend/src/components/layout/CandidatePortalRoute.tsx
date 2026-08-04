import { Navigate } from 'react-router-dom';
import { useCandidatePortalAuthStore } from '@/store/candidatePortalAuthStore';

/**
 * Guards candidate portal routes. There's only one "role" in this portal (a candidate), so
 * unlike the staff-side RoleGuard this only checks authentication, not role membership.
 */
export function CandidatePortalRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useCandidatePortalAuthStore((s) => s.isAuthenticated);
  if (!isAuthenticated) {
    return <Navigate to="/portal/login" replace />;
  }
  return <>{children}</>;
}
