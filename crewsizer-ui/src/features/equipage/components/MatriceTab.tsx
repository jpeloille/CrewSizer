import { useState } from 'react';
import { Button } from '@/components/ui/button';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip';
import { useMatrice } from '../hooks/useEquipageQueries';
import { statutIcon, statutMatriceCellClasses, formatDateFr } from '../lib/statut-helpers';
import type { GroupeCheck } from '@/types/enums';

export function MatriceTab() {
  const [filtreGroupe, setFiltreGroupe] = useState<GroupeCheck | undefined>(undefined);
  const { data: matrice, isLoading } = useMatrice(filtreGroupe);

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  if (!matrice) return null;

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

      <div className="overflow-x-auto rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="sticky left-0 z-10 bg-card min-w-[80px]">Code</TableHead>
              <TableHead className="sticky left-[80px] z-10 bg-card min-w-[140px]">Nom</TableHead>
              <TableHead className="sticky left-[220px] z-10 bg-card min-w-[65px]">Grade</TableHead>
              {matrice.codesChecks.map((code) => (
                <TableHead key={code} className="min-w-[50px] text-center font-data text-xs">
                  {code}
                </TableHead>
              ))}
            </TableRow>
          </TableHeader>
          <TableBody>
            {matrice.lignes.length > 0 ? (
              matrice.lignes.map((ligne) => (
                <TableRow key={ligne.membreId}>
                  <TableCell className="sticky left-0 z-10 bg-card font-data font-medium">
                    {ligne.code}
                  </TableCell>
                  <TableCell className="sticky left-[80px] z-10 bg-card">
                    {ligne.nom}
                  </TableCell>
                  <TableCell className="sticky left-[220px] z-10 bg-card font-data text-xs">
                    {ligne.grade}
                  </TableCell>
                  {matrice.codesChecks.map((code) => {
                    const cell = ligne.checks[code];
                    if (!cell) {
                      return <TableCell key={code} className="text-center text-muted-foreground/40">&#9675;</TableCell>;
                    }
                    return (
                      <TableCell key={code} className="p-0 text-center">
                        <Tooltip>
                          <TooltipTrigger asChild>
                            <div className={`flex h-full w-full items-center justify-center px-2 py-2 ${statutMatriceCellClasses(cell.statut)}`}>
                              {statutIcon(cell.statut)}
                            </div>
                          </TooltipTrigger>
                          <TooltipContent>
                            <p className="font-data">{code} — {formatDateFr(cell.dateExpiration)}</p>
                          </TooltipContent>
                        </Tooltip>
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell colSpan={3 + matrice.codesChecks.length} className="h-24 text-center">
                  Aucun membre.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className="flex gap-4 text-xs text-muted-foreground">
        <span className="flex items-center gap-1">
          <span className="inline-block h-3 w-3 rounded-sm bg-emerald-500/40"></span> Valide
        </span>
        <span className="flex items-center gap-1">
          <span className="inline-block h-3 w-3 rounded-sm bg-amber-500/40"></span> Proche / Avert.
        </span>
        <span className="flex items-center gap-1">
          <span className="inline-block h-3 w-3 rounded-sm bg-red-500/40"></span> Expire
        </span>
        <span className="flex items-center gap-1">
          <span className="text-muted-foreground/40">&#9675;</span> N/A
        </span>
      </div>
    </div>
  );
}
