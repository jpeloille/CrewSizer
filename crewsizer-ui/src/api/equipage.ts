import apiClient from './client';
import type {
  QualificationMatrixDto,
  MembreDetailDto,
  EquipageKpiDto,
  AlerteQualificationDto,
  DefinitionCheckDto,
  MatriceQualificationsDto,
  ImportEquipageResultDto,
} from '@/types/equipage';
import type { GroupeCheck } from '@/types/enums';

export const equipageApi = {
  getMembres: () =>
    apiClient.get<QualificationMatrixDto>('/equipage/membres').then((r) => r.data),

  getMembreDetail: (id: string) =>
    apiClient.get<MembreDetailDto>(`/equipage/membres/${id}`).then((r) => r.data),

  getKpi: () =>
    apiClient.get<EquipageKpiDto>('/equipage/kpi').then((r) => r.data),

  getAlertes: () =>
    apiClient.get<AlerteQualificationDto[]>('/equipage/alertes').then((r) => r.data),

  getChecks: (groupe?: GroupeCheck) =>
    apiClient
      .get<DefinitionCheckDto[]>('/equipage/checks', {
        params: groupe ? { groupe } : {},
      })
      .then((r) => r.data),

  getMatrice: (groupe?: GroupeCheck) =>
    apiClient
      .get<MatriceQualificationsDto>('/equipage/matrice', {
        params: groupe ? { groupe } : {},
      })
      .then((r) => r.data),

  getMembresPourCheck: (code: string) =>
    apiClient
      .get<AlerteQualificationDto[]>(`/equipage/checks/${code}/membres`)
      .then((r) => r.data),

  importEquipage: (formData: FormData) =>
    apiClient
      .post<ImportEquipageResultDto>('/equipage/import', formData, {
        headers: { 'Content-Type': 'multipart/form-data' },
      })
      .then((r) => r.data),
};
