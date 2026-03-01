import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { scenariosApi } from '@/api/scenarios';
import type { ScenarioDto } from '@/types/scenario';

export const scenarioKeys = {
  all: ['scenarios'] as const,
  detail: (id: string) => ['scenario', id] as const,
};

export function useScenario(id: string) {
  return useQuery({
    queryKey: scenarioKeys.detail(id),
    queryFn: () => scenariosApi.getById(id),
    enabled: !!id,
  });
}

export function useUpdateScenario() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: ScenarioDto }) =>
      scenariosApi.update(id, data),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: scenarioKeys.detail(variables.id) });
      qc.invalidateQueries({ queryKey: scenarioKeys.all });
      toast.success('Scenario enregistre');
    },
    onError: () => toast.error('Erreur lors de la sauvegarde du scenario'),
  });
}
