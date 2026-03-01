import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';
import type { MembreDetailDto, StatutQualificationDto } from '@/types/equipage';
import { statutBadgeClasses, statutLabel, statutCardClasses, statutColor, formatDateFr } from '../lib/statut-helpers';

interface MembreDetailProps {
  membre: MembreDetailDto;
}

function InfoField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs text-muted-foreground">{label}</p>
      <p className="font-data text-sm font-medium">{value || '-'}</p>
    </div>
  );
}

function CheckCard({ q }: { q: StatutQualificationDto }) {
  const joursRestants = q.dateExpiration
    ? Math.ceil((new Date(q.dateExpiration).getTime() - Date.now()) / (1000 * 60 * 60 * 24))
    : null;

  return (
    <div className={`rounded-lg border p-3 ${statutCardClasses(q.statut)}`}>
      <div className="flex items-center justify-between">
        <span className="font-data text-sm font-semibold">{q.codeCheck}</span>
        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${statutBadgeClasses(q.statut)}`}>
          {statutLabel(q.statut)}
        </span>
      </div>
      <div className="mt-2 flex items-center justify-between text-xs text-muted-foreground">
        <span>{formatDateFr(q.dateExpiration)}</span>
        {joursRestants !== null && (
          <span className={`font-data font-semibold ${statutColor(q.statut)}`}>
            {joursRestants > 0 ? `${joursRestants}j` : `${joursRestants}j`}
          </span>
        )}
      </div>
    </div>
  );
}

export function MembreDetail({ membre }: MembreDetailProps) {
  const totalChecks = membre.nbChecksValides + membre.nbChecksExpires + membre.nbChecksAvertissement;

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg">
            {membre.nom}
          </CardTitle>
          <div className="flex items-center gap-2">
            <Badge variant="outline" className="font-data">{membre.grade}</Badge>
            <Badge variant="outline" className="font-data">{membre.contrat}</Badge>
            <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${statutBadgeClasses(membre.statutGlobal)}`}>
              {statutLabel(membre.statutGlobal)}
            </span>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-3 gap-4">
          <InfoField label="Code" value={membre.code} />
          <InfoField label="Matricule" value={membre.matricule} />
          <InfoField label="Categorie" value={membre.categorie} />
          <InfoField label="Date entree" value={formatDateFr(membre.dateEntree)} />
          <InfoField label="Type avion" value={membre.typeAvion} />
          <InfoField label="Bases" value={membre.bases.join(', ')} />
        </div>
        {membre.roles.length > 0 && (
          <InfoField label="Roles" value={membre.roles.join(', ')} />
        )}

        <Separator />

        <div>
          <div className="mb-3 flex items-center gap-2">
            <h4 className="text-sm font-semibold">
              Qualifications
            </h4>
            <span className="font-data text-xs text-muted-foreground">
              {membre.nbChecksValides}/{totalChecks}
            </span>
            {membre.nbChecksExpires > 0 && (
              <span className="inline-flex items-center rounded-full border border-red-500/25 bg-red-500/15 px-2 py-0.5 text-xs font-medium text-red-400">
                {membre.nbChecksExpires} expire(s)
              </span>
            )}
            {membre.nbChecksAvertissement > 0 && (
              <span className="inline-flex items-center rounded-full border border-amber-500/25 bg-amber-500/15 px-2 py-0.5 text-xs font-medium text-amber-400">
                {membre.nbChecksAvertissement} avert.
              </span>
            )}
          </div>

          {membre.qualifications.length > 0 ? (
            <div className="grid grid-cols-2 gap-2 xl:grid-cols-3">
              {membre.qualifications.map((q) => (
                <CheckCard key={q.codeCheck} q={q} />
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Aucune qualification.</p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
