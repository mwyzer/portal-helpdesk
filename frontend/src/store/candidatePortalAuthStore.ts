import { create } from 'zustand';
import candidatePortalApi from '@/lib/candidatePortalApi';

interface CandidatePortalProfile {
  candidateId: string;
  fullName: string;
  email: string;
}

interface CandidatePortalAuthState {
  profile: CandidatePortalProfile | null;
  accessToken: string | null;
  refreshToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  activate: (setupToken: string, newPassword: string) => Promise<void>;
  logout: () => Promise<void>;
}

function readStoredProfile(): CandidatePortalProfile | null {
  const raw = localStorage.getItem('candidatePortalProfile');
  if (!raw) return null;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

function applyAuthResponse(data: { accessToken: string; refreshToken: string; profile: CandidatePortalProfile }) {
  localStorage.setItem('candidatePortalAccessToken', data.accessToken);
  localStorage.setItem('candidatePortalRefreshToken', data.refreshToken);
  localStorage.setItem('candidatePortalProfile', JSON.stringify(data.profile));
}

// Reads persisted state synchronously at store creation (same fix applied to authStore.ts
// after the 2026-08-04 bug where `user` stayed null forever on a hard page reload).
export const useCandidatePortalAuthStore = create<CandidatePortalAuthState>((set) => ({
  profile: readStoredProfile(),
  accessToken: localStorage.getItem('candidatePortalAccessToken'),
  refreshToken: localStorage.getItem('candidatePortalRefreshToken'),
  isAuthenticated: !!localStorage.getItem('candidatePortalAccessToken'),
  isLoading: false,

  login: async (email: string, password: string) => {
    set({ isLoading: true });
    try {
      const { data } = await candidatePortalApi.post('/login', { email, password });
      applyAuthResponse(data);
      set({ profile: data.profile, accessToken: data.accessToken, refreshToken: data.refreshToken, isAuthenticated: true, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  activate: async (setupToken: string, newPassword: string) => {
    set({ isLoading: true });
    try {
      const { data } = await candidatePortalApi.post('/activate', { setupToken, newPassword });
      applyAuthResponse(data);
      set({ profile: data.profile, accessToken: data.accessToken, refreshToken: data.refreshToken, isAuthenticated: true, isLoading: false });
    } catch (error) {
      set({ isLoading: false });
      throw error;
    }
  },

  logout: async () => {
    try {
      const refreshToken = localStorage.getItem('candidatePortalRefreshToken');
      if (refreshToken) {
        await candidatePortalApi.post('/logout', JSON.stringify(refreshToken), {
          headers: { 'Content-Type': 'application/json' },
        });
      }
    } finally {
      localStorage.removeItem('candidatePortalAccessToken');
      localStorage.removeItem('candidatePortalRefreshToken');
      localStorage.removeItem('candidatePortalProfile');
      set({ profile: null, accessToken: null, refreshToken: null, isAuthenticated: false });
    }
  },
}));
