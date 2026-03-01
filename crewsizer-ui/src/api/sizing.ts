import apiClient from './client';
import type { CombinedSizingResult } from '@/types/sizing';

export const sizingApi = {
  run: (scenarioId: string) =>
    apiClient
      .post<CombinedSizingResult>('/sizing/run', { scenarioId })
      .then((r) => r.data),
};
