import { httpClient } from '@/api/httpClient';

export interface JwtTokenResponse {
  token: string;
}

export interface LoginRequest {
  credentials: string;
  password: string;
  rememberMe: boolean;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  image?: File | null;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

interface MessageResponse {
  message: string;
}

const IDENTITY_BASE_PATH = '/Identity';

const createRegisterPayload = (payload: RegisterRequest): FormData => {
  const formData = new FormData();
  formData.append('username', payload.username);
  formData.append('email', payload.email);
  formData.append('password', payload.password);
  formData.append('firstName', payload.firstName);
  formData.append('lastName', payload.lastName);

  if (payload.image) {
    formData.append('image', payload.image);
  }

  return formData;
};

export const identityApi = {
  async login(payload: LoginRequest): Promise<JwtTokenResponse> {
    const response = await httpClient.post<JwtTokenResponse>(`${IDENTITY_BASE_PATH}/login/`, payload);

    return response.data;
  },

  async register(payload: RegisterRequest): Promise<JwtTokenResponse> {
    const requestPayload = createRegisterPayload(payload);
    const response = await httpClient.post<JwtTokenResponse>(`${IDENTITY_BASE_PATH}/register/`, requestPayload, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });

    return response.data;
  },

  async forgotPassword(payload: ForgotPasswordRequest): Promise<MessageResponse> {
    const response = await httpClient.post<MessageResponse>(`${IDENTITY_BASE_PATH}/forgot-password/`, payload);

    return response.data;
  },

  async resetPassword(payload: ResetPasswordRequest): Promise<MessageResponse> {
    const response = await httpClient.post<MessageResponse>(`${IDENTITY_BASE_PATH}/reset-password/`, payload);

    return response.data;
  },
};
