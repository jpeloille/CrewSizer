import apiClient from './client';
import type { ScenarioDto, ScenarioListItemDto } from '@/types/scenario';

export const scenariosApi = {
  getAll: () =>
    apiClient.get<ScenarioListItemDto[]>('/scenarios').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ScenarioDto>(`/scenarios/${id}`).then((r) => r.data),

  create: (data: { nom: string; description?: string }) =>
    apiClient.post<ScenarioDto>('/scenarios', data).then((r) => r.data),

  update: (id: string, data: ScenarioDto) =>
    apiClient.put<ScenarioDto>(`/scenarios/${id}`, data).then((r) => r.data),

  delete: (id: string) => apiClient.delete(`/scenarios/${id}`),

  clone: (id: string, nouveauNom: string) =>
    apiClient
      .post<ScenarioDto>(`/scenarios/${id}/clone`, { nouveauNom })
      .then((r) => r.data),
};
