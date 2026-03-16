import axios from 'axios';

import { isExpirationIsoExpired } from '@/features/auth/utils/jwtSession';
import { useAuthStore } from '@/store/auth.store';

const baseURL = import.meta.env.VITE_REACT_APP_SERVER_URL ?? '';

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
