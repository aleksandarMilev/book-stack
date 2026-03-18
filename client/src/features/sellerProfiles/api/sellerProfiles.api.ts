import axios from 'axios';

import { httpClient } from '@/api/httpClient';

export interface SellerProfileResponse {
  userId: string;
  displayName: string;
  phoneNumber?: string | null;
  supportsOnlinePayment: boolean;
  supportsCashOnDelivery: boolean;
  isActive: boolean;
  createdOn: string;
  modifiedOn?: string | null;
}

export interface UpsertSellerProfileRequest {
  displayName: string;
  phoneNumber?: string;
  supportsOnlinePayment: boolean;
  supportsCashOnDelivery: boolean;
}

const SELLER_PROFILES_BASE_PATH = '/SellerProfiles';

const normalizePhoneNumber = (phoneNumber: string | undefined): string | undefined =>
  phoneNumber && phoneNumber.trim() ? phoneNumber.trim() : undefined;

export const sellerProfilesApi = {
  async getMine(): Promise<SellerProfileResponse | null> {
    try {
      const response = await httpClient.get<SellerProfileResponse | null>(
        `${SELLER_PROFILES_BASE_PATH}/mine/`,
      );

      if (response.status === 204 || response.data === null) {
        return null;
      }

      return response.data;
    } catch (error: unknown) {
      if (axios.isAxiosError(error) && error.response?.status === 404) {
        return null;
      }

      throw error;
    }
  },

  async upsertMine(payload: UpsertSellerProfileRequest): Promise<SellerProfileResponse> {
    const response = await httpClient.put<SellerProfileResponse>(
      `${SELLER_PROFILES_BASE_PATH}/mine/`,
      {
        displayName: payload.displayName.trim(),
        phoneNumber: normalizePhoneNumber(payload.phoneNumber),
        supportsOnlinePayment: payload.supportsOnlinePayment,
        supportsCashOnDelivery: payload.supportsCashOnDelivery,
      },
    );

    return response.data;
  },
};
