import { useMemo } from 'react';
import type { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '@/components/shared/DataTable';
import { Checkbox } from '@/components/ui/checkbox';
import {
  useEquipageMembres,
  useUpdateMembreRoles,
} from '@/features/equipage/hooks/useEquipageQueries';
import type { MembreEquipageDto } from '@/types/equipage';
import { toast } from 'sonner';
import type { AxiosError } from 'axios';

const AVAILABLE_ROLES = ['RDOV', 'RDFE', 'CSV'] as const;

// ── Sous-composant : une checkbox de rôle par membre ─────────

function RoleCheckbox({
  membre,
  role,
}: {
  membre: MembreEquipageDto;
  role: string;
}) {
  const updateRoles = useUpdateMembreRoles();
  const checked = (membre.roles ?? []).includes(role);

  const handleToggle = () => {
    const currentRoles = membre.roles ?? [];
    const newRoles = checked
      ? currentRoles.filter((r) => r !== role)
      : [...currentRoles, role];

    updateRoles.mutate(
      { id: membre.id, roles: newRoles },
      {
        onSuccess: () => toast.success(`Roles mis a jour pour ${membre.nom}`),
        onError: (err) => {
          const axiosErr = err as AxiosError<{ errors?: Record<string, string[]> }>;
          const errors = axiosErr.response?.data?.errors;
          const msg = errors
            ? Object.values(errors).flat().join(', ')
            : axiosErr.message ?? 'Erreur inconnue';
          console.error('PATCH roles failed:', err);
          toast.error(`Erreur roles : ${msg}`);
        },
      },
    );
  };

  return (
    <div className="flex justify-center">
      <Checkbox
        checked={checked}
        disabled={updateRoles.isPending}
        onCheckedChange={handleToggle}
      />
    </div>
  );
}

// ── Composant principal ──────────────────────────────────────

export function RolesTab() {
  const { data: matrixData, isLoading } = useEquipageMembres();
  const membres = matrixData?.membres ?? [];

  const columns = useMemo<ColumnDef<MembreEquipageDto>[]>(
    () => [
      {
        accessorKey: 'code',
        header: 'Code',
        cell: ({ getValue }) => (
          <span className="font-data text-sm font-medium">
            {getValue() as string}
          </span>
        ),
      },
      {
        accessorKey: 'nom',
        header: 'Nom',
      },
      {
        accessorKey: 'grade',
        header: 'Grade',
        cell: ({ getValue }) => (
          <span className="font-data text-xs">{getValue() as string}</span>
        ),
      },
      {
        accessorKey: 'contrat',
        header: 'Type',
        cell: ({ getValue }) => (
          <span className="font-data text-xs">{getValue() as string}</span>
        ),
      },
      ...AVAILABLE_ROLES.map((role) => ({
        id: `role-${role}`,
        header: () => <span className="block text-center">{role}</span>,
        cell: ({ row }: { row: { original: MembreEquipageDto } }) => (
          <RoleCheckbox membre={row.original} role={role} />
        ),
      })),
    ],
    [],
  );

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-foreground">
            Attribution des roles
          </h2>
          <p className="text-sm text-muted-foreground">
            Cochez les roles pour chaque membre d&apos;equipage.
          </p>
        </div>
        <span className="font-data text-xs text-muted-foreground">
          {membres.length} membre(s)
        </span>
      </div>

      <DataTable
        columns={columns}
        data={membres}
        searchPlaceholder="Rechercher un membre..."
      />
    </div>
  );
}
