import { Badge } from '@/components/ui/badge';
import type { ResultatCategorie } from '@/types/resultat';

interface CategoryBreakdownProps {
  categories: ResultatCategorie[];
}

function statusBadge(statut: string) {
  const s = statut.toUpperCase();
  if (s === 'OK' || s === 'CONFORTABLE')
    return <Badge className="bg-green-600 text-white">{statut}</Badge>;
  if (s === 'WARNING' || s === 'TENDU')
    return <Badge className="bg-amber-500 text-white">{statut}</Badge>;
  return <Badge variant="destructive">{statut}</Badge>;
}

function StatRow({ label, value, valueClass }: { label: string; value: React.ReactNode; valueClass?: string }) {
  return (
    <div className="flex items-center justify-between border-b border-dashed border-border py-1.5 text-sm last:border-0">
      <span className="text-muted-foreground">{label}</span>
      <span className={`font-data font-medium ${valueClass ?? ''}`}>{value}</span>
    </div>
  );
}

export function CategoryBreakdown({ categories }: CategoryBreakdownProps) {
  return (
    <div className="grid grid-cols-1 gap-4 p-5 sm:grid-cols-2 xl:grid-cols-4">
      {categories.map((cat) => (
        <div
          key={cat.nom}
          className="rounded-lg border bg-card p-4"
        >
          <div className="mb-3 flex items-center justify-between">
            <h4 className="text-sm font-semibold">{cat.nom}</h4>
            {statusBadge(cat.statut)}
          </div>
          <StatRow label="Effectif" value={cat.effectif} />
          <StatRow label="Capacite nette HDV" value={`${cat.capacite.toFixed(0)} h`} />
          <StatRow label="Besoin programme" value={`${cat.besoin.toFixed(0)} h`} />
          <StatRow
            label="Marge"
            value={`${cat.marge >= 0 ? '+' : ''}${cat.marge.toFixed(0)} h`}
            valueClass={cat.marge >= 0 ? 'text-green-500' : 'text-red-500'}
          />
          <StatRow
            label="Taux engagement (τ)"
            value={`${(cat.tauxEngagement * 100).toFixed(1)}%`}
          />
        </div>
      ))}
    </div>
  );
}
