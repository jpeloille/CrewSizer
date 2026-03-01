import { cn } from '@/lib/utils';
import type { ResultatMarge } from '@/types/resultat';

interface FtlSectionProps {
  resultat: ResultatMarge;
}

// EU-OPS Subpart Q HDV limits
const HDV_LIMITS = { '28j': 100, '90j': 280, '12m': 900 };

function statusColor(pct: number) {
  if (pct > 100) return 'red';
  if (pct >= 85) return 'amber';
  return 'green';
}

const BAR_COLORS = {
  green: 'bg-green-500',
  amber: 'bg-amber-500',
  red: 'bg-red-500',
};

const DOT_COLORS = {
  green: 'bg-green-500',
  amber: 'bg-amber-500',
  red: 'bg-red-500',
};

const STATUS_ICONS = {
  green: '✓',
  amber: '⚠',
  red: '✕',
};

interface FtlRowProps {
  label: string;
  value: number;
  limit: number;
}

function FtlRow({ label, value, limit }: FtlRowProps) {
  const pct = limit > 0 ? (value / limit) * 100 : 0;
  const status = statusColor(pct);

  return (
    <tr className={cn('border-b last:border-0', status === 'red' && 'bg-red-500/5')}>
      <td className="py-2.5 pl-4 pr-3 text-sm">{label}</td>
      <td
        className={cn(
          'py-2.5 pr-3 text-right font-data text-sm font-medium',
          status === 'red' && 'font-bold text-red-500'
        )}
      >
        {value.toFixed(1)} h
      </td>
      <td className="py-2.5 pr-3 text-right font-data text-sm">{limit} h</td>
      <td
        className={cn(
          'py-2.5 pr-3 text-right font-data text-sm',
          status === 'red' && 'font-bold text-red-500'
        )}
      >
        {pct.toFixed(1)}%
      </td>
      <td className="py-2.5 pr-3">
        <div className="flex items-center gap-2">
          <div className="h-1.5 min-w-[100px] flex-1 overflow-hidden rounded-full bg-border">
            <div
              className={cn('h-full rounded-full transition-all duration-700', BAR_COLORS[status])}
              style={{ width: `${Math.min(pct, 100)}%` }}
            />
          </div>
        </div>
      </td>
      <td className="py-2.5 pr-4 text-center">
        <span className="flex items-center justify-center gap-1.5 text-xs">
          <span className={cn('inline-block h-2 w-2 rounded-full', DOT_COLORS[status])} />
          {STATUS_ICONS[status]}
        </span>
      </td>
    </tr>
  );
}

function GroupSeparator({ label }: { label: string }) {
  return (
    <tr>
      <td
        colSpan={6}
        className="bg-muted/50 py-2 pl-4 font-data text-xs font-bold uppercase tracking-wider text-muted-foreground"
      >
        {label}
      </td>
    </tr>
  );
}

export function FtlSection({ resultat }: FtlSectionProps) {
  const pnt = resultat.pnt;
  const pnc = resultat.pnc;
  const tsPnt = resultat.verifTempsServicePNT;
  const tsPnc = resultat.verifTempsServicePNC;

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2.5 pl-4 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Contrainte
            </th>
            <th className="py-2.5 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Cumul projete
            </th>
            <th className="py-2.5 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Limite
            </th>
            <th className="py-2.5 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              % utilise
            </th>
            <th className="py-2.5 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Jauge
            </th>
            <th className="py-2.5 pr-4 text-center font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Statut
            </th>
          </tr>
        </thead>
        <tbody>
          {/* PNT HDV */}
          <GroupSeparator label="PNT — Heures de vol" />
          <FtlRow label="HDV cumulees 28 jours" value={pnt.verif28j.item1} limit={HDV_LIMITS['28j']} />
          <FtlRow label="HDV cumulees 90 jours" value={pnt.verif90j.item1} limit={HDV_LIMITS['90j']} />
          <FtlRow label="HDV cumulees 12 mois" value={pnt.verif12m.item1} limit={HDV_LIMITS['12m']} />

          {/* PNC HDV */}
          <GroupSeparator label="PNC — Heures de vol" />
          <FtlRow label="HDV cumulees 28 jours" value={pnc.verif28j.item1} limit={HDV_LIMITS['28j']} />
          <FtlRow label="HDV cumulees 90 jours" value={pnc.verif90j.item1} limit={HDV_LIMITS['90j']} />
          <FtlRow label="HDV cumulees 12 mois" value={pnc.verif12m.item1} limit={HDV_LIMITS['12m']} />

          {/* Temps de service */}
          <GroupSeparator label="Temps de service" />
          <FtlRow label="TS 7 jours (PNT)" value={tsPnt.verif7j.item1} limit={tsPnt.verif7j.item2} />
          <FtlRow label="TS 14 jours (PNT)" value={tsPnt.verif14j.item1} limit={tsPnt.verif14j.item2} />
          <FtlRow label="TS 28 jours (PNT)" value={tsPnt.verif28j.item1} limit={tsPnt.verif28j.item2} />
          <FtlRow label="TS 7 jours (PNC)" value={tsPnc.verif7j.item1} limit={tsPnc.verif7j.item2} />
          <FtlRow label="TS 14 jours (PNC)" value={tsPnc.verif14j.item1} limit={tsPnc.verif14j.item2} />
          <FtlRow label="TS 28 jours (PNC)" value={tsPnc.verif28j.item1} limit={tsPnc.verif28j.item2} />
        </tbody>
      </table>
    </div>
  );
}
