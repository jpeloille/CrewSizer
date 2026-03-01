import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import { Loader2, CheckCircle2, AlertTriangle, Cpu } from 'lucide-react';
import type { SolveProgress } from '@/types/sizing';
import { useSetting } from '@/features/parametres/hooks/useSettingsQueries';

interface SolveProgressCardProps {
  progress: SolveProgress | null;
  isStarting?: boolean;
}

function getStatusInfo(progress: SolveProgress | null) {
  if (!progress || progress.solutionsFound === 0) {
    return {
      label: 'Initialisation...',
      color: 'text-blue-500',
      icon: <Loader2 className="h-4 w-4 animate-spin text-blue-500" />,
      badgeVariant: 'secondary' as const,
    };
  }

  const sinceImprovement = progress.secondsSinceLastImprovement ?? 0;

  if (sinceImprovement > 60) {
    return {
      label: 'Stagnation — le solver peut etre arrete',
      color: 'text-amber-500',
      icon: <AlertTriangle className="h-4 w-4 text-amber-500" />,
      badgeVariant: 'destructive' as const,
    };
  }

  if (sinceImprovement > 30) {
    return {
      label: 'Convergence — recherche de preuve d\'optimalite',
      color: 'text-blue-500',
      icon: <Cpu className="h-4 w-4 text-blue-500" />,
      badgeVariant: 'secondary' as const,
    };
  }

  return {
    label: 'Le solver progresse',
    color: 'text-green-500',
    icon: <CheckCircle2 className="h-4 w-4 text-green-500" />,
    badgeVariant: 'default' as const,
  };
}

export function SolveProgressCard({ progress, isStarting }: SolveProgressCardProps) {
  const { data: timeoutSetting } = useSetting('solver.timeout');
  const timeout = Number(timeoutSetting?.value) || 300;
  const elapsed = progress?.elapsedSeconds ?? 0;
  const percent = Math.min(100, (elapsed / timeout) * 100);
  const statusInfo = getStatusInfo(progress);

  if (isStarting && !progress) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-12">
          <Loader2 className="mr-3 h-6 w-6 animate-spin text-muted-foreground" />
          <span className="text-muted-foreground">Lancement du solver...</span>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg">Dimensionnement en cours</CardTitle>
          {progress?.currentCategory && (
            <Badge variant="outline">{progress.currentCategory}</Badge>
          )}
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Barre de progression */}
        <div className="space-y-1.5">
          <div className="flex justify-between text-sm text-muted-foreground">
            <span>{Math.floor(elapsed)}s ecoulees</span>
            <span>timeout {timeout}s</span>
          </div>
          <Progress value={percent} />
        </div>

        {/* Indicateur de statut */}
        <div className="flex items-center gap-2">
          {statusInfo.icon}
          <span className={`text-sm font-medium ${statusInfo.color}`}>
            {statusInfo.label}
          </span>
        </div>

        {/* Metriques */}
        <div className="grid grid-cols-3 gap-4">
          <div className="rounded-lg bg-muted/50 p-3 text-center">
            <div className="text-2xl font-bold tabular-nums">
              {progress?.solutionsFound ?? 0}
            </div>
            <div className="text-xs text-muted-foreground">Solutions trouvees</div>
          </div>
          <div className="rounded-lg bg-muted/50 p-3 text-center">
            <div className="text-2xl font-bold tabular-nums">
              {progress && progress.bestObjective > 0
                ? progress.bestObjective
                : '—'}
            </div>
            <div className="text-xs text-muted-foreground">Meilleur objectif</div>
          </div>
          <div className="rounded-lg bg-muted/50 p-3 text-center">
            <div className="text-2xl font-bold tabular-nums">
              {progress && progress.secondsSinceLastImprovement != null
                ? `${Math.floor(progress.secondsSinceLastImprovement)}s`
                : '—'}
            </div>
            <div className="text-xs text-muted-foreground">Depuis derniere amelioration</div>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
