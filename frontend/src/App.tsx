import { Routes, Route, Navigate } from 'react-router-dom';
import { AppLayout } from '@/components/layout/AppLayout';
import { ProtectedRoute } from '@/components/layout/ProtectedRoute';
import { LoginPage } from '@/pages/LoginPage';
import { ForgotPasswordPage } from '@/pages/ForgotPasswordPage';
import { ResetPasswordPage } from '@/pages/ResetPasswordPage';
import { DashboardPage } from '@/pages/DashboardPage';
import { UsersPage } from '@/pages/UsersPage';
import { RolesPage } from '@/pages/RolesPage';
import { DepartmentsPage } from '@/pages/DepartmentsPage';
import { MeetingsPage } from '@/pages/MeetingsPage';
import { ActionItemsPage } from '@/pages/ActionItemsPage';
import { DocumentRequestsPage } from '@/pages/DocumentRequestsPage';
import { DocumentTemplatesPage } from '@/pages/DocumentTemplatesPage';
import { ChatPage } from '@/pages/ChatPage';
import { KnowledgeBasePage } from '@/pages/KnowledgeBasePage';

export default function App() {
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
        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="users" element={<UsersPage />} />
        <Route path="roles" element={<RolesPage />} />
        <Route path="departments" element={<DepartmentsPage />} />
        <Route path="meetings" element={<MeetingsPage />} />
        <Route path="action-items" element={<ActionItemsPage />} />
        <Route path="documents/requests" element={<DocumentRequestsPage />} />
        <Route path="documents/templates" element={<DocumentTemplatesPage />} />
        <Route path="ai/chat" element={<ChatPage />} />
        <Route path="knowledge-base" element={<KnowledgeBasePage />} />
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
