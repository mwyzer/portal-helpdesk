import axios from 'axios';

// Separate axios instance + localStorage keys from lib/axios.ts's staff client, so a staff
// session and a candidate session can never collide in the same browser -- see the
// CandidatePortal JWT audience isolation in the backend's Program.cs/TokenService.
const apiBaseUrl = import.meta.env.VITE_API_URL ?? '';

const candidatePortalApi = axios.create({
  baseURL: `${apiBaseUrl}/api/candidate-portal`,
  headers: { 'Content-Type': 'application/json' },
});

candidatePortalApi.interceptors.request.use((config) => {
  const token = localStorage.getItem('candidatePortalAccessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

candidatePortalApi.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const refreshToken = localStorage.getItem('candidatePortalRefreshToken');
      if (refreshToken) {
        try {
          const { data } = await axios.post(`${apiBaseUrl}/api/candidate-portal/refresh-token`, {
            accessToken: localStorage.getItem('candidatePortalAccessToken'),
            refreshToken,
          });

          localStorage.setItem('candidatePortalAccessToken', data.accessToken);
          localStorage.setItem('candidatePortalRefreshToken', data.refreshToken);
          localStorage.setItem('candidatePortalProfile', JSON.stringify(data.profile));

          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
          return candidatePortalApi(originalRequest);
        } catch {
          localStorage.removeItem('candidatePortalAccessToken');
          localStorage.removeItem('candidatePortalRefreshToken');
          localStorage.removeItem('candidatePortalProfile');
          window.location.href = '/portal/login';
        }
      } else {
        localStorage.removeItem('candidatePortalAccessToken');
        localStorage.removeItem('candidatePortalRefreshToken');
        localStorage.removeItem('candidatePortalProfile');
        window.location.href = '/portal/login';
      }
    }

    return Promise.reject(error);
  },
);

export default candidatePortalApi;
