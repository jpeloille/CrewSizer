import { useQuery } from '@tanstack/react-query';
import { scenariosApi } from '@/api/scenarios';
import { calculApi } from '@/api/calcul';
import { Card, CardContent } from '@/components/ui/card';
import { useActiveScenario } from '@/hooks/useActiveScenario';
import { useNavigate } from 'react-router';
import { FileText, Plane, Users, BarChart3 } from 'lucide-react';

function formatPeriode(dateDebut?: string, dateFin?: string) {
  if (!dateDebut || !dateFin) return '';
  const fmt = (d: string) => new Date(d).toLocaleDateString('fr-FR');
  return `${fmt(dateDebut)} — ${fmt(dateFin)}`;
}

function daysBetween(dateDebut?: string, dateFin?: string) {
  if (!dateDebut || !dateFin) return 0;
  const d = new Date(dateDebut);
  const f = new Date(dateFin);
  return Math.round((f.getTime() - d.getTime()) / 86400000) + 1;
}

export function DashboardPage() {
  const { activeScenarioId, setActiveScenarioId } = useActiveScenario();
  const navigate = useNavigate();

  const { data: scenarios, isLoading } = useQuery({
    queryKey: ['scenarios'],
    queryFn: scenariosApi.getAll,
  });

  const { data: activeScenario } = useQuery({
    queryKey: ['scenario', activeScenarioId],
    queryFn: () => scenariosApi.getById(activeScenarioId!),
    enabled: !!activeScenarioId,
  });

  const { data: snapshots } = useQuery({
    queryKey: ['calcul', 'snapshots', activeScenarioId],
    queryFn: () => calculApi.getSnapshots(activeScenarioId!),
    enabled: !!activeScenarioId,
  });

  const lastSnapshot = snapshots?.[0];

  // Auto-select first scenario if none active
  if (!activeScenarioId && scenarios?.length) {
    setActiveScenarioId(scenarios[0].id);
  }

  if (isLoading) {
    return <p className="text-muted-foreground">Chargement...</p>;
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Tableau de bord</h1>
        {scenarios && scenarios.length > 0 && (
          <select
            value={activeScenarioId ?? ''}
            onChange={(e) => setActiveScenarioId(e.target.value || null)}
            className="rounded-md border border-border bg-card px-3 py-2 font-data text-sm text-foreground"
          >
            {scenarios.map((s) => (
              <option key={s.id} value={s.id}>
                {s.nom} ({formatPeriode(s.dateDebut, s.dateFin)})
              </option>
            ))}
          </select>
        )}
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/15 text-primary">
              <FileText className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Scenario actif</p>
              <p className="font-data text-lg font-bold">
                {activeScenario?.nom ?? '-'}
              </p>
              <p className="text-xs text-muted-foreground">
                {activeScenario
                  ? formatPeriode(activeScenario.dateDebut, activeScenario.dateFin)
                  : 'Aucun scenario selectionne'}
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/15 text-primary">
              <Users className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Effectif</p>
              <p className="font-data text-lg font-bold">
                {activeScenario
                  ? activeScenario.cdb + activeScenario.opl + activeScenario.cc + activeScenario.pnc
                  : '-'}
              </p>
              <p className="text-xs text-muted-foreground">
                {activeScenario &&
                  `${activeScenario.cdb} CDB, ${activeScenario.opl} OPL, ${activeScenario.cc} CC, ${activeScenario.pnc} PNC`}
              </p>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="flex items-center gap-3 p-4">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/15 text-primary">
              <Plane className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Programme</p>
              <p className="font-data text-lg font-bold">
                {activeScenario ? daysBetween(activeScenario.dateDebut, activeScenario.dateFin) : '-'}
              </p>
              <p className="text-xs text-muted-foreground">
                jours dans la période
              </p>
            </div>
          </CardContent>
        </Card>

        <Card
          className={lastSnapshot ? 'cursor-pointer transition-colors hover:border-primary/30' : ''}
          onClick={() => lastSnapshot && navigate('/resultats')}
        >
          <CardContent className="flex items-center gap-3 p-4">
            <div
              className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${
                lastSnapshot
                  ? lastSnapshot.tauxEngagementGlobal >= 0.95
                    ? 'bg-red-500/15 text-red-500'
                    : lastSnapshot.tauxEngagementGlobal >= 0.85
                      ? 'bg-amber-500/15 text-amber-500'
                      : 'bg-green-500/15 text-green-500'
                  : 'bg-muted text-muted-foreground'
              }`}
            >
              <BarChart3 className="h-5 w-5" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">Calcul</p>
              {lastSnapshot ? (
                <>
                  <p className="font-data text-lg font-bold">
                    {(lastSnapshot.tauxEngagementGlobal * 100).toFixed(1)}%
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {lastSnapshot.statutGlobal} — {lastSnapshot.categorieContraignante}
                  </p>
                </>
              ) : (
                <>
                  <p className="font-data text-lg font-bold">-</p>
                  <p className="text-xs text-muted-foreground">
                    Aucun calcul effectue
                  </p>
                </>
              )}
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
