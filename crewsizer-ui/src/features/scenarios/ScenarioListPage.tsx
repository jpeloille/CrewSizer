import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { ColumnDef } from '@tanstack/react-table';
import { scenariosApi } from '@/api/scenarios';
import type { ScenarioListItemDto } from '@/types/scenario';
import { DataTable } from '@/components/shared/DataTable';
import { ConfirmDialog } from '@/components/shared/ConfirmDialog';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Plus, Copy, Trash2, Pencil } from 'lucide-react';

const columns: ColumnDef<ScenarioListItemDto>[] = [
  { accessorKey: 'nom', header: 'Nom' },
  {
    id: 'periode',
    header: 'Période',
    cell: ({ row }) => {
      const d = row.original.dateDebut;
      const f = row.original.dateFin;
      if (!d || !f) return '—';
      return `${new Date(d).toLocaleDateString('fr-FR')} — ${new Date(f).toLocaleDateString('fr-FR')}`;
    },
  },
  { accessorKey: 'description', header: 'Description' },
  {
    accessorKey: 'dateModification',
    header: 'Modifie le',
    cell: ({ getValue }) =>
      new Date(getValue() as string).toLocaleDateString('fr-FR', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      }),
  },
  { accessorKey: 'modifiePar', header: 'Par' },
];

export function ScenarioListPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [createOpen, setCreateOpen] = useState(false);
  const [cloneTarget, setCloneTarget] = useState<ScenarioListItemDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ScenarioListItemDto | null>(null);
  const [formNom, setFormNom] = useState('');
  const [formDesc, setFormDesc] = useState('');

  const { data: scenarios = [], isLoading } = useQuery({
    queryKey: ['scenarios'],
    queryFn: scenariosApi.getAll,
  });

  const createMutation = useMutation({
    mutationFn: (data: { nom: string; description?: string }) =>
      scenariosApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scenarios'] });
      setCreateOpen(false);
      setFormNom('');
      setFormDesc('');
    },
  });

  const cloneMutation = useMutation({
    mutationFn: ({ id, nouveauNom }: { id: string; nouveauNom: string }) =>
      scenariosApi.clone(id, nouveauNom),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scenarios'] });
      setCloneTarget(null);
      setFormNom('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => scenariosApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['scenarios'] });
      setDeleteTarget(null);
    },
  });

  const actionsColumn: ColumnDef<ScenarioListItemDto> = {
    id: 'actions',
    header: '',
    cell: ({ row }) => (
      <div className="flex gap-1">
        <Button
          variant="ghost"
          size="icon"
          title="Editer"
          onClick={() => navigate(`/scenarios/${row.original.id}`)}
        >
          <Pencil className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          title="Dupliquer"
          onClick={(e) => {
            e.stopPropagation();
            setCloneTarget(row.original);
            setFormNom(`${row.original.nom} (copie)`);
          }}
        >
          <Copy className="h-4 w-4" />
        </Button>
        <Button
          variant="ghost"
          size="icon"
          title="Supprimer"
          onClick={(e) => {
            e.stopPropagation();
            setDeleteTarget(row.original);
          }}
        >
          <Trash2 className="h-4 w-4 text-destructive" />
        </Button>
      </div>
    ),
  };

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Scenarios</h1>
        <Button onClick={() => setCreateOpen(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Nouveau
        </Button>
      </div>

      <DataTable
        columns={[...columns, actionsColumn]}
        data={scenarios}
        searchPlaceholder="Rechercher un scenario..."
        onRowClick={(row) => navigate(`/scenarios/${row.id}`)}
      />

      {/* Dialog creation */}
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Nouveau scenario</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Nom</Label>
              <Input
                value={formNom}
                onChange={(e) => setFormNom(e.target.value)}
                placeholder="Ex: Janvier 2026"
              />
            </div>
            <div className="space-y-2">
              <Label>Description</Label>
              <Input
                value={formDesc}
                onChange={(e) => setFormDesc(e.target.value)}
                placeholder="Description (optionnel)"
              />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              Annuler
            </Button>
            <Button
              onClick={() =>
                createMutation.mutate({
                  nom: formNom,
                  description: formDesc || undefined,
                })
              }
              disabled={!formNom || createMutation.isPending}
            >
              {createMutation.isPending ? 'Creation...' : 'Creer'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog clone */}
      <Dialog open={!!cloneTarget} onOpenChange={() => setCloneTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Dupliquer le scenario</DialogTitle>
          </DialogHeader>
          <div className="space-y-2">
            <Label>Nom du nouveau scenario</Label>
            <Input value={formNom} onChange={(e) => setFormNom(e.target.value)} />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCloneTarget(null)}>
              Annuler
            </Button>
            <Button
              onClick={() =>
                cloneTarget &&
                cloneMutation.mutate({ id: cloneTarget.id, nouveauNom: formNom })
              }
              disabled={!formNom || cloneMutation.isPending}
            >
              {cloneMutation.isPending ? 'Duplication...' : 'Dupliquer'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Dialog suppression */}
      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={() => setDeleteTarget(null)}
        title="Supprimer le scenario"
        description={`Etes-vous sur de vouloir supprimer "${deleteTarget?.nom}" ? Cette action est irreversible.`}
        confirmLabel="Supprimer"
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        loading={deleteMutation.isPending}
      />
    </div>
  );
}
