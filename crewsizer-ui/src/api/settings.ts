import apiClient from './client';

export interface AppSettingDto {
  key: string;
  value: string;
  description: string | null;
}

export const settingsApi = {
  getAll: () =>
    apiClient.get<AppSettingDto[]>('/settings').then((r) => r.data),

  get: (key: string) =>
    apiClient.get<AppSettingDto>(`/settings/${encodeURIComponent(key)}`).then((r) => r.data),

  update: (key: string, value: string) =>
    apiClient
      .put<AppSettingDto>(`/settings/${encodeURIComponent(key)}`, { value })
      .then((r) => r.data),
};
