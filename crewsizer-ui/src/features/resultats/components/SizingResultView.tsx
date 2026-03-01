import { Alert, AlertTitle, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { cn } from '@/lib/utils';
import type { CombinedSizingResult, SizingResult, CriticalDay, ConstraintSource } from '@/types/sizing';
import { EngagementGauge } from './EngagementGauge';
import { CollapsibleSection } from './CollapsibleSection';
import {
  AlertTriangle,
  BarChart3,
  CheckCircle2,
  Clock,
  Plane,
  Settings,
  Shield,
  Users,
  XCircle,
} from 'lucide-react';

interface SizingResultViewProps {
  result: CombinedSizingResult;
}

const RANK_LABELS: Record<string, string> = {
  CDB: 'CDB',
  OPL: 'OPL',
  CC: 'CC',
  PNC: 'PNC',
  RPN: 'RPN',
};

const RANK_FULL_LABELS: Record<string, string> = {
  CDB: 'Commandants de bord',
  OPL: 'Officiers pilotes',
  CC: 'Chefs de cabine',
  PNC: 'PNC',
  RPN: 'Responsables PNC',
};

const SOURCE_STYLES: Record<ConstraintSource, { label: string; className: string }> = {
  OroFtl: { label: 'ORO.FTL', className: 'bg-blue-600 text-white' },
  ConventionCompagnie: { label: 'Convention', className: 'bg-purple-600 text-white' },
  Deliberation77: { label: 'Délib. 77', className: 'bg-teal-600 text-white' },
  Structural: { label: 'Struct.', className: 'bg-gray-500 text-white' },
};

// ─── Helpers ─────────────────────────────────────────────

function computeUtilization(result: SizingResult, ranks: string[]): number {
  let totalMin = 0;
  let totalMargin = 0;
  for (const rank of ranks) {
    totalMin += result.minimumCrewByRank[rank] ?? 0;
    totalMargin += result.marginByRank[rank] ?? 0;
  }
  const total = totalMin + totalMargin;
  return total > 0 ? totalMin / total : 0;
}

function utilizationColor(u: number) {
  if (u >= 0.95) return { color: '#ef4444', label: 'Critique', variant: 'bg-red-500' };
  if (u >= 0.85) return { color: '#f59e0b', label: 'Tendu', variant: 'bg-amber-500' };
  return { color: '#22c55e', label: 'Confortable', variant: 'bg-green-600' };
}

function marginStatus(margin: number): 'green' | 'amber' | 'red' {
  if (margin <= 0) return 'red';
  if (margin <= 2) return 'amber';
  return 'green';
}

function statusBadge(status: SizingResult['status']) {
  switch (status) {
    case 'Optimal':
      return <Badge className="bg-green-600 text-white">Optimal</Badge>;
    case 'Feasible':
      return <Badge className="bg-amber-500 text-white">Faisable</Badge>;
    case 'Infeasible':
      return <Badge className="bg-red-600 text-white">Infaisable</Badge>;
    case 'Timeout':
      return <Badge className="bg-orange-500 text-white">Timeout</Badge>;
    case 'Error':
      return <Badge className="bg-red-800 text-white">Erreur</Badge>;
  }
}

// ─── Kpi (same as EngagementCard) ────────────────────────

function Kpi({ label, value, sub }: { label: string; value: React.ReactNode; sub?: string }) {
  return (
    <div className="flex flex-col">
      <span className="font-data text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
        {label}
      </span>
      <span className="font-data text-lg font-semibold tracking-tight">{value}</span>
      {sub && <span className="text-[11px] text-muted-foreground">{sub}</span>}
    </div>
  );
}

// ─── SizingEngagementCard ────────────────────────────────

function SizingEngagementCard({
  label,
  icon,
  result,
  ranks,
}: {
  label: string;
  icon: React.ReactNode;
  result: SizingResult;
  ranks: string[];
}) {
  const utilization = result.isFeasible ? computeUtilization(result, ranks) : 1;
  const { color, label: statusLabel, variant } = utilizationColor(utilization);

  const totalMin = ranks.reduce((s, r) => s + (result.minimumCrewByRank[r] ?? 0), 0);
  const totalMargin = ranks.reduce((s, r) => s + (result.marginByRank[r] ?? 0), 0);

  return (
    <Card>
      <CardHeader className="border-b">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2 text-sm font-semibold">
            {icon}
            {label}
            {statusBadge(result.status)}
            <Badge className={`${variant} text-white`}>{statusLabel}</Badge>
          </div>
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            <Clock className="h-3 w-3" />
            {(result.solveTimeMs / 1000).toFixed(1)}s
          </span>
        </div>
      </CardHeader>
      <CardContent className="pt-5">
        {!result.isFeasible ? (
          <div className="flex items-start gap-3 rounded-md bg-red-500/10 p-4">
            <XCircle className="mt-0.5 h-5 w-5 shrink-0 text-red-500" />
            <div>
              <p className="font-semibold text-red-500">Programme non couvrable</p>
              <p className="mt-1 text-sm text-muted-foreground">
                {result.message || "L'effectif disponible est insuffisant."}
              </p>
            </div>
          </div>
        ) : (
          <div className="flex gap-6">
            <EngagementGauge value={utilization} color={color} />
            <div className="grid flex-1 grid-cols-2 gap-x-5 gap-y-3">
              <Kpi label="Effectif minimum" value={totalMin} />
              <Kpi
                label="Marge totale"
                value={
                  <span className={totalMargin > 0 ? 'text-green-500' : 'text-red-500'}>
                    {totalMargin > 0 ? `+${totalMargin}` : totalMargin}
                  </span>
                }
              />
              {ranks.map((rank) => (
                <Kpi
                  key={rank}
                  label={`Min ${RANK_LABELS[rank] ?? rank}`}
                  value={result.minimumCrewByRank[rank] ?? 0}
                  sub={`marge : ${result.marginByRank[rank] != null
                    ? (result.marginByRank[rank] > 0 ? '+' : '') + result.marginByRank[rank]
                    : '—'}`}
                />
              ))}
              <Kpi
                label="Contrainte mordante"
                value={
                  result.bindingConstraint ? (
                    <span className="inline-flex items-center gap-1.5">
                      {result.bindingConstraintSource && SOURCE_STYLES[result.bindingConstraintSource] && (
                        <Badge className={`${SOURCE_STYLES[result.bindingConstraintSource].className} text-[9px] px-1.5 py-0`}>
                          {SOURCE_STYLES[result.bindingConstraintSource].label}
                        </Badge>
                      )}
                      {result.bindingConstraintCode && (
                        <span className="font-data text-[10px] font-bold text-muted-foreground">
                          {result.bindingConstraintCode}
                        </span>
                      )}
                      <span className="inline-flex rounded bg-primary/10 px-2 py-0.5 font-data text-xs font-bold text-primary">
                        {result.bindingConstraint}
                      </span>
                    </span>
                  ) : (
                    '—'
                  )
                }
              />
              <Kpi label="Statut solveur" value={result.status} />
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

// ─── RankBreakdown (CategoryBreakdown-style) ─────────────

function rankStatusBadge(margin: number) {
  const s = marginStatus(margin);
  if (s === 'green') return <Badge className="bg-green-600 text-white">OK</Badge>;
  if (s === 'amber') return <Badge className="bg-amber-500 text-white">Tendu</Badge>;
  return <Badge variant="destructive">Critique</Badge>;
}

function StatRow({
  label,
  value,
  valueClass,
}: {
  label: string;
  value: React.ReactNode;
  valueClass?: string;
}) {
  return (
    <div className="flex items-center justify-between border-b border-dashed border-border py-1.5 text-sm last:border-0">
      <span className="text-muted-foreground">{label}</span>
      <span className={`font-data font-medium ${valueClass ?? ''}`}>{value}</span>
    </div>
  );
}

function RankBreakdown({
  pntResult,
  pncResult,
}: {
  pntResult: SizingResult;
  pncResult: SizingResult;
}) {
  const allRanks = [
    { rank: 'CDB', result: pntResult },
    { rank: 'OPL', result: pntResult },
    { rank: 'CC', result: pncResult },
    { rank: 'PNC', result: pncResult },
  ];

  return (
    <div className="grid grid-cols-1 gap-4 p-5 sm:grid-cols-2 xl:grid-cols-4">
      {allRanks.map(({ rank, result }) => {
        const minimum = result.minimumCrewByRank[rank] ?? 0;
        const margin = result.marginByRank[rank] ?? 0;
        const total = minimum + margin;
        const utilization = total > 0 ? minimum / total : 0;

        if (!result.isFeasible) return null;

        return (
          <div key={rank} className="rounded-lg border bg-card p-4">
            <div className="mb-3 flex items-center justify-between">
              <h4 className="text-sm font-semibold">{RANK_FULL_LABELS[rank] ?? rank}</h4>
              {rankStatusBadge(margin)}
            </div>
            <StatRow label="Minimum requis" value={minimum} />
            <StatRow label="Disponibles" value={total} />
            <StatRow
              label="Marge"
              value={`${margin > 0 ? '+' : ''}${margin}`}
              valueClass={margin > 0 ? 'text-green-500' : margin === 0 ? 'text-amber-500' : 'text-red-500'}
            />
            <StatRow
              label="Taux utilisation"
              value={`${(utilization * 100).toFixed(1)}%`}
            />
          </div>
        );
      })}
    </div>
  );
}

// ─── CriticalDaysTable (FtlSection-style) ────────────────

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

function CriticalDayRow({ day }: { day: CriticalDay }) {
  const pct = day.required > 0 ? (day.required / day.available) * 100 : 0;
  const status = marginStatus(day.margin);

  return (
    <tr className={cn('border-b last:border-0', status === 'red' && 'bg-red-500/5')}>
      <td className="py-2.5 pl-4 pr-3 font-data text-sm">
        {new Date(day.date).toLocaleDateString('fr-FR', {
          weekday: 'short',
          day: '2-digit',
          month: 'short',
        })}
      </td>
      <td className="py-2.5 pr-3">
        <Badge variant="outline" className="font-data text-[10px]">
          {day.rank}
        </Badge>
      </td>
      <td className="py-2.5 pr-3 text-right font-data text-sm">{day.available}</td>
      <td className="py-2.5 pr-3 text-right font-data text-sm">{day.required}</td>
      <td
        className={cn(
          'py-2.5 pr-3 text-right font-data text-sm',
          status === 'red' && 'font-bold text-red-500'
        )}
      >
        {day.margin > 0 ? `+${day.margin}` : day.margin}
      </td>
      <td className="py-2.5 pr-3">
        <div className="flex items-center gap-2">
          <div className="h-1.5 min-w-[100px] flex-1 overflow-hidden rounded-full bg-border">
            <div
              className={cn(
                'h-full rounded-full transition-all duration-700',
                BAR_COLORS[status]
              )}
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

function CriticalDaysTable({ criticalDays }: { criticalDays: CriticalDay[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b text-left">
            <th className="py-2.5 pl-4 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Date
            </th>
            <th className="py-2.5 pr-3 font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Rang
            </th>
            <th className="py-2.5 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Disponible
            </th>
            <th className="py-2.5 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Requis
            </th>
            <th className="py-2.5 pr-3 text-right font-data text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
              Marge
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
          {criticalDays.map((day, i) => (
            <CriticalDayRow key={i} day={day} />
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ─── ParamRow ────────────────────────────────────────────

function ParamRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between py-1 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-data font-medium">{value}</span>
    </div>
  );
}

// ─── Main Component ──────────────────────────────────────

export function SizingResultView({ result }: SizingResultViewProps) {
  const allCriticalDays = [
    ...result.pntResult.criticalDays,
    ...result.pncResult.criticalDays,
  ].sort((a, b) => a.date.localeCompare(b.date));

  const pntTotalMin = ['CDB', 'OPL'].reduce(
    (s, r) => s + (result.pntResult.minimumCrewByRank[r] ?? 0),
    0
  );
  const pncTotalMin = ['CC', 'PNC'].reduce(
    (s, r) => s + (result.pncResult.minimumCrewByRank[r] ?? 0),
    0
  );

  return (
    <div className="space-y-6">
      {/* 1. Bannière faisabilité (AlertsBanner-style) */}
      {result.isBothFeasible ? (
        <Alert className="border-green-500/30 bg-green-500/10">
          <CheckCircle2 className="h-4 w-4 text-green-500" />
          <AlertTitle className="text-green-500">Programme couvrable</AlertTitle>
          <AlertDescription className="text-green-500/80">
            PNT ({pntTotalMin} min.) et PNC ({pncTotalMin} min.) — les deux categories sont
            couvrables. Temps total : {(result.totalSolveTimeMs / 1000).toFixed(1)}s.
          </AlertDescription>
        </Alert>
      ) : (
        <Alert className="border-red-500/30 bg-red-500/10">
          <AlertTriangle className="h-4 w-4 text-red-500" />
          <AlertTitle className="text-red-500">Programme non couvrable</AlertTitle>
          <AlertDescription className="text-red-500/80">
            Au moins une categorie ne peut pas etre couverte avec l'effectif disponible. Temps
            total : {(result.totalSolveTimeMs / 1000).toFixed(1)}s.
          </AlertDescription>
        </Alert>
      )}

      {/* 2. Cartes PNT + PNC (EngagementCard-style) */}
      <div className="grid gap-5 lg:grid-cols-2">
        <SizingEngagementCard
          label="PNT — Pilotes"
          icon={<Plane className="h-4 w-4" />}
          result={result.pntResult}
          ranks={['CDB', 'OPL']}
        />
        <SizingEngagementCard
          label="PNC — Cabine"
          icon={<Users className="h-4 w-4" />}
          result={result.pncResult}
          ranks={['CC', 'PNC']}
        />
      </div>

      {/* 3. Ventilation par rang (CategoryBreakdown-style) */}
      {(result.pntResult.isFeasible || result.pncResult.isFeasible) && (
        <CollapsibleSection
          title="Ventilation par rang — Effectifs minimaux"
          icon={<BarChart3 className="h-4 w-4" />}
        >
          <RankBreakdown pntResult={result.pntResult} pncResult={result.pncResult} />
        </CollapsibleSection>
      )}

      {/* 4. Jours critiques (FtlSection-style) */}
      {allCriticalDays.length > 0 && (
        <CollapsibleSection
          title={`Jours critiques (${allCriticalDays.length})`}
          icon={<Shield className="h-4 w-4" />}
        >
          <CriticalDaysTable criticalDays={allCriticalDays} />
        </CollapsibleSection>
      )}

      {/* 5. Paramètres solveur */}
      <CollapsibleSection
        title="Parametres du solveur"
        icon={<Settings className="h-4 w-4" />}
        defaultOpen={false}
      >
        <div className="grid grid-cols-2 gap-5 p-5 lg:grid-cols-4">
          <div>
            <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              PNT
            </h4>
            <ParamRow label="Statut" value={result.pntResult.status} />
            <ParamRow
              label="Temps resolution"
              value={`${(result.pntResult.solveTimeMs / 1000).toFixed(1)}s`}
            />
            <ParamRow label="Faisable" value={result.pntResult.isFeasible ? 'Oui' : 'Non'} />
            <ParamRow label="Contrainte" value={result.pntResult.bindingConstraint ?? '—'} />
          </div>
          <div>
            <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              PNC
            </h4>
            <ParamRow label="Statut" value={result.pncResult.status} />
            <ParamRow
              label="Temps resolution"
              value={`${(result.pncResult.solveTimeMs / 1000).toFixed(1)}s`}
            />
            <ParamRow label="Faisable" value={result.pncResult.isFeasible ? 'Oui' : 'Non'} />
            <ParamRow label="Contrainte" value={result.pncResult.bindingConstraint ?? '—'} />
          </div>
          <div>
            <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Effectifs minimaux
            </h4>
            {['CDB', 'OPL'].map((r) => (
              <ParamRow
                key={r}
                label={RANK_LABELS[r]}
                value={result.pntResult.minimumCrewByRank[r] ?? 0}
              />
            ))}
            {['CC', 'PNC'].map((r) => (
              <ParamRow
                key={r}
                label={RANK_LABELS[r]}
                value={result.pncResult.minimumCrewByRank[r] ?? 0}
              />
            ))}
          </div>
          <div>
            <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Marges
            </h4>
            {['CDB', 'OPL'].map((r) => (
              <ParamRow
                key={r}
                label={RANK_LABELS[r]}
                value={`${(result.pntResult.marginByRank[r] ?? 0) > 0 ? '+' : ''}${result.pntResult.marginByRank[r] ?? 0}`}
              />
            ))}
            {['CC', 'PNC'].map((r) => (
              <ParamRow
                key={r}
                label={RANK_LABELS[r]}
                value={`${(result.pncResult.marginByRank[r] ?? 0) > 0 ? '+' : ''}${result.pncResult.marginByRank[r] ?? 0}`}
              />
            ))}
          </div>
        </div>
      </CollapsibleSection>
    </div>
  );
}
