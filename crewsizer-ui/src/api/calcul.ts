import apiClient from './client';
import type { CalculSnapshotDto, CalculSnapshotListItemDto } from '@/types/calcul';

export const calculApi = {
  getSnapshots: (scenarioId?: string) =>
    apiClient
      .get<CalculSnapshotListItemDto[]>('/calcul/snapshots', {
        params: scenarioId ? { scenarioId } : {},
      })
      .then((r) => r.data),

  getSnapshot: (id: string) =>
    apiClient.get<CalculSnapshotDto>(`/calcul/snapshots/${id}`).then((r) => r.data),

  runCalcul: (scenarioId: string) =>
    apiClient.post<CalculSnapshotDto>('/calcul/run', { scenarioId }).then((r) => r.data),
};
