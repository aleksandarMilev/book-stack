import { httpClient } from '@/api/httpClient';
import type { AdminStatistics } from '@/features/admin/types/admin.types';

const ADMIN_STATISTICS_BASE_PATH = '/Admin/Statistics';

export const adminStatisticsApi = {
  async getStatistics(): Promise<AdminStatistics> {
    const response = await httpClient.get<AdminStatistics>(`${ADMIN_STATISTICS_BASE_PATH}/`);

    return response.data;
  },
};
