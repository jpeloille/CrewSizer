import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { settingsApi } from '@/api/settings';
import { toast } from 'sonner';

const settingsKeys = {
  all: ['settings'] as const,
  detail: (key: string) => ['settings', key] as const,
};

export function useSettings() {
  return useQuery({
    queryKey: settingsKeys.all,
    queryFn: settingsApi.getAll,
  });
}

export function useSetting(key: string) {
  return useQuery({
    queryKey: settingsKeys.detail(key),
    queryFn: () => settingsApi.get(key),
  });
}

export function useUpdateSetting() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ key, value }: { key: string; value: string }) =>
      settingsApi.update(key, value),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: settingsKeys.all });
      queryClient.setQueryData(settingsKeys.detail(data.key), data);
      toast.success('Parametre mis a jour');
    },
    onError: () => {
      toast.error('Erreur lors de la mise a jour');
    },
  });
}
