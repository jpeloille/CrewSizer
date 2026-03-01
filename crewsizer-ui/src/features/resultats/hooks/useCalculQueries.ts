import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { calculApi } from '@/api/calcul';
import { toast } from 'sonner';

export const calculKeys = {
  all: ['calcul'] as const,
  snapshots: () => [...calculKeys.all, 'snapshots'] as const,
  snapshotsByScenario: (scenarioId: string) =>
    [...calculKeys.all, 'snapshots', scenarioId] as const,
  snapshot: (id: string) => [...calculKeys.all, 'snapshot', id] as const,
};

export function useSnapshots(scenarioId?: string) {
  return useQuery({
    queryKey: scenarioId
      ? calculKeys.snapshotsByScenario(scenarioId)
      : calculKeys.snapshots(),
    queryFn: () => calculApi.getSnapshots(scenarioId),
    enabled: !!scenarioId,
  });
}

export function useSnapshot(id: string | null) {
  return useQuery({
    queryKey: calculKeys.snapshot(id!),
    queryFn: () => calculApi.getSnapshot(id!),
    enabled: !!id,
  });
}

export function useRunCalcul() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (scenarioId: string) => calculApi.runCalcul(scenarioId),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: calculKeys.all });
      toast.success('Calcul termine avec succes');
      return data;
    },
    onError: () => {
      // Error propagated to component via runCalcul.error / runCalcul.isError
      // ErrorPanel will display the structured error
    },
  });
}
