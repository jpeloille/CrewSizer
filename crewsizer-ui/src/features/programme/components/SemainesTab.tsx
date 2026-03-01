import { useState } from 'react';
import { Pencil, Trash2, Plus, ArrowRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import type { SemaineTypeDto } from '@/types/semaine';
import type { CreateSemainePayload } from '@/api/programme';
import {
  useVols,
  useBlocs,
  useSemaines,
  useCreateSemaine,
  useUpdateSemaine,
  useDeleteSemaine,
} from '../hooks/useProgrammeQueries';
import { SemaineForm } from './SemaineForm';
import { DAYS_SHORT, getRouteColor, hoursToHHMM } from '../lib/constants';
import { cn } from '@/lib/utils';

export function SemainesTab() {
  const { data: semaines = [], isLoading } = useSemaines();
  const { data: blocs = [] } = useBlocs();
  const { data: vols = [] } = useVols();
  const createMutation = useCreateSemaine();
  const updateMutation = useUpdateSemaine();
  const deleteMutation = useDeleteSemaine();

  const [formOpen, setFormOpen] = useState(false);
  const [editSemaine, setEditSemaine] = useState<SemaineTypeDto | undefined>();
  const [deleteTarget, setDeleteTarget] = useState<SemaineTypeDto | null>(
    null
  );

  const openCreate = () => {
    setEditSemaine(undefined);
    setFormOpen(true);
  };

  const openEdit = (s: SemaineTypeDto) => {
    setEditSemaine(s);
    setFormOpen(true);
  };

  const handleSubmit = (data: CreateSemainePayload) => {
    if (editSemaine) {
      updateMutation.mutate(
        { id: editSemaine.id, data },
        { onSuccess: () => setFormOpen(false) }
      );
    } else {
      createMutation.mutate(data, { onSuccess: () => setFormOpen(false) });
    }
  };

  const blocMap = new Map(blocs.map((b) => [b.id, b]));
  const volMap = new Map(vols.map((v) => [v.id, v]));

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-end">
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" />
          Nouvelle semaine type
        </Button>
      </div>

      {semaines.map((wp) => {
        const totalBlocs = wp.placements.length;
        const totalFlights = wp.placements.reduce((acc, p) => {
          const blk = blocMap.get(p.blocId);
          return acc + (blk?.nbEtapes ?? 0);
        }, 0);
        const totalHdv = wp.placements.reduce((acc, p) => {
          const blk = blocMap.get(p.blocId);
          return acc + (blk?.hdvBloc ?? 0);
        }, 0);
        const isHS = wp.saison === 'HAUTE';

        return (
          <div key={wp.id} className="space-y-2">
            {/* Header */}
            <div className="flex flex-wrap items-center gap-3">
              <span className="font-data text-lg font-bold tracking-wider text-primary">
                {wp.reference}
              </span>
              <Badge
                variant="outline"
                className={cn(
                  'font-data text-xs',
                  isHS
                    ? 'border-amber-500/25 bg-amber-500/12 text-amber-500'
                    : 'border-primary/25 bg-primary/12 text-primary'
                )}
              >
                {wp.saison}
              </Badge>
              <span className="font-data text-[11px] text-muted-foreground">
                {totalBlocs} blocs · {totalFlights} vols ·{' '}
                {hoursToHHMM(totalHdv)} bloc
              </span>
              <div className="flex-1" />
              <Button
                variant="outline"
                size="sm"
                onClick={() => openEdit(wp)}
              >
                <Pencil className="mr-1 h-3 w-3" />
                Editer
              </Button>
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7"
                onClick={() => setDeleteTarget(wp)}
              >
                <Trash2 className="h-3.5 w-3.5 text-destructive" />
              </Button>
            </div>

            {/* 7-day grid */}
            <div className="grid grid-cols-7 gap-1.5">
              {DAYS_SHORT.map((day, i) => {
                const dayName = [
                  'Lundi',
                  'Mardi',
                  'Mercredi',
                  'Jeudi',
                  'Vendredi',
                  'Samedi',
                  'Dimanche',
                ][i];
                const dayPlacements = wp.placements
                  .filter((p) => p.jour === dayName)
                  .sort((a, b) => a.sequence - b.sequence);

                return (
                  <div
                    key={i}
                    className="min-h-[80px] rounded-lg border border-border bg-card p-2"
                  >
                    <div className="mb-1.5 font-data text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                      {day}
                    </div>

                    {dayPlacements.length === 0 && (
                      <div className="text-[10px] italic text-muted-foreground/50">
                        —
                      </div>
                    )}

                    {dayPlacements.map((dp, j) => {
                      const blk = blocMap.get(dp.blocId);
                      if (!blk) return null;
                      const isMatin = blk.periode === 'matin';
                      const blocVols = blk.etapes
                        .sort((a, b) => a.position - b.position)
                        .map((e) => volMap.get(e.volId))
                        .filter(Boolean);

                      return (
                        <div
                          key={j}
                          className={cn(
                            'mb-1 rounded border-l-2 px-1.5 py-1 font-data text-[11px]',
                            isMatin
                              ? 'border-l-amber-500 bg-amber-500/5'
                              : 'border-l-purple-500 bg-purple-500/5'
                          )}
                        >
                          <div className="mb-0.5 font-bold">{blk.code}</div>
                          {blocVols.map((vol) =>
                            vol ? (
                              <div
                                key={vol.id}
                                className="flex gap-1 text-[9px] text-muted-foreground"
                              >
                                <span
                                  className="flex items-center gap-0.5"
                                  style={{
                                    color: getRouteColor(
                                      vol.depart,
                                      vol.arrivee
                                    ),
                                  }}
                                >
                                  {vol.depart}
                                  <ArrowRight className="h-2 w-2" />
                                  {vol.arrivee}
                                </span>
                                <span>{vol.heureDepart}</span>
                              </div>
                            ) : null
                          )}
                        </div>
                      );
                    })}
                  </div>
                );
              })}
            </div>
          </div>
        );
      })}

      {semaines.length === 0 && (
        <div className="py-12 text-center text-muted-foreground">
          Aucune semaine type. Creez-en une pour commencer.
        </div>
      )}

      <SemaineForm
        open={formOpen}
        onOpenChange={setFormOpen}
        semaine={editSemaine}
        blocs={blocs}
        onSubmit={handleSubmit}
        isPending={createMutation.isPending || updateMutation.isPending}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={() => setDeleteTarget(null)}
        title="Supprimer la semaine type"
        description={`Etes-vous sur de vouloir supprimer "${deleteTarget?.reference}" ? Cette action est irreversible.`}
        confirmLabel="Supprimer"
        onConfirm={() =>
          deleteTarget && deleteMutation.mutate(deleteTarget.id)
        }
        loading={deleteMutation.isPending}
      />
    </div>
  );
}
