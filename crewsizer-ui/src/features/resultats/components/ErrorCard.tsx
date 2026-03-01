import { useState } from 'react';
import { ChevronDown, AlertOctagon, AlertTriangle, Info } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { CalculError } from '../lib/parseErrors';

interface ErrorCardProps {
  error: CalculError;
  defaultExpanded?: boolean;
}

const SEVERITY_CONFIG = {
  fatal: {
    icon: AlertOctagon,
    label: 'FATAL',
    border: 'border-red-500/40',
    bg: 'bg-red-500/10',
    badge: 'bg-red-600 text-white',
    iconColor: 'text-red-500',
  },
  error: {
    icon: AlertTriangle,
    label: 'ERREUR',
    border: 'border-red-400/30',
    bg: 'bg-red-400/5',
    badge: 'bg-red-500 text-white',
    iconColor: 'text-red-400',
  },
  warning: {
    icon: Info,
    label: 'AVERTISSEMENT',
    border: 'border-amber-500/30',
    bg: 'bg-amber-500/5',
    badge: 'bg-amber-500 text-white',
    iconColor: 'text-amber-500',
  },
} as const;

export function ErrorCard({ error, defaultExpanded = false }: ErrorCardProps) {
  const [expanded, setExpanded] = useState(defaultExpanded);
  const config = SEVERITY_CONFIG[error.severity];
  const Icon = config.icon;

  return (
    <div className={cn('rounded-lg border', config.border, config.bg)}>
      <button
        type="button"
        className="flex w-full items-center gap-3 px-4 py-3 text-left"
        onClick={() => error.hint && setExpanded(!expanded)}
      >
        <Icon className={cn('h-4 w-4 shrink-0', config.iconColor)} />
        <span
          className={cn(
            'rounded px-1.5 py-0.5 font-data text-[10px] font-bold uppercase tracking-wider',
            config.badge
          )}
        >
          {config.label}
        </span>
        <span className="flex-1 text-sm font-medium">{error.message}</span>
        {error.hint && (
          <ChevronDown
            className={cn(
              'h-4 w-4 shrink-0 text-muted-foreground transition-transform duration-200',
              expanded && 'rotate-180'
            )}
          />
        )}
      </button>
      {expanded && error.hint && (
        <div className="border-t border-dashed border-border/50 px-4 py-3 pl-[4.25rem]">
          <p className="text-sm text-muted-foreground">{error.hint}</p>
        </div>
      )}
    </div>
  );
}
