import { useState } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { ChevronDown, ChevronRight } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { DataTable } from '@/components/shared/DataTable';
import type { AlerteQualificationDto } from '@/types/equipage';
import { statutBadgeClasses, statutLabel, formatDateFr } from '../lib/statut-helpers';

interface AlertesSectionProps {
  alertes: AlerteQualificationDto[];
}

const columns: ColumnDef<AlerteQualificationDto>[] = [
  { accessorKey: 'membreCode', header: 'Code', cell: ({ getValue }) => (
    <span className="font-data">{getValue() as string}</span>
  )},
  { accessorKey: 'membreNom', header: 'Nom' },
  { accessorKey: 'grade', header: 'Grade', cell: ({ getValue }) => (
    <span className="font-data text-xs">{getValue() as string}</span>
  )},
  { accessorKey: 'codeCheck', header: 'Check', cell: ({ getValue }) => (
    <span className="font-data">{getValue() as string}</span>
  )},
  { accessorKey: 'descriptionCheck', header: 'Description' },
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
      const jours = getValue() as number | null;
      if (jours === null) return '-';
      return <span className="font-data">{jours}j</span>;
    },
  },
];

export function AlertesSection({ alertes }: AlertesSectionProps) {
  const [open, setOpen] = useState(false);

  if (alertes.length === 0) return null;

  return (
    <div className="space-y-2">
      <Button
        variant="ghost"
        className="flex items-center gap-2"
        onClick={() => setOpen(!open)}
      >
        {open ? <ChevronDown className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
        Alertes qualifications
        <Badge className="bg-red-500/15 text-red-400 border-red-500/25">{alertes.length}</Badge>
      </Button>
      {open && (
        <DataTable
          columns={columns}
          data={alertes}
          searchPlaceholder="Rechercher une alerte..."
        />
      )}
    </div>
  );
}
