import { Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/components/layout/AppLayout';
import { ProtectedRoute } from '@/components/layout/ProtectedRoute';
import { RoleGuard } from '@/components/layout/RoleGuard';
import { LoginPage } from '@/pages/LoginPage';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage';
import { ResetPasswordPage } from '@/pages/ResetPasswordPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { UsersPage } from '@/pages/UsersPage';
import { RolesPage } from '@/pages/RolesPage';
import { DepartmentsPage } from '@/pages/DepartmentsPage';
import { MeetingsPage } from '@/pages/MeetingsPage';
import { MeetingDetailPage } from '@/pages/MeetingDetailPage';
import { ActionItemsPage } from '@/pages/ActionItemsPage';
import { DocumentRequestsPage } from '@/pages/DocumentRequestsPage';
import { DocumentTemplatesPage } from '@/pages/DocumentTemplatesPage';
import { ChatPage } from '@/pages/ChatPage';
import { ChatSessionPage } from '@/pages/ChatSessionPage';
import { ConversationListPage } from '@/pages/ConversationListPage';
import { KnowledgeBasePage } from '@/pages/KnowledgeBasePage';
import { KnowledgeDocumentDetailPage } from '@/pages/KnowledgeDocumentDetailPage';
import { EmployeesPage } from '@/pages/EmployeesPage';
import { LeaveTypesPage } from '@/pages/LeaveTypesPage';
import { LeaveRequestsPage } from '@/pages/LeaveRequestsPage';
import { LeaveApprovalsPage } from '@/pages/LeaveApprovalsPage';
import { NotificationCenterPage } from '@/pages/NotificationCenterPage';
import { TicketsPage } from '@/pages/TicketsPage';
import { TicketDetailPage } from '@/pages/TicketDetailPage';
import { TicketCategoriesPage } from '@/pages/TicketCategoriesPage';
import { EscalationsPage } from '@/pages/EscalationsPage';
import { AgentAssignmentsPage } from '@/pages/AgentAssignmentsPage';
import { VacanciesPage } from '@/pages/VacanciesPage';
import { CandidatesPage } from '@/pages/CandidatesPage';
import { CandidateDetailPage } from '@/pages/CandidateDetailPage';
import { InterviewsPage } from '@/pages/InterviewsPage';

// ── Role aliases for route guards ─────────────────

const Admin    = ['SuperAdmin', 'HRD'];
const Manager  = ['SuperAdmin', 'HRD', 'Manager'];
const Secretary = ['SuperAdmin', 'HRD', 'Manager', 'Secretary'];
const Agent    = ['SuperAdmin', 'HRD', 'Manager', 'Agent'];

export function App() {
  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/forgot-password" element={<ForgotPasswordPage />} />
      <Route path="/reset-password" element={<ResetPasswordPage />} />

      {/* Protected */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />

        {/* ── All authenticated users ── */}
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="leave-requests" element={<LeaveRequestsPage />} />
        <Route path="ai/chat" element={<ChatPage />} />
        <Route path="ai/chat/:sessionId" element={<ChatSessionPage />} />
        <Route path="knowledge-base" element={<KnowledgeBasePage />} />
        <Route path="notifications" element={<NotificationCenterPage />} />

        {/* ── Ticketing (all authenticated) ── */}
        <Route path="tickets" element={<TicketsPage />} />
        <Route path="tickets/:id" element={<TicketDetailPage />} />

        {/* ── Admin only ── */}
        <Route path="users" element={<RoleGuard allowedRoles={Admin}><UsersPage /></RoleGuard>} />
        <Route path="roles" element={<RoleGuard allowedRoles={Admin}><RolesPage /></RoleGuard>} />
        <Route path="departments" element={<RoleGuard allowedRoles={Admin}><DepartmentsPage /></RoleGuard>} />
        <Route path="leave-types" element={<RoleGuard allowedRoles={Admin}><LeaveTypesPage /></RoleGuard>} />
        <Route path="ai/conversations" element={<RoleGuard allowedRoles={Admin}><ConversationListPage /></RoleGuard>} />
        <Route path="knowledge-base/:id" element={<RoleGuard allowedRoles={Admin}><KnowledgeDocumentDetailPage /></RoleGuard>} />
        <Route path="tickets/categories" element={<RoleGuard allowedRoles={Admin}><TicketCategoriesPage /></RoleGuard>} />
        <Route path="tickets/agents" element={<RoleGuard allowedRoles={Admin}><AgentAssignmentsPage /></RoleGuard>} />

        {/* ── Admin + Manager ── */}
        <Route path="employees" element={<RoleGuard allowedRoles={Manager}><EmployeesPage /></RoleGuard>} />
        <Route path="leave-approvals" element={<RoleGuard allowedRoles={Manager}><LeaveApprovalsPage /></RoleGuard>} />
        <Route path="tickets/escalations" element={<RoleGuard allowedRoles={Manager}><EscalationsPage /></RoleGuard>} />

        {/* ── Admin + Manager + Secretary ── */}
        <Route path="meetings" element={<RoleGuard allowedRoles={Secretary}><MeetingsPage /></RoleGuard>} />
        <Route path="meetings/:id" element={<RoleGuard allowedRoles={Secretary}><MeetingDetailPage /></RoleGuard>} />
        <Route path="action-items" element={<RoleGuard allowedRoles={Secretary}><ActionItemsPage /></RoleGuard>} />
        <Route path="documents/requests" element={<RoleGuard allowedRoles={Secretary}><DocumentRequestsPage /></RoleGuard>} />
        <Route path="documents/templates" element={<RoleGuard allowedRoles={Secretary}><DocumentTemplatesPage /></RoleGuard>} />

        {/* ── Admin + Manager (Recruitment) ── */}
        <Route path="recruitment/vacancies" element={<RoleGuard allowedRoles={Manager}><VacanciesPage /></RoleGuard>} />
        <Route path="recruitment/candidates" element={<RoleGuard allowedRoles={Manager}><CandidatesPage /></RoleGuard>} />
        <Route path="recruitment/candidates/:id" element={<RoleGuard allowedRoles={Manager}><CandidateDetailPage /></RoleGuard>} />
        <Route path="recruitment/interviews" element={<RoleGuard allowedRoles={Manager}><InterviewsPage /></RoleGuard>} />
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
