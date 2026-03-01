import { Alert, AlertTitle, AlertDescription } from '@/components/ui/alert';
import { AlertTriangle, CheckCircle2 } from 'lucide-react';

interface AlertsBannerProps {
  alertes: string[];
}

export function AlertsBanner({ alertes }: AlertsBannerProps) {
  if (alertes.length === 0) {
    return (
      <Alert className="border-green-500/30 bg-green-500/10">
        <CheckCircle2 className="h-4 w-4 text-green-500" />
        <AlertTitle className="text-green-500">Aucune alerte</AlertTitle>
        <AlertDescription className="text-green-500/80">
          Tous les indicateurs sont conformes.
        </AlertDescription>
      </Alert>
    );
  }

  const hasCritique = alertes.some(
    (a) => a.includes('CRITIQUE') || a.includes('DEPASSEMENT')
  );

  return (
    <Alert
      className={
        hasCritique
          ? 'border-red-500/30 bg-red-500/10'
          : 'border-amber-500/30 bg-amber-500/10'
      }
    >
      <AlertTriangle
        className={`h-4 w-4 ${hasCritique ? 'text-red-500' : 'text-amber-500'}`}
      />
      <AlertTitle className={hasCritique ? 'text-red-500' : 'text-amber-500'}>
        {alertes.length} alerte{alertes.length > 1 ? 's' : ''} detectee
        {alertes.length > 1 ? 's' : ''}
      </AlertTitle>
      <AlertDescription>
        <ul className="mt-1 space-y-1">
          {alertes.map((alerte, i) => {
            const isCrit =
              alerte.includes('CRITIQUE') || alerte.includes('DEPASSEMENT');
            return (
              <li
                key={i}
                className={
                  isCrit
                    ? 'font-medium text-red-500'
                    : 'text-muted-foreground'
                }
              >
                {alerte}
              </li>
            );
          })}
        </ul>
      </AlertDescription>
    </Alert>
  );
}
