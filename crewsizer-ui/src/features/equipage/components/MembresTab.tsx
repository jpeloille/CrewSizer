import { useState } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '@/components/shared/DataTable';
import { MembreDetail } from './MembreDetail';
import { useEquipageMembres, useMembreDetail } from '../hooks/useEquipageQueries';
import type { MembreEquipageDto } from '@/types/equipage';
import { statutBadgeClasses, statutLabel } from '../lib/statut-helpers';

function CheckMiniBar({ membre }: { membre: MembreEquipageDto }) {
  const checks = membre.qualificationsResume;
  if (!checks || checks.length === 0) return <span className="text-muted-foreground">-</span>;

  return (
    <div className="flex items-center gap-px">
      {checks.map((c, i) => (
        <div
          key={i}
          className={`h-4 w-1.5 rounded-sm ${
            c === 'Valide'
              ? 'bg-emerald-500'
              : c === 'ExpirationProche' || c === 'Avertissement'
                ? 'bg-amber-500'
                : c === 'Expire'
                  ? 'bg-red-500'
                  : 'bg-muted-foreground/30'
          }`}
          title={c}
        />
      ))}
    </div>
  );
}

const columns: ColumnDef<MembreEquipageDto>[] = [
  { accessorKey: 'code', header: 'Code', cell: ({ getValue }) => (
    <span className="font-data text-sm">{getValue() as string}</span>
  )},
  { accessorKey: 'nom', header: 'Nom' },
  { accessorKey: 'grade', header: 'Grade', cell: ({ getValue }) => (
    <span className="font-data text-xs">{getValue() as string}</span>
  )},
  { accessorKey: 'contrat', header: 'Type', cell: ({ getValue }) => (
    <span className="font-data text-xs">{getValue() as string}</span>
  )},
  {
    id: 'checks',
    header: 'Checks',
    cell: ({ row }) => <CheckMiniBar membre={row.original} />,
  },
  {
    accessorKey: 'statutGlobal',
    header: 'Statut',
    cell: ({ getValue }) => {
      const statut = getValue() as MembreEquipageDto['statutGlobal'];
      return (
        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${statutBadgeClasses(statut)}`}>
          {statutLabel(statut)}
        </span>
      );
    },
  },
];

export function MembresTab() {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const { data: matrixData, isLoading } = useEquipageMembres();
  const { data: detail } = useMembreDetail(selectedId);

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  const membres = matrixData?.membres ?? [];

  return (
    <div className="flex gap-4">
      <div className="w-[45%]">
        <DataTable
          columns={columns}
          data={membres}
          searchPlaceholder="Rechercher un membre..."
          onRowClick={(row) => setSelectedId(row.id)}
          selectedRowId={selectedId ?? undefined}
          getRowId={(row) => row.id}
        />
      </div>
      <div className="w-[55%]">
        {detail ? (
          <MembreDetail membre={detail} />
        ) : (
          <div className="flex h-64 items-center justify-center rounded-lg border border-dashed border-border">
            <p className="text-muted-foreground">
              Selectionnez un membre pour voir le detail.
            </p>
          </div>
        )}
      </div>
    </div>
  );
}
