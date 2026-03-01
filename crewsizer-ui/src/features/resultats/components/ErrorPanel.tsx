import { XCircle, AlertTriangle } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { CalculError } from '../lib/parseErrors';
import { ErrorCard } from './ErrorCard';

interface ErrorPanelProps {
  errors: CalculError[];
  timestamp?: string;
}

export function ErrorPanel({ errors, timestamp }: ErrorPanelProps) {
  const fatals = errors.filter((e) => e.severity === 'fatal');
  const errs = errors.filter((e) => e.severity === 'error');
  const warns = errors.filter((e) => e.severity === 'warning');

  const hasFatal = fatals.length > 0;
  const title = hasFatal ? 'Calcul impossible' : 'Calcul avec erreurs';

  return (
    <div className="space-y-4">
      {/* Summary banner */}
      <div
        className={cn(
          'flex items-center gap-3 rounded-lg border px-5 py-4',
          hasFatal
            ? 'border-red-500/30 bg-red-500/10'
            : 'border-amber-500/30 bg-amber-500/10'
        )}
      >
        {hasFatal ? (
          <XCircle className="h-5 w-5 shrink-0 text-red-500" />
        ) : (
          <AlertTriangle className="h-5 w-5 shrink-0 text-amber-500" />
        )}
        <div className="flex-1">
          <h3
            className={cn(
              'text-sm font-semibold',
              hasFatal ? 'text-red-500' : 'text-amber-500'
            )}
          >
            {title}
          </h3>
          {timestamp && (
            <p className="mt-0.5 text-xs text-muted-foreground">{timestamp}</p>
          )}
        </div>

        {/* Severity pills */}
        <div className="flex items-center gap-2">
          {fatals.length > 0 && (
            <span className="rounded-full bg-red-600 px-2.5 py-0.5 font-data text-xs font-bold text-white">
              {fatals.length} fatale{fatals.length > 1 ? 's' : ''}
            </span>
          )}
          {errs.length > 0 && (
            <span className="rounded-full bg-red-500 px-2.5 py-0.5 font-data text-xs font-bold text-white">
              {errs.length} erreur{errs.length > 1 ? 's' : ''}
            </span>
          )}
          {warns.length > 0 && (
            <span className="rounded-full bg-amber-500 px-2.5 py-0.5 font-data text-xs font-bold text-white">
              {warns.length} avertissement{warns.length > 1 ? 's' : ''}
            </span>
          )}
        </div>
      </div>

      {/* Error cards */}
      <div className="space-y-2">
        {fatals.map((err, i) => (
          <ErrorCard key={`fatal-${i}`} error={err} defaultExpanded />
        ))}
        {errs.map((err, i) => (
          <ErrorCard key={`error-${i}`} error={err} />
        ))}
        {warns.map((err, i) => (
          <ErrorCard key={`warn-${i}`} error={err} />
        ))}
      </div>
    </div>
  );
}
