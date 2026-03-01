import { cn } from '@/lib/utils';
import type { VerifTsvMax } from '@/types/resultat';

interface TsvVerificationProps {
  verifications: VerifTsvMax[];
  tousBlocsConformes: boolean;
}

export function TsvVerification({ verifications, tousBlocsConformes }: TsvVerificationProps) {
  if (verifications.length === 0) {
    return (
      <p className="p-5 text-sm text-muted-foreground">
        Aucune verification TSV disponible.
      </p>
    );
  }

  return (
    <div className="p-5 space-y-3">
      <div className="flex items-center gap-2 text-sm">
        <span className="text-muted-foreground">Tous conformes :</span>
        {tousBlocsConformes ? (
          <span className="font-semibold text-green-400">Oui</span>
        ) : (
          <span className="font-semibold text-red-400">Non</span>
        )}
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b text-left">
              <th className="py-2 pl-4 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                Bloc
              </th>
              <th className="py-2 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                Jour
              </th>
              <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                Etapes
              </th>
              <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                TSV (h)
              </th>
              <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                Max (h)
              </th>
              <th className="py-2 pr-4 text-center font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                Statut
              </th>
            </tr>
          </thead>
          <tbody>
            {verifications.map((v, i) => (
              <tr
                key={i}
                className={cn(
                  'border-b last:border-0',
                  !v.conforme && 'bg-red-500/5'
                )}
              >
                <td className="py-2 pl-4 pr-3 font-medium">{v.nom}</td>
                <td className="py-2 pr-3">{v.jourNom}</td>
                <td className="py-2 pr-3 text-right font-data">{v.nbEtapes}</td>
                <td
                  className={cn(
                    'py-2 pr-3 text-right font-data',
                    !v.conforme && 'font-bold text-red-500'
                  )}
                >
                  {v.tsvDuree.toFixed(2)}
                </td>
                <td className="py-2 pr-3 text-right font-data">
                  {v.tsvMaxAutorise.toFixed(2)}
                </td>
                <td className="py-2 pr-4 text-center">
                  {v.conforme ? (
                    <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-green-500/15 text-xs text-green-400">
                      ✓
                    </span>
                  ) : (
                    <span className="inline-flex h-5 w-5 items-center justify-center rounded-full bg-red-500/15 text-xs font-bold text-red-400">
                      ✕
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
