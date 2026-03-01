import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { equipageApi } from '@/api/equipage';
import type { GroupeCheck } from '@/types/enums';

export const equipageKeys = {
  all: ['equipage'] as const,
  kpi: () => [...equipageKeys.all, 'kpi'] as const,
  alertes: () => [...equipageKeys.all, 'alertes'] as const,
  membres: () => [...equipageKeys.all, 'membres'] as const,
  membreDetail: (id: string) => [...equipageKeys.all, 'membre', id] as const,
  checks: (groupe?: GroupeCheck) => [...equipageKeys.all, 'checks', groupe] as const,
  matrice: (groupe?: GroupeCheck) => [...equipageKeys.all, 'matrice', groupe] as const,
  membresPourCheck: (code: string) => [...equipageKeys.all, 'checks', code, 'membres'] as const,
};

export function useEquipageKpi() {
  return useQuery({
    queryKey: equipageKeys.kpi(),
    queryFn: equipageApi.getKpi,
  });
}

export function useEquipageAlertes() {
  return useQuery({
    queryKey: equipageKeys.alertes(),
    queryFn: equipageApi.getAlertes,
  });
}

export function useEquipageMembres() {
  return useQuery({
    queryKey: equipageKeys.membres(),
    queryFn: equipageApi.getMembres,
  });
}

export function useMembreDetail(id: string | null) {
  return useQuery({
    queryKey: equipageKeys.membreDetail(id!),
    queryFn: () => equipageApi.getMembreDetail(id!),
    enabled: !!id,
  });
}

export function useChecks(groupe?: GroupeCheck) {
  return useQuery({
    queryKey: equipageKeys.checks(groupe),
    queryFn: () => equipageApi.getChecks(groupe),
  });
}

export function useMatrice(groupe?: GroupeCheck) {
  return useQuery({
    queryKey: equipageKeys.matrice(groupe),
    queryFn: () => equipageApi.getMatrice(groupe),
  });
}

export function useMembresPourCheck(code: string | null) {
  return useQuery({
    queryKey: equipageKeys.membresPourCheck(code!),
    queryFn: () => equipageApi.getMembresPourCheck(code!),
    enabled: !!code,
  });
}

export function useImportEquipage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (formData: FormData) => equipageApi.importEquipage(formData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: equipageKeys.all });
    },
  });
}
