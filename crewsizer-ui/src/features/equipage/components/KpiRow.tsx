import { Card, CardContent } from '@/components/ui/card';
import { Users, AlertTriangle, Shield, UserCheck } from 'lucide-react';
import type { EquipageKpiDto } from '@/types/equipage';

interface KpiRowProps {
  kpi: EquipageKpiDto;
}

interface StatCardProps {
  label: string;
  value: number;
  sub?: string;
  icon: React.ReactNode;
  accent?: boolean;
  alert?: boolean;
}

function StatCard({ label, value, sub, icon, accent, alert }: StatCardProps) {
  return (
    <Card className={
      alert
        ? 'border-red-500/40 bg-red-500/5'
        : accent
          ? 'border-primary/30 bg-primary/5'
          : ''
    }>
      <CardContent className="flex items-center gap-3 p-4">
        <div className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${
          alert ? 'bg-red-500/15 text-red-400' : accent ? 'bg-primary/15 text-primary' : 'bg-muted text-muted-foreground'
        }`}>
          {icon}
        </div>
        <div>
          <p className="text-xs text-muted-foreground">{label}</p>
          <p className="font-data text-xl font-bold">{value}</p>
          {sub && <p className="text-xs text-muted-foreground">{sub}</p>}
        </div>
      </CardContent>
    </Card>
  );
}

export function KpiRow({ kpi }: KpiRowProps) {
  const totalAlertes = kpi.alertesExpirees + kpi.alertesProches + kpi.alertesAvertissement;

  return (
    <div className="grid grid-cols-3 gap-3 md:grid-cols-6">
      <StatCard
        label="Actifs"
        value={kpi.totalActifs}
        sub={`sur ${kpi.totalMembres}`}
        icon={<Users className="h-5 w-5" />}
        accent
      />
      <StatCard label="CDB" value={kpi.cdb} icon={<Shield className="h-5 w-5" />} />
      <StatCard label="OPL" value={kpi.opl} icon={<UserCheck className="h-5 w-5" />} />
      <StatCard label="CC" value={kpi.cc} icon={<UserCheck className="h-5 w-5" />} />
      <StatCard label="PNC" value={kpi.pnc} icon={<Users className="h-5 w-5" />} />
      <StatCard
        label="Alertes"
        value={totalAlertes}
        sub={totalAlertes > 0
          ? `${kpi.alertesExpirees} exp. / ${kpi.alertesProches} proche`
          : undefined}
        icon={<AlertTriangle className="h-5 w-5" />}
        alert={totalAlertes > 0}
      />
    </div>
  );
}
