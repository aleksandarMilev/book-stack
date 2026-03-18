import axios from 'axios';

import { isExpirationIsoExpired } from '@/features/auth/utils/jwtSession';
import { useAuthStore } from '@/store/auth.store';

const rawBaseURL = import.meta.env.VITE_REACT_APP_SERVER_URL?.trim() ?? '';
const baseURL = rawBaseURL.replace(/\/+$/, '');

if (import.meta.env.DEV && typeof window !== 'undefined') {
  if (!baseURL) {
    console.warn(
      '[httpClient] VITE_REACT_APP_SERVER_URL is empty. Requests will use the current browser origin.',
    );
  } else if (/\/\/localhost(?::|\/|$)/i.test(baseURL)) {
    console.warn(
      '[httpClient] VITE_REACT_APP_SERVER_URL points to localhost. In some Windows Docker setups localhost resolves to ::1 and can hang; prefer http://127.0.0.1:8080.',
    );
  } else if (baseURL.includes('host.docker.internal')) {
    console.warn(
      '[httpClient] VITE_REACT_APP_SERVER_URL points to host.docker.internal. In browser-based dev flows this host can be unreachable; prefer http://127.0.0.1:8080.',
    );
  }
}

export const httpClient = axios.create({
  baseURL,
  withCredentials: false,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10_000,
});

httpClient.interceptors.request.use((config) => {
  const session = useAuthStore.getState().session;
  if (!session?.accessToken) {
    return config;
  }

  if (isExpirationIsoExpired(session.expiresAtUtc)) {
    useAuthStore.getState().clearSession();
    return config;
  }

  const requestHeaders = axios.AxiosHeaders.from(config.headers);
  requestHeaders.set('Authorization', `Bearer ${session.accessToken}`);
  config.headers = requestHeaders;

  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      useAuthStore.getState().clearSession();
    }

    return Promise.reject(error);
  },
);
