import { httpClient } from '@/api/httpClient';

export interface ProfileResponse {
  id: string;
  firstName: string;
  lastName: string;
  imagePath: string;
}

const PROFILE_BASE_PATH = '/Profiles';

export const profileApi = {
  async getMine(): Promise<ProfileResponse> {
    const response = await httpClient.get<ProfileResponse>(`${PROFILE_BASE_PATH}/mine/`);

    return response.data;
  },
};
