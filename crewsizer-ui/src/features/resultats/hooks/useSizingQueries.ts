import { useMutation } from '@tanstack/react-query';
import { sizingApi } from '@/api/sizing';
import { toast } from 'sonner';

export function useRunSizing() {
  return useMutation({
    mutationFn: (scenarioId: string) => sizingApi.run(scenarioId),
    onSuccess: () => {
      toast.success('Dimensionnement CP-SAT termine');
    },
  });
}
