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
  isActive: boolean;
}

const SELLER_PROFILES_BASE_PATH = '/SellerProfiles';

const normalizePhoneNumber = (phoneNumber: string | undefined): string | undefined =>
  phoneNumber && phoneNumber.trim() ? phoneNumber.trim() : undefined;

export const sellerProfilesApi = {
  async getMine(): Promise<SellerProfileResponse | null> {
    const response = await httpClient.get<SellerProfileResponse | null>(
      `${SELLER_PROFILES_BASE_PATH}/mine/`,
    );

    return response.data;
  },

  async upsertMine(payload: UpsertSellerProfileRequest): Promise<SellerProfileResponse> {
    const response = await httpClient.put<SellerProfileResponse>(
      `${SELLER_PROFILES_BASE_PATH}/mine/`,
      {
        displayName: payload.displayName.trim(),
        phoneNumber: normalizePhoneNumber(payload.phoneNumber),
        supportsOnlinePayment: payload.supportsOnlinePayment,
        supportsCashOnDelivery: payload.supportsCashOnDelivery,
        isActive: payload.isActive,
      },
    );

    return response.data;
  },
};
