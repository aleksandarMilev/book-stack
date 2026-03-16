import { isAxiosError } from 'axios';

import type { ApiErrorPayload } from '@/api/types/api.types';

export function getApiErrorMessage(error: unknown, fallbackMessage: string): string {
  if (!isAxiosError<ApiErrorPayload>(error)) {
    return fallbackMessage;
  }

  const apiMessage = error.response?.data?.errorMessage;
  if (apiMessage) {
    return apiMessage;
  }

  const validationErrors = error.response?.data?.errors;
  if (validationErrors) {
    const firstErrorList = Object.values(validationErrors)[0];
    if (firstErrorList?.[0]) {
      return firstErrorList[0];
    }
  }

  return fallbackMessage;
}
