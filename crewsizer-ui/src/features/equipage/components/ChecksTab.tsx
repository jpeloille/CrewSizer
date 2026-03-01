import { useState } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { DataTable } from '@/components/shared/DataTable';
import { useChecks, useMembresPourCheck } from '../hooks/useEquipageQueries';
import type { DefinitionCheckDto, AlerteQualificationDto } from '@/types/equipage';
import type { GroupeCheck } from '@/types/enums';
import { statutBadgeClasses, statutLabel, formatDateFr } from '../lib/statut-helpers';

const checkColumns: ColumnDef<DefinitionCheckDto>[] = [
  { accessorKey: 'code', header: 'Code', cell: ({ getValue }) => (
    <span className="font-data font-medium">{getValue() as string}</span>
  )},
  { accessorKey: 'description', header: 'Description' },
  { accessorKey: 'groupe', header: 'Groupe', cell: ({ getValue }) => (
    <span className="font-data text-xs">{getValue() as string}</span>
  )},
  {
    accessorKey: 'primaire',
    header: 'Prim.',
    cell: ({ getValue }) => (getValue() as boolean) ? 'Oui' : 'Non',
  },
  {
    id: 'validite',
    header: 'Validite',
    cell: ({ row }) => (
      <span className="font-data text-xs">{row.original.validiteNombre} {row.original.validiteUnite}</span>
    ),
  },
  {
    accessorKey: 'nbMembresValides',
    header: 'Valides',
    cell: ({ getValue }) => {
      const n = getValue() as number;
      return n > 0 ? (
        <Badge className="bg-emerald-500/15 text-emerald-400 border-emerald-500/25 hover:bg-emerald-500/20">{n}</Badge>
      ) : <span className="text-muted-foreground">0</span>;
    },
  },
  {
    accessorKey: 'nbMembresAvertissement',
    header: 'Avert.',
    cell: ({ getValue }) => {
      const n = getValue() as number;
      return n > 0 ? (
        <Badge className="bg-amber-500/15 text-amber-400 border-amber-500/25 hover:bg-amber-500/20">{n}</Badge>
      ) : <span className="text-muted-foreground">0</span>;
    },
  },
  {
    accessorKey: 'nbMembresExpires',
    header: 'Expires',
    cell: ({ getValue }) => {
      const n = getValue() as number;
      return n > 0 ? (
        <Badge className="bg-red-500/15 text-red-400 border-red-500/25 hover:bg-red-500/20">{n}</Badge>
      ) : <span className="text-muted-foreground">0</span>;
    },
  },
  { accessorKey: 'nbMembresTotal', header: 'Total', cell: ({ getValue }) => (
    <span className="font-data">{getValue() as number}</span>
  )},
];

const membreColumns: ColumnDef<AlerteQualificationDto>[] = [
  { accessorKey: 'membreCode', header: 'Code', cell: ({ getValue }) => (
    <span className="font-data">{getValue() as string}</span>
  )},
  { accessorKey: 'membreNom', header: 'Nom' },
  { accessorKey: 'grade', header: 'Grade', cell: ({ getValue }) => (
    <span className="font-data text-xs">{getValue() as string}</span>
  )},
  {
    accessorKey: 'statut',
    header: 'Statut',
    cell: ({ getValue }) => {
      const statut = getValue() as AlerteQualificationDto['statut'];
      return (
        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${statutBadgeClasses(statut)}`}>
          {statutLabel(statut)}
        </span>
      );
    },
  },
  {
    accessorKey: 'dateExpiration',
    header: 'Expiration',
    cell: ({ getValue }) => (
      <span className="font-data text-xs">{formatDateFr(getValue() as string | null)}</span>
    ),
  },
  {
    accessorKey: 'joursRestants',
    header: 'Jours',
    cell: ({ getValue }) => {
      const j = getValue() as number | null;
      return j !== null ? <span className="font-data">{j}j</span> : '-';
    },
  },
];

export function ChecksTab() {
  const [filtreGroupe, setFiltreGroupe] = useState<GroupeCheck | undefined>(undefined);
  const [selectedCheck, setSelectedCheck] = useState<DefinitionCheckDto | null>(null);
  const { data: checks = [], isLoading } = useChecks(filtreGroupe);
  const { data: checkMembres = [], isLoading: isMembresLoading } = useMembresPourCheck(
    selectedCheck?.code ?? null
  );

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  return (
    <div className="space-y-4">
      <div className="flex gap-2">
        <Button
          variant={filtreGroupe === undefined ? 'default' : 'outline'}
          size="sm"
          onClick={() => setFiltreGroupe(undefined)}
        >
          Tous
        </Button>
        <Button
          variant={filtreGroupe === 'Cockpit' ? 'default' : 'outline'}
          size="sm"
          onClick={() => setFiltreGroupe('Cockpit')}
        >
          Cockpit (PNT)
        </Button>
        <Button
          variant={filtreGroupe === 'Cabine' ? 'default' : 'outline'}
          size="sm"
          onClick={() => setFiltreGroupe('Cabine')}
        >
          Cabine (PNC)
        </Button>
      </div>

      <DataTable
        columns={checkColumns}
        data={checks}
        searchPlaceholder="Rechercher un check..."
        onRowClick={(row) => setSelectedCheck(row)}
        getRowId={(row) => row.id}
        selectedRowId={selectedCheck?.id}
      />

      <Dialog open={!!selectedCheck} onOpenChange={() => setSelectedCheck(null)}>
        <DialogContent className="max-w-3xl">
          <DialogHeader>
            <DialogTitle className="font-data">
              {selectedCheck?.code} — {selectedCheck?.description}
            </DialogTitle>
          </DialogHeader>
          {isMembresLoading ? (
            <p className="text-muted-foreground">Chargement...</p>
          ) : (
            <DataTable
              columns={membreColumns}
              data={checkMembres}
              searchPlaceholder="Rechercher un membre..."
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
