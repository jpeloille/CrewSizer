import { useMemo, useState, useEffect } from 'react';
import { Button } from '@/components/ui/button';
import { useActiveScenario } from '@/hooks/useActiveScenario';
import { useSnapshots, useSnapshot, useRunCalcul } from './hooks/useCalculQueries';
import { useRunSizing } from './hooks/useSizingQueries';
import { AlertsBanner } from './components/AlertsBanner';
import { ErrorPanel } from './components/ErrorPanel';
import { EngagementCard } from './components/EngagementCard';
import { CategoryBreakdown } from './components/CategoryBreakdown';
import { FtlSection } from './components/FtlSection';
import { TsvVerification } from './components/TsvVerification';
import { ProgrammeDetail } from './components/ProgrammeDetail';
import { MonthlyBreakdown } from './components/MonthlyBreakdown';
import { CollapsibleSection } from './components/CollapsibleSection';
import { SizingResultView } from './components/SizingResultView';
import { SolveProgressCard } from './components/SolveProgressCard';
import { parseAlertes, parseHttpError } from './lib/parseErrors';
import type { ResultatMarge } from '@/types/resultat';
import type { CombinedSizingResult } from '@/types/sizing';
import {
  Play,
  Loader2,
  Plane,
  Users,
  BarChart3,
  Shield,
  Calendar,
  FileText,
  Settings,
} from 'lucide-react';

type Engine = 'marge' | 'cpsat';

export function ResultatsPage() {
  const { activeScenarioId } = useActiveScenario();
  const { data: snapshots, isLoading: snapshotsLoading } = useSnapshots(
    activeScenarioId ?? undefined
  );
  const runCalcul = useRunCalcul();
  const sizing = useRunSizing();

  const [selectedSnapshotId, setSelectedSnapshotId] = useState<string | null>(null);
  const [engine, setEngine] = useState<Engine>('marge');
  const sizingResult = sizing.result;

  // Auto-select latest snapshot
  useEffect(() => {
    if (snapshots?.length && !selectedSnapshotId) {
      setSelectedSnapshotId(snapshots[0].id);
    }
  }, [snapshots, selectedSnapshotId]);

  // Reset selection when scenario changes
  useEffect(() => {
    setSelectedSnapshotId(null);
  }, [activeScenarioId]);

  const { data: snapshotDetail } = useSnapshot(selectedSnapshotId);

  const resultat = useMemo<ResultatMarge | null>(() => {
    if (!snapshotDetail?.resultatJson) return null;
    try {
      return JSON.parse(snapshotDetail.resultatJson) as ResultatMarge;
    } catch {
      return null;
    }
  }, [snapshotDetail]);

  // Parse structured errors
  const isCalculError =
    resultat && snapshotDetail?.statutGlobal?.toUpperCase() === 'ERREUR';
  const calculErrors = useMemo(() => {
    if (!isCalculError || !resultat?.alertes) return [];
    return parseAlertes(resultat.alertes);
  }, [isCalculError, resultat]);

  const httpErrors = useMemo(() => {
    if (!runCalcul.isError) return [];
    return parseHttpError(runCalcul.error);
  }, [runCalcul.isError, runCalcul.error]);

  // Warnings only (non-fatal, non-error) for normal AlertsBanner
  const warningAlertes = useMemo(() => {
    if (!resultat?.alertes || isCalculError) return [];
    return resultat.alertes.filter(
      (a) => !a.startsWith('ERREUR:') && !a.startsWith('ALERTE:')
    );
  }, [resultat, isCalculError]);

  const handleRunCalcul = () => {
    if (!activeScenarioId) return;

    if (engine === 'marge') {
      runCalcul.mutate(activeScenarioId, {
        onSuccess: (data) => {
          setSelectedSnapshotId(data.id);
        },
      });
    } else {
      sizing.start(activeScenarioId);
    }
  };

  const isPending = engine === 'marge' ? runCalcul.isPending : sizing.isPending;

  if (!activeScenarioId) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Resultats</h1>
        <p className="text-muted-foreground">
          Aucun scenario actif. Selectionnez un scenario depuis le tableau de bord.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">
            {engine === 'marge'
              ? 'Resultats — Calcul de Marge d\'Engagement'
              : 'Resultats — Dimensionnement CP-SAT'}
          </h1>
          {engine === 'marge' && resultat && (
            <p className="mt-1 text-sm text-muted-foreground">
              Periode : {resultat.libellePeriode} ({resultat.nbJours} jours)
            </p>
          )}
        </div>
        <div className="flex items-center gap-3">
          {/* Engine selector */}
          <select
            value={engine}
            onChange={(e) => setEngine(e.target.value as Engine)}
            className="rounded-md border border-border bg-card px-3 py-2 text-sm text-foreground"
          >
            <option value="marge">Calculateur Marge</option>
            <option value="cpsat">Optimiseur CP-SAT</option>
          </select>

          {/* Snapshot selector (Marge only) */}
          {engine === 'marge' && snapshots && snapshots.length > 0 && (
            <select
              value={selectedSnapshotId ?? ''}
              onChange={(e) => setSelectedSnapshotId(e.target.value || null)}
              className="rounded-md border border-border bg-card px-3 py-2 font-data text-sm text-foreground"
            >
              {snapshots.map((s) => (
                <option key={s.id} value={s.id}>
                  {new Date(s.dateCalcul).toLocaleString('fr-FR')} —{' '}
                  {(s.tauxEngagementGlobal * 100).toFixed(1)}% ({s.statutGlobal})
                </option>
              ))}
            </select>
          )}

          <Button onClick={handleRunCalcul} disabled={isPending}>
            {isPending ? (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            ) : (
              <Play className="mr-2 h-4 w-4" />
            )}
            {engine === 'marge' ? 'Lancer un calcul' : 'Lancer le dimensionnement'}
          </Button>
        </div>
      </div>

      {/* Content */}
      {/* HTTP mutation error */}
      {httpErrors.length > 0 && <ErrorPanel errors={httpErrors} />}

      {/* CP-SAT sizing error */}
      {engine === 'cpsat' && sizing.isError && (
        <ErrorPanel errors={[{ message: sizing.error ?? 'Erreur du solver' }]} />
      )}

      {engine === 'cpsat' ? (
        /* ────── CP-SAT Results ────── */
        sizing.isPending ? (
          <SolveProgressCard progress={sizing.progress} isStarting={sizing.isStarting} />
        ) : sizingResult ? (
          <SizingResultView result={sizingResult} />
        ) : (
          !sizing.isError && (
            <p className="text-muted-foreground">
              Lancez le dimensionnement CP-SAT pour calculer les effectifs PNT et PNC.
            </p>
          )
        )
      ) : (
        /* ────── Marge Results (existing) ────── */
        <>
          {snapshotsLoading ? (
            <p className="text-muted-foreground">Chargement...</p>
          ) : !snapshots?.length && httpErrors.length === 0 ? (
            <p className="text-muted-foreground">
              Aucun calcul disponible. Lancez un calcul avec le bouton ci-dessus.
            </p>
          ) : !resultat ? (
            httpErrors.length === 0 && (
              <p className="text-muted-foreground">Chargement du resultat...</p>
            )
          ) : isCalculError ? (
            /* Calculation error — show ErrorPanel, no engagement/FTL cards */
            <ErrorPanel
              errors={calculErrors}
              timestamp={
                snapshotDetail
                  ? new Date(snapshotDetail.dateCalcul).toLocaleString('fr-FR')
                  : undefined
              }
            />
          ) : (
            <>
              {/* Alerts banner (warnings only) */}
              <AlertsBanner alertes={warningAlertes} />

              {/* Engagement cards PNT + PNC */}
              <div className="grid gap-5 lg:grid-cols-2">
                <EngagementCard
                  label="PNT — Pilotes"
                  icon={<Plane className="h-4 w-4" />}
                  groupe={resultat.pnt}
                  categories={[resultat.cdb, resultat.opl]}
                  effectifTotal={
                    resultat.effectifTotal
                      ? resultat.effectifTotal.cdb + resultat.effectifTotal.opl
                      : undefined
                  }
                />
                <EngagementCard
                  label="PNC — Cabine"
                  icon={<Users className="h-4 w-4" />}
                  groupe={resultat.pnc}
                  categories={[resultat.cc, resultat.pncDetail]}
                  effectifTotal={
                    resultat.effectifTotal
                      ? resultat.effectifTotal.cc + resultat.effectifTotal.pnc
                      : undefined
                  }
                />
              </div>

              {/* Category breakdown */}
              <CollapsibleSection
                title="Ventilation par categorie — Taux d'engagement"
                icon={<BarChart3 className="h-4 w-4" />}
              >
                <CategoryBreakdown
                  categories={[resultat.cdb, resultat.opl, resultat.cc, resultat.pncDetail]}
                />
              </CollapsibleSection>

              {/* Ventilation mensuelle */}
              {resultat.resultatsParMois && resultat.resultatsParMois.length > 0 && (
                <CollapsibleSection
                  title="Ventilation mensuelle"
                  icon={<Calendar className="h-4 w-4" />}
                >
                  <MonthlyBreakdown mois={resultat.resultatsParMois} />
                </CollapsibleSection>
              )}

              {/* Programme detail */}
              {(resultat.detailProgramme.length > 0 || resultat.resumeSemaine.length > 0) && (
                <CollapsibleSection
                  title="Detail du programme"
                  icon={<FileText className="h-4 w-4" />}
                >
                  <ProgrammeDetail
                    detailProgramme={resultat.detailProgramme}
                    resumeSemaine={resultat.resumeSemaine}
                  />
                </CollapsibleSection>
              )}

              {/* FTL verification */}
              <CollapsibleSection
                title="Verification FTL — EU-OPS Subpart Q"
                icon={<Shield className="h-4 w-4" />}
              >
                <FtlSection resultat={resultat} />
              </CollapsibleSection>

              {/* TSV verification per bloc */}
              {resultat.verificationsTSV && resultat.verificationsTSV.length > 0 && (
                <CollapsibleSection
                  title="Verification TSV par bloc"
                  icon={<Shield className="h-4 w-4" />}
                  defaultOpen={false}
                >
                  <TsvVerification
                    verifications={resultat.verificationsTSV}
                    tousBlocsConformes={resultat.tousBlocsConformesTSV}
                  />
                </CollapsibleSection>
              )}

              {/* Parameters */}
              <CollapsibleSection
                title="Parametres du calcul"
                icon={<Settings className="h-4 w-4" />}
                defaultOpen={false}
              >
                <div className="grid grid-cols-2 gap-5 p-5 lg:grid-cols-4">
                  <div>
                    <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Programme
                    </h4>
                    <ParamRow label="Jours dispo" value={resultat.nbJours} />
                    <ParamRow label="Total blocs" value={resultat.totalBlocs} />
                    <ParamRow label="HDV totale" value={`${resultat.totalHDV.toFixed(1)} h`} />
                    <ParamRow label="Rotations" value={resultat.rotations} />
                    <ParamRow
                      label="Etapes/rotation"
                      value={resultat.etapesParRotation.toFixed(1)}
                    />
                  </div>
                  <div>
                    <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Effectif PNT
                    </h4>
                    <ParamRow label="Effectif" value={resultat.pnt.effectif} />
                    <ParamRow label="CDB" value={resultat.cdb.effectif} />
                    <ParamRow label="OPL" value={resultat.opl.effectif} />
                    <ParamRow label="N min PNT" value={resultat.nMinPNT} />
                    <ParamRow label="Excedent" value={resultat.excedentPNT} />
                  </div>
                  <div>
                    <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Effectif PNC
                    </h4>
                    <ParamRow label="Effectif" value={resultat.pnc.effectif} />
                    <ParamRow label="CC" value={resultat.cc.effectif} />
                    <ParamRow label="PNC" value={resultat.pncDetail.effectif} />
                    <ParamRow label="N min PNC" value={resultat.nMinPNCGroupe} />
                    <ParamRow label="Excedent" value={resultat.excedentPNCGroupe} />
                  </div>
                  <div>
                    <h4 className="mb-2 font-data text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                      Cabine
                    </h4>
                    <ParamRow label="Rot. avec PNC" value={resultat.rotationsAvecPNC} />
                    <ParamRow label="Rot. sans PNC" value={resultat.rotationsSansPNC} />
                    <ParamRow label="Blocs absorbables" value={resultat.blocsAbsorbables} />
                    <ParamRow
                      label="Semaines periode"
                      value={resultat.nbSemainesPeriode}
                    />
                  </div>
                </div>
              </CollapsibleSection>
            </>
          )}
        </>
      )}
    </div>
  );
}

function ParamRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex justify-between py-1 text-sm">
      <span className="text-muted-foreground">{label}</span>
      <span className="font-data font-medium">{value}</span>
    </div>
  );
}
