import { httpClient } from '@/api/httpClient';
import { resolveAssetUrl } from '@/api/utils/assetUrl';

interface ProfileApiModel {
  id: string;
  firstName: string;
  lastName: string;
  imagePath: string;
}

export interface ProfileResponse {
  id: string;
  firstName: string;
  lastName: string;
  imagePath: string;
  imageUrl: string;
}

export interface EditProfileRequest {
  firstName: string;
  lastName: string;
  image?: File | null;
  removeImage?: boolean;
}

const PROFILE_BASE_PATH = '/Profiles';

const mapProfile = (profile: ProfileApiModel): ProfileResponse => ({
  id: profile.id,
  firstName: profile.firstName,
  lastName: profile.lastName,
  imagePath: profile.imagePath,
  imageUrl: resolveAssetUrl(profile.imagePath),
});

const createEditProfilePayload = (payload: EditProfileRequest): FormData => {
  const formData = new FormData();
  formData.append('firstName', payload.firstName);
  formData.append('lastName', payload.lastName);
  formData.append('removeImage', String(Boolean(payload.removeImage)));

  if (payload.image) {
    formData.append('image', payload.image);
  }

  return formData;
};

export const profileApi = {
  async getMine(): Promise<ProfileResponse> {
    const response = await httpClient.get<ProfileApiModel>(`${PROFILE_BASE_PATH}/mine/`);

    return mapProfile(response.data);
  },

  async updateMine(payload: EditProfileRequest): Promise<void> {
    await httpClient.put(PROFILE_BASE_PATH, createEditProfilePayload(payload), {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
};
