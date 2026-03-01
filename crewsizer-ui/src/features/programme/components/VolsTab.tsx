import { useState } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { Pencil, Copy, Trash2, Plus, ArrowRight } from 'lucide-react';
import { DataTable } from '@/components/shared/DataTable';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import type { VolDto } from '@/types/vol';
import {
  useVols,
  useCreateVol,
  useUpdateVol,
  useDeleteVol,
} from '../hooks/useProgrammeQueries';
import { VolForm } from './VolForm';
import { getRouteColor, hoursToHHMM } from '../lib/constants';
import type { CreateVolPayload } from '@/api/programme';

export function VolsTab() {
  const { data: vols = [], isLoading } = useVols();
  const createMutation = useCreateVol();
  const updateMutation = useUpdateVol();
  const deleteMutation = useDeleteVol();

  const [formOpen, setFormOpen] = useState(false);
  const [editVol, setEditVol] = useState<VolDto | undefined>();
  const [deleteTarget, setDeleteTarget] = useState<VolDto | null>(null);

  const openCreate = () => {
    setEditVol(undefined);
    setFormOpen(true);
  };

  const openEdit = (vol: VolDto) => {
    setEditVol(vol);
    setFormOpen(true);
  };

  const handleCopy = (vol: VolDto) => {
    createMutation.mutate({
      numero: `${vol.numero} (copie)`,
      depart: vol.depart,
      arrivee: vol.arrivee,
      heureDepart: vol.heureDepart,
      heureArrivee: vol.heureArrivee,
      mh: vol.mh,
    });
  };

  const handleSubmit = (data: CreateVolPayload) => {
    if (editVol) {
      updateMutation.mutate(
        { id: editVol.id, data },
        { onSuccess: () => setFormOpen(false) }
      );
    } else {
      createMutation.mutate(data, { onSuccess: () => setFormOpen(false) });
    }
  };

  const totalHdv = vols.reduce((sum, v) => sum + v.hdvVol, 0);

  const columns: ColumnDef<VolDto>[] = [
    {
      accessorKey: 'numero',
      header: 'N° Vol',
      cell: ({ getValue }) => (
        <span className="font-data text-sm font-bold tracking-wide text-primary">
          {getValue() as string}
        </span>
      ),
    },
    {
      id: 'route',
      header: 'Route',
      accessorFn: (row) => `${row.depart} ${row.arrivee}`,
      cell: ({ row }) => {
        const { depart, arrivee } = row.original;
        const color = getRouteColor(depart, arrivee);
        return (
          <span className="inline-flex items-center gap-1.5 font-data text-sm font-semibold">
            <span
              className="rounded px-1.5 py-0.5 text-xs"
              style={{
                background: `${color}18`,
                color,
              }}
            >
              {depart}
            </span>
            <ArrowRight className="h-3 w-3 text-muted-foreground" />
            <span style={{ color }}>{arrivee}</span>
          </span>
        );
      },
    },
    {
      accessorKey: 'heureDepart',
      header: 'STD',
      cell: ({ getValue }) => (
        <span className="font-data text-xs text-muted-foreground">
          {getValue() as string}
        </span>
      ),
    },
    {
      accessorKey: 'heureArrivee',
      header: 'STA',
      cell: ({ getValue }) => (
        <span className="font-data text-xs text-muted-foreground">
          {getValue() as string}
        </span>
      ),
    },
    {
      accessorKey: 'hdvVol',
      header: 'Bloc',
      cell: ({ getValue }) => (
        <span className="rounded bg-primary/10 px-2 py-0.5 font-data text-xs font-semibold text-primary">
          {hoursToHHMM(getValue() as number)}
        </span>
      ),
    },
    {
      accessorKey: 'mh',
      header: 'MH',
      cell: ({ getValue }) =>
        getValue() ? (
          <Badge
            variant="outline"
            className="border-amber-500/25 bg-amber-500/15 font-data text-[9px] font-bold text-amber-500"
          >
            MH
          </Badge>
        ) : null,
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className="flex gap-1">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            title="Editer"
            onClick={() => openEdit(row.original)}
          >
            <Pencil className="h-3.5 w-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            title="Copier"
            onClick={() => handleCopy(row.original)}
          >
            <Copy className="h-3.5 w-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            title="Supprimer"
            onClick={() => setDeleteTarget(row.original)}
          >
            <Trash2 className="h-3.5 w-3.5 text-destructive" />
          </Button>
        </div>
      ),
    },
  ];

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="font-data text-xs text-muted-foreground">
          Temps de bloc total : {hoursToHHMM(totalHdv)}
        </span>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" />
          Nouveau vol
        </Button>
      </div>

      <DataTable
        columns={columns}
        data={vols}
        searchPlaceholder="Rechercher vol, route..."
        getRowId={(row) => row.id}
      />

      <div className="font-data text-xs text-muted-foreground">
        {vols.length} vol(s)
      </div>

      <VolForm
        open={formOpen}
        onOpenChange={setFormOpen}
        vol={editVol}
        onSubmit={handleSubmit}
        isPending={createMutation.isPending || updateMutation.isPending}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={() => setDeleteTarget(null)}
        title="Supprimer le vol"
        description={`Etes-vous sur de vouloir supprimer "${deleteTarget?.numero}" ? Cette action est irreversible.`}
        confirmLabel="Supprimer"
        onConfirm={() =>
          deleteTarget && deleteMutation.mutate(deleteTarget.id)
        }
        loading={deleteMutation.isPending}
      />
    </div>
  );
}
