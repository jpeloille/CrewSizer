import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  programmeApi,
  type CreateVolPayload,
  type CreateBlocPayload,
  type CreateBlocTypePayload,
  type CreateTypeAvionPayload,
  type CreateSemainePayload,
} from '@/api/programme';
import type { AffectationSemaineDto } from '@/types/scenario';

// ─── Query keys ───────────────────────────────────────────
export const programmeKeys = {
  all: ['programme'] as const,
  vols: () => [...programmeKeys.all, 'vols'] as const,
  blocs: () => [...programmeKeys.all, 'blocs'] as const,
  blocTypes: () => [...programmeKeys.all, 'blocTypes'] as const,
  typesAvion: () => [...programmeKeys.all, 'typesAvion'] as const,
  semaines: () => [...programmeKeys.all, 'semaines'] as const,
  calendrier: (scenarioId: string) =>
    [...programmeKeys.all, 'calendrier', scenarioId] as const,
};

// ─── Vols ─────────────────────────────────────────────────
export function useVols() {
  return useQuery({
    queryKey: programmeKeys.vols(),
    queryFn: programmeApi.getVols,
  });
}

export function useCreateVol() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateVolPayload) => programmeApi.createVol(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.vols() });
      toast.success('Vol cree');
    },
    onError: () => toast.error('Erreur lors de la creation du vol'),
  });
}

export function useUpdateVol() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateVolPayload }) =>
      programmeApi.updateVol(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.vols() });
      toast.success('Vol modifie');
    },
    onError: () => toast.error('Erreur lors de la modification du vol'),
  });
}

export function useDeleteVol() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => programmeApi.deleteVol(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.vols() });
      toast.success('Vol supprime');
    },
    onError: () =>
      toast.error('Impossible de supprimer ce vol (utilise dans un bloc ?)'),
  });
}

// ─── BlocTypes ─────────────────────────────────────────────
export function useBlocTypes() {
  return useQuery({
    queryKey: programmeKeys.blocTypes(),
    queryFn: programmeApi.getBlocTypes,
  });
}

export function useCreateBlocType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateBlocTypePayload) =>
      programmeApi.createBlocType(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.blocTypes() });
      toast.success('Type de bloc cree');
    },
    onError: () => toast.error('Erreur lors de la creation du type de bloc'),
  });
}

export function useUpdateBlocType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: CreateBlocTypePayload;
    }) => programmeApi.updateBlocType(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.blocTypes() });
      toast.success('Type de bloc modifie');
    },
    onError: () =>
      toast.error('Erreur lors de la modification du type de bloc'),
  });
}

export function useDeleteBlocType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => programmeApi.deleteBlocType(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.blocTypes() });
      toast.success('Type de bloc supprime');
    },
    onError: () =>
      toast.error(
        'Impossible de supprimer ce type de bloc (utilise dans un bloc ?)'
      ),
  });
}

// ─── Types avion ──────────────────────────────────────────
export function useTypesAvion() {
  return useQuery({
    queryKey: programmeKeys.typesAvion(),
    queryFn: programmeApi.getTypesAvion,
  });
}

export function useCreateTypeAvion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTypeAvionPayload) =>
      programmeApi.createTypeAvion(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.typesAvion() });
      toast.success('Type avion cree');
    },
    onError: () => toast.error('Erreur lors de la creation du type avion'),
  });
}

export function useUpdateTypeAvion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: CreateTypeAvionPayload;
    }) => programmeApi.updateTypeAvion(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.typesAvion() });
      toast.success('Type avion modifie');
    },
    onError: () =>
      toast.error('Erreur lors de la modification du type avion'),
  });
}

export function useDeleteTypeAvion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => programmeApi.deleteTypeAvion(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.typesAvion() });
      toast.success('Type avion supprime');
    },
    onError: () =>
      toast.error(
        'Impossible de supprimer ce type avion (utilise dans un bloc ?)'
      ),
  });
}

// ─── Blocs ────────────────────────────────────────────────
export function useBlocs() {
  return useQuery({
    queryKey: programmeKeys.blocs(),
    queryFn: programmeApi.getBlocs,
  });
}

export function useCreateBloc() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateBlocPayload) => programmeApi.createBloc(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.blocs() });
      toast.success('Bloc cree');
    },
    onError: () => toast.error('Erreur lors de la creation du bloc'),
  });
}

export function useUpdateBloc() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateBlocPayload }) =>
      programmeApi.updateBloc(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.blocs() });
      toast.success('Bloc modifie');
    },
    onError: () => toast.error('Erreur lors de la modification du bloc'),
  });
}

export function useDeleteBloc() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => programmeApi.deleteBloc(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.blocs() });
      toast.success('Bloc supprime');
    },
    onError: () =>
      toast.error(
        'Impossible de supprimer ce bloc (utilise dans une semaine type ?)'
      ),
  });
}

// ─── Semaines types ───────────────────────────────────────
export function useSemaines() {
  return useQuery({
    queryKey: programmeKeys.semaines(),
    queryFn: programmeApi.getSemaines,
  });
}

export function useCreateSemaine() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateSemainePayload) =>
      programmeApi.createSemaine(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.semaines() });
      toast.success('Semaine type creee');
    },
    onError: () => toast.error('Erreur lors de la creation de la semaine type'),
  });
}

export function useUpdateSemaine() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: CreateSemainePayload;
    }) => programmeApi.updateSemaine(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.semaines() });
      toast.success('Semaine type modifiee');
    },
    onError: () =>
      toast.error('Erreur lors de la modification de la semaine type'),
  });
}

export function useDeleteSemaine() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => programmeApi.deleteSemaine(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: programmeKeys.semaines() });
      toast.success('Semaine type supprimee');
    },
    onError: () =>
      toast.error(
        'Impossible de supprimer cette semaine type (utilisee dans un calendrier ?)'
      ),
  });
}

// ─── Calendrier ───────────────────────────────────────────
export function useCalendrier(scenarioId: string | null) {
  return useQuery({
    queryKey: programmeKeys.calendrier(scenarioId!),
    queryFn: () => programmeApi.getCalendrier(scenarioId!),
    enabled: !!scenarioId,
  });
}

export function useUpdateCalendrier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      scenarioId,
      affectations,
    }: {
      scenarioId: string;
      affectations: AffectationSemaineDto[];
    }) => programmeApi.updateCalendrier(scenarioId, affectations),
    onSuccess: (_, { scenarioId }) => {
      qc.invalidateQueries({
        queryKey: programmeKeys.calendrier(scenarioId),
      });
      toast.success('Calendrier sauvegarde');
    },
    onError: () => toast.error('Erreur lors de la sauvegarde du calendrier'),
  });
}
