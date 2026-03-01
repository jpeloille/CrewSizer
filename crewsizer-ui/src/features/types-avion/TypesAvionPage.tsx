import { useState } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { Pencil, Copy, Trash2, Plus } from 'lucide-react';
import { DataTable } from '@/components/shared/DataTable';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import { Button } from '@/components/ui/button';
import type { TypeAvionDto } from '@/types/typeAvion';
import {
  useTypesAvion,
  useCreateTypeAvion,
  useUpdateTypeAvion,
  useDeleteTypeAvion,
} from '@/features/programme/hooks/useProgrammeQueries';
import { TypeAvionForm } from './TypeAvionForm';
import type { CreateTypeAvionPayload } from '@/api/programme';

export function TypesAvionPage() {
  const { data: typesAvion = [], isLoading } = useTypesAvion();
  const createMutation = useCreateTypeAvion();
  const updateMutation = useUpdateTypeAvion();
  const deleteMutation = useDeleteTypeAvion();

  const [formOpen, setFormOpen] = useState(false);
  const [editTypeAvion, setEditTypeAvion] = useState<
    TypeAvionDto | undefined
  >();
  const [deleteTarget, setDeleteTarget] = useState<TypeAvionDto | null>(null);

  const openCreate = () => {
    setEditTypeAvion(undefined);
    setFormOpen(true);
  };

  const openEdit = (ta: TypeAvionDto) => {
    setEditTypeAvion(ta);
    setFormOpen(true);
  };

  const handleCopy = (ta: TypeAvionDto) => {
    createMutation.mutate({
      code: `${ta.code}_COPIE`,
      libelle: `${ta.libelle} (copie)`,
      nbCdb: ta.nbCdb,
      nbOpl: ta.nbOpl,
      nbCc: ta.nbCc,
      nbPnc: ta.nbPnc,
    });
  };

  const handleSubmit = (data: CreateTypeAvionPayload) => {
    if (editTypeAvion) {
      updateMutation.mutate(
        { id: editTypeAvion.id, data },
        { onSuccess: () => setFormOpen(false) }
      );
    } else {
      createMutation.mutate(data, { onSuccess: () => setFormOpen(false) });
    }
  };

  const columns: ColumnDef<TypeAvionDto>[] = [
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
      id: 'equipage',
      header: 'Equipage',
      cell: ({ row }) => {
        const ta = row.original;
        return (
          <div className="flex flex-wrap gap-1">
            <span className="rounded bg-blue-500/15 px-1.5 py-0.5 font-data text-[10px] font-semibold text-blue-500">
              {ta.nbCdb} CDB
            </span>
            <span className="rounded bg-blue-500/15 px-1.5 py-0.5 font-data text-[10px] font-semibold text-blue-500">
              {ta.nbOpl} OPL
            </span>
            <span className="rounded bg-emerald-500/15 px-1.5 py-0.5 font-data text-[10px] font-semibold text-emerald-500">
              {ta.nbCc} CC
            </span>
            <span className="rounded bg-emerald-500/15 px-1.5 py-0.5 font-data text-[10px] font-semibold text-emerald-500">
              {ta.nbPnc} PNC
            </span>
          </div>
        );
      },
    },
    {
      id: 'total',
      header: 'Total',
      cell: ({ row }) => {
        const ta = row.original;
        const total = ta.nbCdb + ta.nbOpl + ta.nbCc + ta.nbPnc;
        return (
          <span className="rounded bg-primary/10 px-2 py-0.5 font-data text-xs font-semibold text-primary">
            {total}
          </span>
        );
      },
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
      <h2 className="text-2xl font-bold text-foreground">Types avion</h2>

      <div className="flex items-center justify-between">
        <span className="font-data text-xs text-muted-foreground">
          {typesAvion.length} type(s) avion
        </span>
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" />
          Nouveau type avion
        </Button>
      </div>

      <DataTable
        columns={columns}
        data={typesAvion}
        searchPlaceholder="Rechercher code, libelle..."
        getRowId={(row) => row.id}
      />

      <TypeAvionForm
        open={formOpen}
        onOpenChange={setFormOpen}
        typeAvion={editTypeAvion}
        onSubmit={handleSubmit}
        isPending={createMutation.isPending || updateMutation.isPending}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={() => setDeleteTarget(null)}
        title="Supprimer le type avion"
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
