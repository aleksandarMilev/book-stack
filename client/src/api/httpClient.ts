import axios from 'axios';

const baseURL = import.meta.env.VITE_REACT_APP_SERVER_URL ?? '';

export const httpClient = axios.create({
  baseURL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 10_000,
});
