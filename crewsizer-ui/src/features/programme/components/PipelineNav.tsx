import { Plane, Layers, Grid3X3, Calendar, ArrowRight } from 'lucide-react';
import { cn } from '@/lib/utils';

interface PipelineStep {
  key: string;
  label: string;
  icon: React.ReactNode;
  count: string | number;
}

interface PipelineNavProps {
  activeTab: string;
  onTabChange: (tab: string) => void;
  volsCount: number;
  blocsCount: number;
  semainesCount: number;
  calendarWeeks: number;
}

const STEPS: Omit<PipelineStep, 'count'>[] = [
  { key: 'vols', label: 'Catalogue Vols', icon: <Plane className="h-4 w-4" /> },
  { key: 'blocs', label: 'Blocs de Vols', icon: <Layers className="h-4 w-4" /> },
  { key: 'semaines', label: 'Semaines Types', icon: <Grid3X3 className="h-4 w-4" /> },
  { key: 'calendrier', label: 'Calendrier', icon: <Calendar className="h-4 w-4" /> },
];

export function PipelineNav({
  activeTab,
  onTabChange,
  volsCount,
  blocsCount,
  semainesCount,
  calendarWeeks,
}: PipelineNavProps) {
  const counts: Record<string, string | number> = {
    vols: volsCount,
    blocs: blocsCount,
    semaines: semainesCount,
    calendrier: `${calendarWeeks} sem.`,
  };

  return (
    <div className="flex flex-wrap items-center gap-2 rounded-xl border border-border bg-card p-3.5">
      {STEPS.map((step, i) => (
        <div key={step.key} className="contents">
          {i > 0 && (
            <ArrowRight className="h-3 w-3 text-muted-foreground" />
          )}
          <button
            onClick={() => onTabChange(step.key)}
            className={cn(
              'flex items-center gap-1.5 rounded-md border border-transparent px-3 py-1.5 text-sm font-semibold transition-colors',
              'hover:bg-accent',
              activeTab === step.key &&
                'border-primary/30 bg-primary/8 text-primary'
            )}
          >
            <span
              className={cn(
                'flex h-5 w-5 items-center justify-center rounded-full font-data text-[10px] font-bold',
                activeTab === step.key
                  ? 'bg-primary/20 text-primary'
                  : 'bg-muted text-muted-foreground'
              )}
            >
              {i + 1}
            </span>
            {step.icon}
            <span>{step.label}</span>
            <span className="font-data text-xs text-muted-foreground">
              ({counts[step.key]})
            </span>
          </button>
        </div>
      ))}
    </div>
  );
}
