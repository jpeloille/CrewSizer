import { useState, useEffect, useMemo } from 'react';
import { Save, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useActiveScenario } from '@/hooks/useActiveScenario';
import { useScenario } from '@/features/scenarios/hooks/useScenarioQueries';
import { getIsoWeeksForPeriod, groupByQuarter } from '@/lib/dates';
import type { AffectationSemaineDto } from '@/types/scenario';
import {
  useSemaines,
  useCalendrier,
  useUpdateCalendrier,
} from '../hooks/useProgrammeQueries';
import { cn } from '@/lib/utils';

const SEMAINE_COLORS = [
  'var(--chart-1)',
  'var(--chart-3)',
  'var(--chart-2)',
  'var(--chart-4)',
  'var(--chart-5)',
  'var(--primary)',
];

export function CalendrierTab() {
  const { activeScenarioId } = useActiveScenario();
  const { data: scenario } = useScenario(activeScenarioId ?? '');
  const { data: semaines = [] } = useSemaines();
  const { data: calendrier, isLoading } = useCalendrier(activeScenarioId);
  const updateMutation = useUpdateCalendrier();

  // Local state for editable affectations
  const [affectations, setAffectations] = useState<AffectationSemaineDto[]>(
    []
  );
  const [dirty, setDirty] = useState(false);

  // Sync server data into local state
  useEffect(() => {
    if (calendrier?.affectations) {
      setAffectations(calendrier.affectations);
      setDirty(false);
    }
  }, [calendrier]);

  const getColor = (semaineTypeId: string) => {
    const idx = semaines.findIndex((s) => s.id === semaineTypeId);
    return idx >= 0 ? SEMAINE_COLORS[idx % SEMAINE_COLORS.length] : undefined;
  };

  const setWeekPattern = (week: number, annee: number, semaineTypeId: string) => {
    const semaineRef =
      semaines.find((s) => s.id === semaineTypeId)?.reference ?? '';

    setAffectations((prev) => {
      const existing = prev.filter((a) => !(a.semaine === week && a.annee === annee));
      if (semaineTypeId) {
        return [
          ...existing,
          { semaine: week, annee, semaineTypeId, semaineTypeRef: semaineRef },
        ];
      }
      return existing;
    });
    setDirty(true);
  };

  const handleSave = () => {
    if (!activeScenarioId) return;
    updateMutation.mutate(
      { scenarioId: activeScenarioId, affectations },
      { onSuccess: () => setDirty(false) }
    );
  };

  if (!activeScenarioId) {
    return (
      <div className="flex items-center gap-3 rounded-xl border border-border bg-card p-6 text-muted-foreground">
        <AlertCircle className="h-5 w-5" />
        <span>
          Selectionnez un scenario actif sur le tableau de bord pour gerer le
          calendrier.
        </span>
      </div>
    );
  }

  // Compute ISO weeks from scenario period
  const isoWeeks = useMemo(() => {
    if (!scenario?.dateDebut || !scenario?.dateFin) return null;
    return getIsoWeeksForPeriod(scenario.dateDebut, scenario.dateFin);
  }, [scenario?.dateDebut, scenario?.dateFin]);

  const quarters = useMemo(() => {
    if (!isoWeeks) return [];
    return groupByQuarter(isoWeeks);
  }, [isoWeeks]);

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  const totalWeeks = isoWeeks?.length ?? 0;

  const getAffectation = (week: number, year: number) =>
    affectations.find((a) => a.semaine === week && a.annee === year);

  const periodLabel = scenario
    ? `${scenario.dateDebut} → ${scenario.dateFin}`
    : '';

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        <span className="text-sm font-semibold">
          Calendrier {periodLabel && `— ${periodLabel}`}
        </span>
        <span className="font-data text-xs text-muted-foreground">
          {totalWeeks} semaines
        </span>
        <div className="flex-1" />

        {/* Legend */}
        <div className="flex flex-wrap gap-3 font-data text-[11px]">
          {semaines.map((s) => (
            <span key={s.id} className="flex items-center gap-1.5">
              <span
                className="inline-block h-2.5 w-2.5 rounded-sm"
                style={{ background: getColor(s.id) }}
              />
              {s.reference}
            </span>
          ))}
        </div>

        <Button
          onClick={handleSave}
          disabled={!dirty || updateMutation.isPending}
          size="sm"
        >
          <Save className="mr-2 h-3.5 w-3.5" />
          {updateMutation.isPending ? 'Sauvegarde...' : 'Sauvegarder'}
        </Button>
      </div>

      {/* Quarters */}
      {quarters.map((q, qi) => (
        <div key={qi}>
          <div className="mb-1 text-center font-data text-[11px] font-bold tracking-wider text-muted-foreground">
            {q.label}
          </div>
          <div
            className="grid gap-1"
            style={{ gridTemplateColumns: `repeat(${Math.min(q.weeks.length, 13)}, minmax(0, 1fr))` }}
          >
            {q.weeks.map((iw) => {
              const aff = getAffectation(iw.week, iw.year);
              const color = aff ? getColor(aff.semaineTypeId) : undefined;

              return (
                <div
                  key={`${iw.year}-${iw.week}`}
                  className={cn(
                    'min-h-[44px] rounded-md border border-transparent p-1 text-center transition-colors',
                    'hover:border-muted-foreground/30'
                  )}
                  style={{
                    background: color ? `${color}08` : undefined,
                    borderColor: color ? `${color}20` : undefined,
                  }}
                >
                  <div className="font-data text-[9px] font-bold text-muted-foreground">
                    S{iw.week}
                  </div>
                  <select
                    className="w-full border-none bg-transparent text-center font-data text-[10px] font-bold outline-none"
                    style={{ color: color || 'var(--muted-foreground)' }}
                    value={aff?.semaineTypeId ?? ''}
                    onChange={(e) =>
                      setWeekPattern(iw.week, iw.year, e.target.value)
                    }
                  >
                    <option value="">—</option>
                    {semaines.map((s) => (
                      <option key={s.id} value={s.id}>
                        {s.reference}
                      </option>
                    ))}
                  </select>
                </div>
              );
            })}
          </div>
        </div>
      ))}

      {/* Summary */}
      <div className="rounded-xl border border-border bg-card p-3 font-data text-xs text-muted-foreground">
        Resume :{' '}
        {semaines
          .map((s) => {
            const count = affectations.filter(
              (a) => a.semaineTypeId === s.id
            ).length;
            return `${s.reference}: ${count} sem.`;
          })
          .join(' · ')}
        {' · '}Non assignees :{' '}
        {totalWeeks - affectations.length} sem.
      </div>
    </div>
  );
}
