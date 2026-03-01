import apiClient from './client';
import type { VolDto } from '@/types/vol';
import type { BlocVolDto, EtapeVolDto } from '@/types/bloc';
import type { BlocTypeDto } from '@/types/blocType';
import type { TypeAvionDto } from '@/types/typeAvion';
import type { SemaineTypeDto, BlocPlacementDto } from '@/types/semaine';
import type { CalendrierDto } from '@/types/calendrier';
import type { AffectationSemaineDto } from '@/types/scenario';

// ─── Payloads ─────────────────────────────────────────────
export interface CreateVolPayload {
  numero: string;
  depart: string;
  arrivee: string;
  heureDepart: string;
  heureArrivee: string;
  mh: boolean;
}

export interface CreateBlocPayload {
  code: string;
  sequence: number;
  jour: string;
  periode: string;
  debutDP: string;
  finDP: string;
  debutFDP: string;
  finFDP: string;
  etapes: EtapeVolDto[];
  blocTypeId?: string;
  typeAvionId: string;
}

export interface CreateBlocTypePayload {
  code: string;
  libelle: string;
  debutPlage: string;
  finPlage: string;
  fdpMax: number;
  hauteSaison: boolean;
}

export interface CreateTypeAvionPayload {
  code: string;
  libelle: string;
  nbCdb: number;
  nbOpl: number;
  nbCc: number;
  nbPnc: number;
}

export interface CreateSemainePayload {
  reference: string;
  saison: string;
  placements: BlocPlacementDto[];
}

// ─── API ──────────────────────────────────────────────────
export const programmeApi = {
  // Vols
  getVols: () =>
    apiClient.get<VolDto[]>('/vols').then((r) => r.data),
  createVol: (data: CreateVolPayload) =>
    apiClient.post<VolDto>('/vols', data).then((r) => r.data),
  updateVol: (id: string, data: CreateVolPayload) =>
    apiClient.put<VolDto>(`/vols/${id}`, data).then((r) => r.data),
  deleteVol: (id: string) =>
    apiClient.delete(`/vols/${id}`),

  // Blocs
  getBlocs: () =>
    apiClient.get<BlocVolDto[]>('/blocs').then((r) => r.data),
  createBloc: (data: CreateBlocPayload) =>
    apiClient.post<BlocVolDto>('/blocs', data).then((r) => r.data),
  updateBloc: (id: string, data: CreateBlocPayload) =>
    apiClient.put<BlocVolDto>(`/blocs/${id}`, data).then((r) => r.data),
  deleteBloc: (id: string) =>
    apiClient.delete(`/blocs/${id}`),

  // Semaines types
  getSemaines: () =>
    apiClient.get<SemaineTypeDto[]>('/semaines').then((r) => r.data),
  createSemaine: (data: CreateSemainePayload) =>
    apiClient.post<SemaineTypeDto>('/semaines', data).then((r) => r.data),
  updateSemaine: (id: string, data: CreateSemainePayload) =>
    apiClient.put<SemaineTypeDto>(`/semaines/${id}`, data).then((r) => r.data),
  deleteSemaine: (id: string) =>
    apiClient.delete(`/semaines/${id}`),

  // BlocTypes
  getBlocTypes: () =>
    apiClient.get<BlocTypeDto[]>('/bloc-types').then((r) => r.data),
  createBlocType: (data: CreateBlocTypePayload) =>
    apiClient.post<BlocTypeDto>('/bloc-types', data).then((r) => r.data),
  updateBlocType: (id: string, data: CreateBlocTypePayload) =>
    apiClient.put<BlocTypeDto>(`/bloc-types/${id}`, data).then((r) => r.data),
  deleteBlocType: (id: string) =>
    apiClient.delete(`/bloc-types/${id}`),

  // Types avion
  getTypesAvion: () =>
    apiClient.get<TypeAvionDto[]>('/types-avion').then((r) => r.data),
  createTypeAvion: (data: CreateTypeAvionPayload) =>
    apiClient.post<TypeAvionDto>('/types-avion', data).then((r) => r.data),
  updateTypeAvion: (id: string, data: CreateTypeAvionPayload) =>
    apiClient.put<TypeAvionDto>(`/types-avion/${id}`, data).then((r) => r.data),
  deleteTypeAvion: (id: string) =>
    apiClient.delete(`/types-avion/${id}`),

  // Calendrier
  getCalendrier: (scenarioId: string) =>
    apiClient.get<CalendrierDto>(`/calendrier/${scenarioId}`).then((r) => r.data),
  updateCalendrier: (scenarioId: string, affectations: AffectationSemaineDto[]) =>
    apiClient
      .put<CalendrierDto>(`/calendrier/${scenarioId}`, { affectations })
      .then((r) => r.data),
};
