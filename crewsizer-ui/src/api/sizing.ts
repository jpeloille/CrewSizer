import apiClient from './client';
import type { SolveProgress } from '@/types/sizing';

export const sizingApi = {
  start: (scenarioId: string) =>
    apiClient
      .post<{ solveId: string }>('/sizing/run', { scenarioId })
      .then((r) => r.data),

  progress: (solveId: string) =>
    apiClient
      .get<SolveProgress>(`/sizing/progress/${solveId}`)
      .then((r) => r.data),
};
