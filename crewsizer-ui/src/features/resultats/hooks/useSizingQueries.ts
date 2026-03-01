import { useState, useCallback, useEffect } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { sizingApi } from '@/api/sizing';
import { toast } from 'sonner';
import type { CombinedSizingResult, SolveProgress } from '@/types/sizing';

export function useRunSizing() {
  const [solveId, setSolveId] = useState<string | null>(null);
  const [result, setResult] = useState<CombinedSizingResult | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Mutation pour démarrer le solve (POST → 202 + solveId)
  const startMutation = useMutation({
    mutationFn: (scenarioId: string) => sizingApi.start(scenarioId),
    onSuccess: (data) => {
      setResult(null);
      setError(null);
      setSolveId(data.solveId);
    },
    onError: () => {
      toast.error('Erreur lors du lancement du dimensionnement');
    },
  });

  // Polling de progression toutes les 2s tant que solveId est défini
  const progressQuery = useQuery({
    queryKey: ['sizing', 'progress', solveId],
    queryFn: () => sizingApi.progress(solveId!),
    enabled: !!solveId,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      if (status === 'completed' || status === 'error') return false;
      return 2000;
    },
  });

  // Réagir aux changements de progression
  useEffect(() => {
    const data = progressQuery.data;
    if (!data) return;

    if (data.status === 'completed' && data.result) {
      setResult(data.result);
      setSolveId(null);
      toast.success('Dimensionnement CP-SAT termine');
    } else if (data.status === 'error') {
      setError(data.errorMessage ?? 'Erreur inconnue');
      setSolveId(null);
      toast.error(data.errorMessage ?? 'Erreur du solver');
    }
  }, [progressQuery.data]);

  const start = useCallback(
    (scenarioId: string) => startMutation.mutate(scenarioId),
    [startMutation],
  );

  const isStarting = startMutation.isPending;
  const progress: SolveProgress | null = progressQuery.data ?? null;

  return {
    start,
    isRunning: !!solveId && !isStarting,
    isStarting,
    isPending: isStarting || !!solveId,
    progress,
    result,
    error,
    isError: !!error,
  };
}
