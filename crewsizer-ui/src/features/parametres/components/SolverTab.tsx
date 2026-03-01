import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { useSettings, useUpdateSetting } from '../hooks/useSettingsQueries';
import { Loader2 } from 'lucide-react';
import { useCallback, useMemo, useRef } from 'react';

export function SolverTab() {
  const { data: settings, isLoading } = useSettings();
  const updateSetting = useUpdateSetting();
  const debounceTimers = useRef<Record<string, ReturnType<typeof setTimeout>>>({});

  const settingsMap = useMemo(() => {
    if (!settings) return {};
    return Object.fromEntries(settings.map((s) => [s.key, s.value]));
  }, [settings]);

  const updateDebounced = useCallback(
    (key: string, value: string, delay = 600) => {
      if (debounceTimers.current[key]) clearTimeout(debounceTimers.current[key]);
      debounceTimers.current[key] = setTimeout(() => {
        updateSetting.mutate({ key, value });
      }, delay);
    },
    [updateSetting],
  );

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
      </div>
    );
  }

  const isDeterministic = settingsMap['solver.deterministic'] === 'true';

  return (
    <Card>
      <CardHeader>
        <CardTitle>Solver CP-SAT</CardTitle>
        <CardDescription>
          Configuration du moteur de dimensionnement Google OR-Tools CP-SAT.
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Mode deterministe */}
        <div className="flex items-center justify-between rounded-lg border p-4">
          <div className="space-y-0.5">
            <Label htmlFor="deterministic" className="text-base font-medium">
              Mode deterministe
            </Label>
            <p className="text-sm text-muted-foreground">
              Garantit des resultats identiques entre deux executions a donnees constantes.
              Le solver utilise un seul worker au lieu de plusieurs, ce qui le rend plus lent
              mais parfaitement reproductible.
            </p>
          </div>
          <Switch
            id="deterministic"
            checked={isDeterministic}
            onCheckedChange={(checked) =>
              updateSetting.mutate({ key: 'solver.deterministic', value: String(checked) })
            }
            disabled={updateSetting.isPending}
          />
        </div>

        {/* Timeout */}
        <div className="rounded-lg border p-4">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="timeout" className="text-base font-medium">
                Timeout (secondes)
              </Label>
              <p className="text-sm text-muted-foreground">
                Duree maximale de recherche du solver. Au-dela, le meilleur resultat
                trouve est retourne (solution realisable, pas forcement optimale).
              </p>
            </div>
            <Input
              id="timeout"
              type="number"
              min={5}
              max={300}
              step={5}
              className="w-24 text-right"
              defaultValue={settingsMap['solver.timeout'] ?? '30'}
              onChange={(e) => updateDebounced('solver.timeout', e.target.value)}
            />
          </div>
        </div>

        {/* Nombre de workers */}
        <div className="rounded-lg border p-4">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="workers" className="text-base font-medium">
                Workers paralleles
              </Label>
              <p className="text-sm text-muted-foreground">
                Nombre de threads de recherche paralleles.
                0 = automatique (utilise tous les CPUs disponibles).
                {isDeterministic && (
                  <span className="ml-1 font-medium text-amber-600">
                    Ignore en mode deterministe (force a 1).
                  </span>
                )}
              </p>
            </div>
            <Input
              id="workers"
              type="number"
              min={0}
              max={32}
              step={1}
              className="w-24 text-right"
              defaultValue={settingsMap['solver.workers'] ?? '0'}
              onChange={(e) => updateDebounced('solver.workers', e.target.value)}
              disabled={isDeterministic}
            />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
