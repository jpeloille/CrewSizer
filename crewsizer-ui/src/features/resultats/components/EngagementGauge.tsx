interface EngagementGaugeProps {
  /** 0–1 ratio */
  value: number;
  /** Tailwind-compatible color string, e.g. "#f59e0b" */
  color: string;
  /** Diameter in px */
  size?: number;
}

export function EngagementGauge({ value, color, size = 140 }: EngagementGaugeProps) {
  const radius = 50;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference * (1 - Math.min(value, 1));

  return (
    <div className="relative shrink-0" style={{ width: size, height: size }}>
      <svg
        viewBox="0 0 120 120"
        className="h-full w-full"
        style={{ transform: 'rotate(-90deg)' }}
      >
        <circle
          cx="60"
          cy="60"
          r={radius}
          fill="none"
          className="stroke-border"
          strokeWidth="10"
        />
        <circle
          cx="60"
          cy="60"
          r={radius}
          fill="none"
          stroke={color}
          strokeWidth="10"
          strokeLinecap="round"
          strokeDasharray={circumference}
          strokeDashoffset={offset}
          className="transition-[stroke-dashoffset] duration-1000 ease-out"
        />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <span
          className="font-data text-[28px] font-bold leading-none tracking-tight"
          style={{ color }}
        >
          {(value * 100).toFixed(0)}%
        </span>
        <span className="mt-0.5 text-[11px] text-muted-foreground">engagement</span>
      </div>
    </div>
  );
}
