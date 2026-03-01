/** Returns the number of days between two date strings (inclusive). */
export function daysBetween(debut: string, fin: string): number {
  if (!debut || !fin) return 0;
  const d = new Date(debut);
  const f = new Date(fin);
  return Math.round((f.getTime() - d.getTime()) / 86400000) + 1;
}

/** Returns ISO 8601 week number for a given date. */
export function getIsoWeek(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7));
  const yearStart = new Date(Date.UTC(d.getUTCFullYear(), 0, 1));
  return Math.ceil(((d.getTime() - yearStart.getTime()) / 86400000 + 1) / 7);
}

/** Returns ISO 8601 week-based year for a given date. */
export function getIsoYear(date: Date): number {
  const d = new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()));
  d.setUTCDate(d.getUTCDate() + 4 - (d.getUTCDay() || 7));
  return d.getUTCFullYear();
}

export interface IsoWeek {
  week: number;
  year: number;
}

/** Generate all ISO weeks covering a date range. */
export function getIsoWeeksForPeriod(dateDebut: string, dateFin: string): IsoWeek[] {
  if (!dateDebut || !dateFin) return [];
  const start = new Date(dateDebut);
  const end = new Date(dateFin);
  const result: IsoWeek[] = [];
  const seen = new Set<string>();

  const current = new Date(start);
  while (current <= end) {
    const w = getIsoWeek(current);
    const y = getIsoYear(current);
    const key = `${y}-${w}`;
    if (!seen.has(key)) {
      seen.add(key);
      result.push({ week: w, year: y });
    }
    current.setDate(current.getDate() + 1);
  }
  return result;
}

const MONTH_NAMES = [
  'Janvier', 'Fevrier', 'Mars', 'Avril', 'Mai', 'Juin',
  'Juillet', 'Aout', 'Septembre', 'Octobre', 'Novembre', 'Decembre',
];

/** Group ISO weeks by quarter for display. */
export function groupByQuarter(weeks: IsoWeek[]): { label: string; weeks: IsoWeek[] }[] {
  const groups = new Map<string, IsoWeek[]>();
  for (const w of weeks) {
    const q = Math.ceil(w.week / 13);
    const key = `${w.year}-T${q}`;
    if (!groups.has(key)) groups.set(key, []);
    groups.get(key)!.push(w);
  }

  return Array.from(groups.entries()).map(([key, ws]) => {
    const [year, quarter] = key.split('-');
    const qIdx = parseInt(quarter.slice(1)) - 1;
    const startMonth = MONTH_NAMES[qIdx * 3] ?? '';
    const endMonth = MONTH_NAMES[qIdx * 3 + 2] ?? '';
    return {
      label: `${year} T${qIdx + 1} — ${startMonth} – ${endMonth}`,
      weeks: ws,
    };
  });
}
