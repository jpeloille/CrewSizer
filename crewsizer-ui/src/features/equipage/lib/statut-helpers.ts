import type { StatutCheck } from '@/types/enums';

export function statutBadgeClasses(statut: StatutCheck): string {
  switch (statut) {
    case 'Valide':
      return 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/25';
    case 'ExpirationProche':
    case 'Avertissement':
      return 'bg-amber-500/15 text-amber-400 border border-amber-500/25';
    case 'Expire':
      return 'bg-red-500/15 text-red-400 border border-red-500/25';
    case 'NonApplicable':
    default:
      return 'bg-muted text-muted-foreground border border-border';
  }
}

export function statutLabel(statut: StatutCheck): string {
  switch (statut) {
    case 'Valide': return 'Valide';
    case 'ExpirationProche': return 'Proche';
    case 'Avertissement': return 'Avert.';
    case 'Expire': return 'Expire';
    case 'NonApplicable': return 'N/A';
    default: return '-';
  }
}

export function statutIcon(statut: StatutCheck): string {
  switch (statut) {
    case 'Valide': return '\u2713';
    case 'ExpirationProche':
    case 'Avertissement': return '\u26A0';
    case 'Expire': return '\u2717';
    default: return '\u25CB';
  }
}

export function statutColor(statut: StatutCheck): string {
  switch (statut) {
    case 'Valide': return 'text-emerald-400';
    case 'ExpirationProche':
    case 'Avertissement': return 'text-amber-400';
    case 'Expire': return 'text-red-400';
    default: return 'text-muted-foreground';
  }
}

export function statutBarColor(statut: StatutCheck): string {
  switch (statut) {
    case 'Valide': return 'bg-emerald-500';
    case 'ExpirationProche':
    case 'Avertissement': return 'bg-amber-500';
    case 'Expire': return 'bg-red-500';
    default: return 'bg-muted';
  }
}

export function statutCardClasses(statut: StatutCheck): string {
  switch (statut) {
    case 'Valide':
      return 'border-emerald-500/30 bg-emerald-500/5';
    case 'ExpirationProche':
    case 'Avertissement':
      return 'border-amber-500/30 bg-amber-500/5';
    case 'Expire':
      return 'border-red-500/30 bg-red-500/5';
    default:
      return 'border-border bg-muted/30';
  }
}

export function statutMatriceCellClasses(statut: StatutCheck): string {
  switch (statut) {
    case 'Valide':
      return 'bg-emerald-500/15 text-emerald-400';
    case 'ExpirationProche':
    case 'Avertissement':
      return 'bg-amber-500/15 text-amber-400';
    case 'Expire':
      return 'bg-red-500/15 text-red-400 font-semibold';
    default:
      return 'text-muted-foreground';
  }
}

export function formatDateFr(dateStr: string | null): string {
  if (!dateStr) return '-';
  return new Date(dateStr).toLocaleDateString('fr-FR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}
