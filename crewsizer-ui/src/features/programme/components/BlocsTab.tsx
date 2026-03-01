import { useState } from 'react';
import { Pencil, Trash2, Plus, Clock, ArrowRight, Search } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import type { BlocVolDto } from '@/types/bloc';
import type { CreateBlocPayload } from '@/api/programme';
import {
  useVols,
  useBlocs,
  useBlocTypes,
  useTypesAvion,
  useCreateBloc,
  useUpdateBloc,
  useDeleteBloc,
} from '../hooks/useProgrammeQueries';
import { BlocForm } from './BlocForm';
import {
  getRouteColor,
  hoursToHHMM,
  diffTimeMinutes,
  minutesToHHMM,
  timeToMinutes,
  minutesToTime,
} from '../lib/constants';
import { cn } from '@/lib/utils';

export function BlocsTab() {
  const { data: blocs = [], isLoading } = useBlocs();
  const { data: vols = [] } = useVols();
  const { data: blocTypes = [] } = useBlocTypes();
  const { data: typesAvion = [] } = useTypesAvion();
  const createMutation = useCreateBloc();
  const updateMutation = useUpdateBloc();
  const deleteMutation = useDeleteBloc();

  const [search, setSearch] = useState('');
  const [periodFilter, setPeriodFilter] = useState<
    'all' | 'matin' | 'apres-midi'
  >('all');
  const [formOpen, setFormOpen] = useState(false);
  const [editBloc, setEditBloc] = useState<BlocVolDto | undefined>();
  const [deleteTarget, setDeleteTarget] = useState<BlocVolDto | null>(null);

  const openCreate = () => {
    setEditBloc(undefined);
    setFormOpen(true);
  };

  const openEdit = (bloc: BlocVolDto) => {
    setEditBloc(bloc);
    setFormOpen(true);
  };

  const handleSubmit = (data: CreateBlocPayload) => {
    if (editBloc) {
      updateMutation.mutate(
        { id: editBloc.id, data },
        { onSuccess: () => setFormOpen(false) }
      );
    } else {
      createMutation.mutate(data, { onSuccess: () => setFormOpen(false) });
    }
  };

  const volMap = new Map(vols.map((v) => [v.id, v]));

  const filtered = blocs.filter((b) => {
    if (periodFilter !== 'all' && b.periode !== periodFilter) return false;
    if (!search) return true;
    const q = search.toLowerCase();
    return (
      b.code.toLowerCase().includes(q) ||
      (b.nom && b.nom.toLowerCase().includes(q))
    );
  });

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        <div className="relative min-w-[200px] max-w-xs flex-1">
          <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-9"
            placeholder="Rechercher bloc..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>

        <div className="flex rounded-md border border-border">
          {(
            [
              { key: 'all', label: 'TOUS' },
              { key: 'matin', label: 'MATIN' },
              { key: 'apres-midi', label: 'AM' },
            ] as const
          ).map((opt) => (
            <button
              key={opt.key}
              onClick={() => setPeriodFilter(opt.key)}
              className={cn(
                'px-3 py-1.5 font-data text-xs font-semibold tracking-wide transition-colors',
                'first:rounded-l-md last:rounded-r-md',
                periodFilter === opt.key
                  ? 'bg-primary/15 text-primary'
                  : 'text-muted-foreground hover:bg-accent'
              )}
            >
              {opt.label}
            </button>
          ))}
        </div>

        <div className="flex-1" />

        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" />
          Nouveau bloc
        </Button>
      </div>

      {/* Card grid */}
      <div className="grid grid-cols-1 gap-3 md:grid-cols-2 xl:grid-cols-3">
        {filtered.map((bloc) => {
          const etapesSorted = [...bloc.etapes].sort((a, b) => a.position - b.position);
          const hasMod = etapesSorted.some((e) => !!e.modificateur);
          const dpMin = diffTimeMinutes(bloc.debutDP, bloc.finDP);
          const fdpMin = diffTimeMinutes(bloc.debutFDP, bloc.finFDP);
          const isMatin = bloc.periode === 'matin';
          const accentColor = isMatin
            ? 'var(--chart-3)'
            : 'var(--chart-4)';

          return (
            <div
              key={bloc.id}
              className="rounded-xl border border-border bg-card p-3.5 transition-colors hover:border-muted-foreground/30"
            >
              {/* Header */}
              <div className="mb-2.5 flex items-center justify-between">
                <div>
                  <div
                    className="font-data text-[15px] font-bold tracking-wider"
                    style={{ color: accentColor }}
                  >
                    {bloc.code}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {bloc.nom}
                  </div>
                </div>
                <div className="flex items-center gap-1">
                  {hasMod && (
                    <span className="rounded bg-orange-500/15 px-2 py-0.5 font-data text-[10px] font-bold tracking-wide text-orange-500">
                      MH
                    </span>
                  )}
                  {bloc.blocTypeCode && (
                    <span
                      className={cn(
                        'rounded px-2 py-0.5 font-data text-[10px] font-semibold tracking-wide',
                        bloc.blocTypeCode.toLowerCase().includes('hs')
                          ? 'bg-red-500/12 text-red-500'
                          : 'bg-blue-500/12 text-blue-500'
                      )}
                    >
                      {bloc.blocTypeCode}
                    </span>
                  )}
                  {bloc.typeAvionCode && (
                    <span className="rounded bg-cyan-500/12 px-2 py-0.5 font-data text-[10px] font-semibold tracking-wide text-cyan-500">
                      {bloc.typeAvionCode}
                    </span>
                  )}
                  <span
                    className={cn(
                      'rounded px-2 py-0.5 font-data text-[10px] font-semibold tracking-wide',
                      isMatin
                        ? 'bg-amber-500/12 text-amber-500'
                        : 'bg-purple-500/12 text-purple-500'
                    )}
                  >
                    {isMatin ? 'MATIN' : 'AM'}
                  </span>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    onClick={() => openEdit(bloc)}
                  >
                    <Pencil className="h-3.5 w-3.5" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7"
                    onClick={() => setDeleteTarget(bloc)}
                  >
                    <Trash2 className="h-3.5 w-3.5 text-destructive" />
                  </Button>
                </div>
              </div>

              {/* Meta */}
              <div className="mb-2.5 flex flex-wrap gap-2.5 font-data text-[11px] text-muted-foreground">
                <span className="flex items-center gap-1">
                  <Clock className="h-3 w-3" />
                  DP: {minutesToHHMM(dpMin)}
                </span>
                <span>FDP: {minutesToHHMM(fdpMin)}</span>
                <span>Bloc: {hoursToHHMM(bloc.hdvBloc)}</span>
                <span className="font-semibold">{bloc.jour}</span>
                <span>{bloc.nbEtapes} vol(s)</span>
              </div>

              {/* Flight list */}
              <div className="space-y-1">
                {etapesSorted.map((etape, i) => {
                  const vol = volMap.get(etape.volId);
                  if (!vol) return null;
                  const eMod = !!etape.modificateur;
                  const effDep = eMod ? minutesToTime(timeToMinutes(vol.heureDepart) + etape.modificateur!) : vol.heureDepart;
                  const effArr = eMod ? minutesToTime(timeToMinutes(vol.heureArrivee) + etape.modificateur!) : vol.heureArrivee;
                  return (
                    <div
                      key={`${etape.volId}-${i}`}
                      className="flex items-center gap-2 rounded-md bg-background px-2 py-1 text-xs"
                    >
                      <span className="font-data text-[10px] font-bold text-muted-foreground">
                        {etape.position}
                      </span>
                      <span className="font-data text-[11px] font-semibold text-primary">
                        {vol.numero}
                      </span>
                      {eMod && (
                        <span className="rounded bg-orange-500/15 px-1 py-0.5 font-data text-[9px] font-bold text-orange-500">
                          MH
                        </span>
                      )}
                      <span
                        className="flex items-center gap-1 text-[11px]"
                        style={{
                          color: getRouteColor(vol.depart, vol.arrivee),
                        }}
                      >
                        {vol.depart}
                        <ArrowRight className="h-2.5 w-2.5" />
                        {vol.arrivee}
                      </span>
                      <span className="ml-auto font-data text-[10px] text-muted-foreground">
                        {effDep}–{effArr} (
                        {hoursToHHMM(vol.hdvVol)})
                      </span>
                    </div>
                  );
                })}
              </div>

              {/* Timeline bar */}
              <div className="mt-2 h-1 overflow-hidden rounded-full bg-border">
                <div
                  className="h-full rounded-full opacity-60"
                  style={{
                    width: `${Math.min(100, dpMin > 0 ? (bloc.hdvBloc * 60 / dpMin) * 100 : 0)}%`,
                    background: accentColor,
                  }}
                />
              </div>
            </div>
          );
        })}
      </div>

      <BlocForm
        open={formOpen}
        onOpenChange={setFormOpen}
        bloc={editBloc}
        vols={vols}
        blocTypes={blocTypes}
        typesAvion={typesAvion}
        onSubmit={handleSubmit}
        isPending={createMutation.isPending || updateMutation.isPending}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={() => setDeleteTarget(null)}
        title="Supprimer le bloc"
        description={`Etes-vous sur de vouloir supprimer "${deleteTarget?.code}" ? Cette action est irreversible.`}
        confirmLabel="Supprimer"
        onConfirm={() =>
          deleteTarget && deleteMutation.mutate(deleteTarget.id)
        }
        loading={deleteMutation.isPending}
      />
    </div>
  );
}
