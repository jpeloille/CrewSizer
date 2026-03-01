import { useState } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { Pencil, Copy, Trash2, Plus } from 'lucide-react';
import { DataTable } from '@/components/shared/DataTable';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import type { BlocTypeDto } from '@/types/blocType';
import {
  useBlocTypes,
  useCreateBlocType,
  useUpdateBlocType,
  useDeleteBlocType,
} from '@/features/programme/hooks/useProgrammeQueries';
import { BlocTypeForm } from './BlocTypeForm';
import { hoursToHHMM } from '@/features/programme/lib/constants';
import type { CreateBlocTypePayload } from '@/api/programme';

export function BlocTypesPage() {
  const { data: blocTypes = [], isLoading } = useBlocTypes();
  const createMutation = useCreateBlocType();
  const updateMutation = useUpdateBlocType();
  const deleteMutation = useDeleteBlocType();

  const [formOpen, setFormOpen] = useState(false);
  const [editBlocType, setEditBlocType] = useState<BlocTypeDto | undefined>();
  const [deleteTarget, setDeleteTarget] = useState<BlocTypeDto | null>(null);

  const openCreate = () => {
    setEditBlocType(undefined);
    setFormOpen(true);
  };

  const openEdit = (bt: BlocTypeDto) => {
    setEditBlocType(bt);
    setFormOpen(true);
  };

  const handleCopy = (bt: BlocTypeDto) => {
    createMutation.mutate({
      code: `${bt.code}_COPIE`,
      libelle: `${bt.libelle} (copie)`,
      debutPlage: bt.debutPlage,
      finPlage: bt.finPlage,
      fdpMax: bt.fdpMax,
      hauteSaison: bt.hauteSaison,
    });
  };

  const handleSubmit = (data: CreateBlocTypePayload) => {
    if (editBlocType) {
      updateMutation.mutate(
        { id: editBlocType.id, data },
        { onSuccess: () => setFormOpen(false) }
      );
    } else {
      createMutation.mutate(data, { onSuccess: () => setFormOpen(false) });
    }
  };

  const columns: ColumnDef<BlocTypeDto>[] = [
    {
      accessorKey: 'code',
      header: 'Code',
      cell: ({ getValue }) => (
        <span className="font-data text-sm font-bold tracking-wide text-primary">
          {getValue() as string}
        </span>
      ),
    },
    {
      accessorKey: 'libelle',
      header: 'Libelle',
      cell: ({ getValue }) => (
        <span className="text-sm">{getValue() as string}</span>
      ),
    },
    {
      id: 'plage',
      header: 'Plage horaire',
      accessorFn: (row) => `${row.debutPlage} ${row.finPlage}`,
      cell: ({ row }) => (
        <span className="font-data text-xs text-muted-foreground">
          {row.original.debutPlage}–{row.original.finPlage}
        </span>
      ),
    },
    {
      accessorKey: 'fdpMax',
      header: 'FDP max',
      cell: ({ getValue }) => (
        <span className="rounded bg-primary/10 px-2 py-0.5 font-data text-xs font-semibold text-primary">
          {hoursToHHMM(getValue() as number)}
        </span>
      ),
    },
    {
      accessorKey: 'hauteSaison',
      header: 'Saison',
      cell: ({ getValue }) =>
        getValue() ? (
          <Badge
            variant="outline"
            className="border-amber-500/25 bg-amber-500/15 font-data text-[9px] font-bold text-amber-500"
          >
            HS
          </Badge>
        ) : (
          <Badge
            variant="outline"
            className="border-sky-500/25 bg-sky-500/15 font-data text-[9px] font-bold text-sky-500"
          >
            BS
          </Badge>
        ),
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
          {blocTypes.length} type(s) de bloc
        </span>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" />
          Nouveau type
        </Button>
      </div>

      <DataTable
        columns={columns}
        data={blocTypes}
        searchPlaceholder="Rechercher code, libelle..."
        getRowId={(row) => row.id}
      />

      <BlocTypeForm
        open={formOpen}
        onOpenChange={setFormOpen}
        blocType={editBlocType}
        onSubmit={handleSubmit}
        isPending={createMutation.isPending || updateMutation.isPending}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={() => setDeleteTarget(null)}
        title="Supprimer le type de bloc"
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
