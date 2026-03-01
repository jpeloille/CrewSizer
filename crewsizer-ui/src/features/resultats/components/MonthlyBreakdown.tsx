import { cn } from '@/lib/utils';
import type { ResultatMarge } from '@/types/resultat';

interface MonthlyBreakdownProps {
  mois: ResultatMarge[];
}

function statusBadge(statut: string) {
  const s = statut.toUpperCase();
  if (s === 'OK')
    return (
      <span className="inline-flex items-center rounded-full bg-green-500/15 px-2 py-0.5 text-xs font-semibold text-green-400">
        OK
      </span>
    );
  if (s === 'ALERTE')
    return (
      <span className="inline-flex items-center rounded-full bg-amber-500/15 px-2 py-0.5 text-xs font-semibold text-amber-400">
        Alerte
      </span>
    );
  return (
    <span className="inline-flex items-center rounded-full bg-red-500/15 px-2 py-0.5 text-xs font-semibold text-red-400">
      {statut}
    </span>
  );
}

function tauxColor(taux: number) {
  if (taux >= 0.95) return 'text-red-500 font-bold';
  if (taux >= 0.85) return 'text-amber-400';
  return 'text-green-400';
}

export function MonthlyBreakdown({ mois }: MonthlyBreakdownProps) {
  if (mois.length === 0) {
    return (
      <p className="p-5 text-sm text-muted-foreground">
        Aucune ventilation mensuelle disponible.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto p-5">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2 pl-4 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Periode
            </th>
            <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Jours
            </th>
            <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Blocs
            </th>
            <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              HDV (h)
            </th>
            <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Taux PNT
            </th>
            <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Taux PNC
            </th>
            <th className="py-2 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Taux global
            </th>
            <th className="py-2 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Contrainte
            </th>
            <th className="py-2 pr-4 text-center font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Statut
            </th>
          </tr>
        </thead>
        <tbody>
          {mois.map((m, i) => {
            const tauxPnt = Math.max(m.cdb.tauxEngagement, m.opl.tauxEngagement);
            const tauxPnc = Math.max(m.cc.tauxEngagement, m.pncDetail.tauxEngagement);
            return (
              <tr
                key={i}
                className={cn(
                  'border-b last:border-0',
                  m.tauxEngagementGlobal >= 0.95 && 'bg-red-500/5'
                )}
              >
                <td className="py-2 pl-4 pr-3 font-medium">{m.libellePeriode}</td>
                <td className="py-2 pr-3 text-right font-data">{m.nbJours}</td>
                <td className="py-2 pr-3 text-right font-data">{m.totalBlocs}</td>
                <td className="py-2 pr-3 text-right font-data">{m.totalHDV.toFixed(1)}</td>
                <td className={cn('py-2 pr-3 text-right font-data', tauxColor(tauxPnt))}>
                  {(tauxPnt * 100).toFixed(1)}%
                </td>
                <td className={cn('py-2 pr-3 text-right font-data', tauxColor(tauxPnc))}>
                  {(tauxPnc * 100).toFixed(1)}%
                </td>
                <td
                  className={cn(
                    'py-2 pr-3 text-right font-data font-semibold',
                    tauxColor(m.tauxEngagementGlobal)
                  )}
                >
                  {(m.tauxEngagementGlobal * 100).toFixed(1)}%
                </td>
                <td className="py-2 pr-3">
                  <span className="inline-flex rounded bg-primary/10 px-2 py-0.5 font-data text-xs font-bold text-primary">
                    {m.categorieContraignante}
                  </span>
                </td>
                <td className="py-2 pr-4 text-center">{statusBadge(m.statutGlobal)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
