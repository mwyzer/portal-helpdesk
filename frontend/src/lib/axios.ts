import axios from 'axios';

// When frontend and backend share an origin (local dev via Vite proxy, or the Docker/nginx
// setup), this stays empty and requests go out as relative paths. When deployed separately
// (e.g. frontend on Vercel, backend on Render), set VITE_API_URL to the backend's origin.
const apiBaseUrl = import.meta.env.VITE_API_URL ?? '';

const api = axios.create({
  baseURL: `${apiBaseUrl}/api`,
  headers: { 'Content-Type': 'application/json' },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      const refreshToken = localStorage.getItem('refreshToken');
      if (refreshToken) {
        try {
          const { data } = await axios.post(`${apiBaseUrl}/api/auth/refresh-token`, {
            accessToken: localStorage.getItem('accessToken'),
            refreshToken,
          });

          localStorage.setItem('accessToken', data.accessToken);
          localStorage.setItem('refreshToken', data.refreshToken);
          localStorage.setItem('user', JSON.stringify(data.user));

          originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
          return api(originalRequest);
        } catch {
          localStorage.clear();
          window.location.href = '/login';
        }
      } else {
        localStorage.clear();
        window.location.href = '/login';
      }
    }

    return Promise.reject(error);
  },
);

export default api;
