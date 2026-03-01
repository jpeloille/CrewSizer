// ─── Aeroports Nouvelle-Caledonie ─────────────────────────
export const AIRPORTS: Record<string, { name: string; city: string }> = {
  GEA: { name: 'Magenta', city: 'Noumea' },
  NOU: { name: 'La Tontouta', city: 'Tontouta' },
  LIF: { name: 'Wanaham', city: 'Lifou' },
  MEE: { name: 'La Roche', city: 'Mare' },
  OVE: { name: 'Moue', city: 'Ouvea' },
  TOU: { name: 'Touho', city: 'Touho' },
  KNE: { name: 'Kone', city: 'Kone' },
  ILP: { name: 'Neatche', city: 'Ile des Pins' },
  BMY: { name: 'Waala', city: 'Belep' },
  KNQ: { name: 'Kone', city: 'Kone' },
  TGJ: { name: 'Tiga', city: 'Tiga' },
  VLI: { name: 'Vila', city: 'Port Vila' },
  SON: { name: 'Sonto', city: 'Sonto Pekoa' },
  TAH: { name: 'Tanah', city: 'Tanah' }
};

export const AP_KEYS = Object.keys(AIRPORTS);

// ─── Couleurs par destination ─────────────────────────────
export const ROUTE_COLORS: Record<string, string> = {
  LIF: '#06b6d4',
  MEE: '#8b5cf6',
  OVE: '#f59e0b',
  KNE: '#10b981',
  ILP: '#f472b6',
  TOU: '#ef4444',
  BMY: '#6366f1',
  TGJ: '#84cc16',
  GEA: '#94a3b8',
  NOU: '#3b82f6',
  KNQ: '#14b8a6',
  VLI: '#e879f9',
  SON: '#fb923c',
  TAH: '#a78bfa',
};

export function getRouteColor(dep: string, arr: string): string {
  return ROUTE_COLORS[arr] || ROUTE_COLORS[dep] || '#64748b';
}

// ─── Jours de la semaine ──────────────────────────────────
export const DAYS = [
  'Lundi',
  'Mardi',
  'Mercredi',
  'Jeudi',
  'Vendredi',
  'Samedi',
  'Dimanche',
] as const;

export const DAYS_SHORT = ['Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam', 'Dim'] as const;

export const SEASONS = [
  { value: 'BASSE', label: 'Basse saison' },
  { value: 'HAUTE', label: 'Haute saison' },
] as const;

// ─── Utilitaires de formatage ─────────────────────────────
export function hoursToHHMM(h: number): string {
  const hours = Math.floor(h);
  const minutes = Math.round((h - hours) * 60);
  return `${hours}h${minutes.toString().padStart(2, '0')}`;
}

export function timeToMinutes(time: string): number {
  if (!time) return 0;
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

export function diffTimeMinutes(start: string, end: string): number {
  return timeToMinutes(end) - timeToMinutes(start);
}

export function minutesToHHMM(m: number): string {
  const h = Math.floor(m / 60);
  const mm = m % 60;
  return `${h}h${mm.toString().padStart(2, '0')}`;
}

export function minutesToTime(totalMinutes: number): string {
  const normalized = ((totalMinutes % 1440) + 1440) % 1440;
  const h = Math.floor(normalized / 60);
  const m = normalized % 60;
  return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}`;
}
