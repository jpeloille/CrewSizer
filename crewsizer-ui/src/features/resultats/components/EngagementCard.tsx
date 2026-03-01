import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import type { ResultatGroupe, ResultatCategorie } from '@/types/resultat';
import { EngagementGauge } from './EngagementGauge';

interface EngagementCardProps {
  label: string;
  icon: React.ReactNode;
  groupe: ResultatGroupe;
  categories: ResultatCategorie[];
  effectifTotal?: number;
}

function statusColor(taux: number) {
  if (taux < 0.85) return { color: '#22c55e', label: 'Confortable', variant: 'bg-green-600' };
  if (taux < 0.95) return { color: '#f59e0b', label: 'Tendu', variant: 'bg-amber-500' };
  return { color: '#ef4444', label: 'Critique', variant: 'bg-red-500' };
}

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

export function EngagementCard({ label, icon, groupe, categories, effectifTotal }: EngagementCardProps) {
  const taux = Math.max(...categories.map((c) => c.tauxEngagement));
  const { color, label: statusLabel, variant } = statusColor(taux);
  const hasReduction = effectifTotal != null && effectifTotal > groupe.effectif;

  return (
    <Card>
      <CardHeader className="border-b">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2 text-sm font-semibold">
            {icon}
            {label}
            <Badge className={`${variant} text-white`}>{statusLabel}</Badge>
          </div>
          <span className="text-xs text-muted-foreground">
            {hasReduction ? (
              <>
                <span className="font-data font-semibold text-foreground">{groupe.effectif}</span>
                {' engageables / '}
                {effectifTotal} actifs
              </>
            ) : (
              <>{groupe.effectif} navigants actifs</>
            )}
          </span>
        </div>
      </CardHeader>
      <CardContent className="flex gap-6 pt-5">
        <EngagementGauge value={taux} color={color} />
        <div className="grid flex-1 grid-cols-2 gap-x-5 gap-y-3">
          <Kpi label="Cap. brute" value={`${groupe.capaciteBrute.toFixed(0)} h`} />
          <Kpi
            label="Abattements"
            value={`−${Math.abs(groupe.totalAbattements).toFixed(0)} h`}
          />
          <Kpi label="Cap. nette" value={`${groupe.capaciteNette.toFixed(0)} h`} />
          <Kpi label="Alpha (rendement)" value={groupe.alpha.toFixed(3)} />
          <Kpi
            label="H max (contrainte)"
            value={`${groupe.hMax.toFixed(1)} h`}
            sub={`butee ${groupe.contrainteMordante.toLowerCase()} mordante`}
          />
          <Kpi label="Cap. nette HDV" value={`${groupe.capaciteNetteHDV.toFixed(0)} h`} />
          <Kpi label="HDV / navigant" value={`${groupe.hdvParPersonne.toFixed(1)} h`} />
          <Kpi
            label="Contrainte mordante"
            value={
              <span className="inline-flex rounded bg-primary/10 px-2 py-0.5 font-data text-xs font-bold text-primary">
                {groupe.contrainteMordante.toUpperCase()}
              </span>
            }
          />
        </div>
      </CardContent>
    </Card>
  );
}
