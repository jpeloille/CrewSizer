import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ArrowLeft, Save, Plus, Trash2 } from 'lucide-react';
import { programmeApi } from '@/api/programme';
import { useScenario, useUpdateScenario } from './hooks/useScenarioQueries';
import { useEquipageKpi } from '@/features/equipage/hooks/useEquipageQueries';
import { daysBetween, getIsoWeeksForPeriod } from '@/lib/dates';
import type {
  ScenarioDto,
  FonctionSolDto,
  AbattementDto,
  EntreeTsvMaxDto,
  AffectationSemaineDto,
} from '@/types/scenario';
import type { SemaineTypeDto } from '@/types/semaine';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Badge } from '@/components/ui/badge';

// ─── Form State ────────────────────────────────────────────────
interface FormState {
  nom: string;
  description: string;
  dateDebut: string;
  dateFin: string;
  cdb: number;
  opl: number;
  cc: number;
  pnc: number;
  offReglementaire: number;
  offAccordEntreprise: number;
  tsvMaxJournalier: number;
  tsvMoyenRetenu: number;
  reposMinimum: number;
  h28Max: number;
  h90Max: number;
  h12Max: number;
  cumulPntCumul28: number;
  cumulPntCumul90: number;
  cumulPntCumul12: number;
  cumulPncCumul28: number;
  cumulPncCumul90: number;
  cumulPncCumul12: number;
  tsMax7j: number;
  tsMax14j: number;
  tsMax28j: number;
  fonctionsSolPNT: FonctionSolDto[];
  fonctionsSolPNC: FonctionSolDto[];
  abattementsPNT: AbattementDto[];
  abattementsPNC: AbattementDto[];
  tableTsvMax: EntreeTsvMaxDto[];
  calendrier: AffectationSemaineDto[];
}

function dtoToForm(dto: ScenarioDto): FormState {
  return {
    nom: dto.nom,
    description: dto.description ?? '',
    dateDebut: dto.dateDebut,
    dateFin: dto.dateFin,
    cdb: dto.cdb,
    opl: dto.opl,
    cc: dto.cc,
    pnc: dto.pnc,
    offReglementaire: dto.offReglementaire,
    offAccordEntreprise: dto.offAccordEntreprise,
    tsvMaxJournalier: dto.tsvMaxJournalier,
    tsvMoyenRetenu: dto.tsvMoyenRetenu,
    reposMinimum: dto.reposMinimum,
    h28Max: dto.h28Max,
    h90Max: dto.h90Max,
    h12Max: dto.h12Max,
    cumulPntCumul28: dto.cumulPntCumul28,
    cumulPntCumul90: dto.cumulPntCumul90,
    cumulPntCumul12: dto.cumulPntCumul12,
    cumulPncCumul28: dto.cumulPncCumul28,
    cumulPncCumul90: dto.cumulPncCumul90,
    cumulPncCumul12: dto.cumulPncCumul12,
    tsMax7j: dto.tsMax7j,
    tsMax14j: dto.tsMax14j,
    tsMax28j: dto.tsMax28j,
    fonctionsSolPNT: [...dto.fonctionsSolPNT],
    fonctionsSolPNC: [...dto.fonctionsSolPNC],
    abattementsPNT: [...dto.abattementsPNT],
    abattementsPNC: [...dto.abattementsPNC],
    tableTsvMax: dto.tableTsvMax.map((e) => ({ ...e, maxParEtapes: { ...e.maxParEtapes } })),
    calendrier: [...dto.calendrier],
  };
}

function formToDto(form: FormState, original: ScenarioDto): ScenarioDto {
  return {
    ...original,
    nom: form.nom,
    description: form.description || null,
    dateDebut: form.dateDebut,
    dateFin: form.dateFin,
    cdb: form.cdb,
    opl: form.opl,
    cc: form.cc,
    pnc: form.pnc,
    offReglementaire: form.offReglementaire,
    offAccordEntreprise: form.offAccordEntreprise,
    tsvMaxJournalier: form.tsvMaxJournalier,
    tsvMoyenRetenu: form.tsvMoyenRetenu,
    reposMinimum: form.reposMinimum,
    h28Max: form.h28Max,
    h90Max: form.h90Max,
    h12Max: form.h12Max,
    cumulPntCumul28: form.cumulPntCumul28,
    cumulPntCumul90: form.cumulPntCumul90,
    cumulPntCumul12: form.cumulPntCumul12,
    cumulPncCumul28: form.cumulPncCumul28,
    cumulPncCumul90: form.cumulPncCumul90,
    cumulPncCumul12: form.cumulPncCumul12,
    tsMax7j: form.tsMax7j,
    tsMax14j: form.tsMax14j,
    tsMax28j: form.tsMax28j,
    fonctionsSolPNT: form.fonctionsSolPNT,
    fonctionsSolPNC: form.fonctionsSolPNC,
    abattementsPNT: form.abattementsPNT,
    abattementsPNC: form.abattementsPNC,
    tableTsvMax: form.tableTsvMax,
    calendrier: form.calendrier,
  };
}

const defaultForm: FormState = {
  nom: '',
  description: '',
  dateDebut: '',
  dateFin: '',
  cdb: 0,
  opl: 0,
  cc: 0,
  pnc: 0,
  offReglementaire: 8,
  offAccordEntreprise: 0,
  tsvMaxJournalier: 13,
  tsvMoyenRetenu: 11,
  reposMinimum: 12,
  h28Max: 100,
  h90Max: 280,
  h12Max: 900,
  cumulPntCumul28: 0,
  cumulPntCumul90: 0,
  cumulPntCumul12: 0,
  cumulPncCumul28: 0,
  cumulPncCumul90: 0,
  cumulPncCumul12: 0,
  tsMax7j: 60,
  tsMax14j: 110,
  tsMax28j: 190,
  fonctionsSolPNT: [],
  fonctionsSolPNC: [],
  abattementsPNT: [],
  abattementsPNC: [],
  tableTsvMax: [],
  calendrier: [],
};

// ─── Field Component ───────────────────────────────────────────
function Field({
  label,
  children,
  className,
}: {
  label: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={className}>
      <Label className="mb-1 text-xs text-muted-foreground">{label}</Label>
      {children}
    </div>
  );
}

function NumberInput({
  value,
  onChange,
  step,
  min,
}: {
  value: number;
  onChange: (v: number) => void;
  step?: number;
  min?: number;
}) {
  return (
    <Input
      type="number"
      value={value}
      onChange={(e) => onChange(Number(e.target.value))}
      step={step}
      min={min}
      className="font-data"
    />
  );
}

// ─── Main Component ────────────────────────────────────────────
export function ScenarioEditPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: scenario, isLoading } = useScenario(id!);
  const updateMutation = useUpdateScenario();
  const { data: kpi } = useEquipageKpi();
  const [form, setForm] = useState<FormState>(defaultForm);
  const [loaded, setLoaded] = useState(false);

  const { data: semaineTypes = [] } = useQuery<SemaineTypeDto[]>({
    queryKey: ['semaines'],
    queryFn: programmeApi.getSemaines,
  });

  useEffect(() => {
    if (scenario && !loaded) {
      setForm(dtoToForm(scenario));
      setLoaded(true);
    }
  }, [scenario, loaded]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  const nbJours = useMemo(() => daysBetween(form.dateDebut, form.dateFin), [form.dateDebut, form.dateFin]);

  const weeks = useMemo(() => {
    const isoWeeks = getIsoWeeksForPeriod(form.dateDebut, form.dateFin);
    return isoWeeks.map((w) => ({ semaine: w.week, annee: w.year }));
  }, [form.dateDebut, form.dateFin]);

  function handleSave() {
    if (!scenario || !id) return;
    if (!form.nom.trim()) {
      toast.error('Le nom est obligatoire');
      return;
    }
    if (!form.dateDebut || !form.dateFin) {
      toast.error('Les dates de periode sont obligatoires');
      return;
    }
    updateMutation.mutate({ id, data: formToDto(form, scenario) });
  }

  // ─── Collection helpers ───────────────────────────────────
  function addFonctionSol(key: 'fonctionsSolPNT' | 'fonctionsSolPNC') {
    set(key, [...form[key], { nom: '', nbPersonnes: 1, joursSolMois: 0 }]);
  }

  function updateFonctionSol(
    key: 'fonctionsSolPNT' | 'fonctionsSolPNC',
    index: number,
    field: keyof FonctionSolDto,
    value: string | number,
  ) {
    const updated = [...form[key]];
    updated[index] = { ...updated[index], [field]: value };
    set(key, updated);
  }

  function removeFonctionSol(key: 'fonctionsSolPNT' | 'fonctionsSolPNC', index: number) {
    set(key, form[key].filter((_, i) => i !== index));
  }

  function addAbattement(key: 'abattementsPNT' | 'abattementsPNC') {
    set(key, [...form[key], { libelle: '', joursPersonnel: 0 }]);
  }

  function updateAbattement(
    key: 'abattementsPNT' | 'abattementsPNC',
    index: number,
    field: keyof AbattementDto,
    value: string | number,
  ) {
    const updated = [...form[key]];
    updated[index] = { ...updated[index], [field]: value };
    set(key, updated);
  }

  function removeAbattement(key: 'abattementsPNT' | 'abattementsPNC', index: number) {
    set(key, form[key].filter((_, i) => i !== index));
  }

  function addTsvBande() {
    set('tableTsvMax', [
      ...form.tableTsvMax,
      { debutBande: '06:00', finBande: '07:00', maxParEtapes: {} },
    ]);
  }

  function updateTsvBande(index: number, field: keyof EntreeTsvMaxDto, value: unknown) {
    const updated = [...form.tableTsvMax];
    updated[index] = { ...updated[index], [field]: value };
    set('tableTsvMax', updated);
  }

  function removeTsvBande(index: number) {
    set('tableTsvMax', form.tableTsvMax.filter((_, i) => i !== index));
  }

  function updateTsvEtape(bandeIdx: number, nbEtapes: number, maxTsv: number) {
    const updated = [...form.tableTsvMax];
    updated[bandeIdx] = {
      ...updated[bandeIdx],
      maxParEtapes: { ...updated[bandeIdx].maxParEtapes, [nbEtapes]: maxTsv },
    };
    set('tableTsvMax', updated);
  }

  function removeTsvEtape(bandeIdx: number, nbEtapes: number) {
    const updated = [...form.tableTsvMax];
    const mpe = { ...updated[bandeIdx].maxParEtapes };
    delete mpe[nbEtapes];
    updated[bandeIdx] = { ...updated[bandeIdx], maxParEtapes: mpe };
    set('tableTsvMax', updated);
  }

  function updateCalendrierAffectation(semaine: number, annee: number, semaineTypeId: string) {
    const stRef = semaineTypes.find((s) => s.id === semaineTypeId)?.reference ?? '';
    const updated = form.calendrier.filter(
      (a) => !(a.semaine === semaine && a.annee === annee),
    );
    if (semaineTypeId) {
      updated.push({ semaine, annee, semaineTypeId, semaineTypeRef: stRef });
    }
    set('calendrier', updated);
  }

  // ─── Render ───────────────────────────────────────────────
  if (isLoading || !loaded) {
    return <p className="text-muted-foreground">Chargement du scenario...</p>;
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Button variant="ghost" size="icon" onClick={() => navigate('/scenarios')}>
            <ArrowLeft className="h-5 w-5" />
          </Button>
          <h1 className="text-2xl font-bold">{form.nom || 'Scenario'}</h1>
          {nbJours > 0 && (
            <Badge variant="secondary" className="font-data">
              {nbJours} jours
            </Badge>
          )}
        </div>
        <Button onClick={handleSave} disabled={updateMutation.isPending}>
          <Save className="mr-2 h-4 w-4" />
          {updateMutation.isPending ? 'Enregistrement...' : 'Enregistrer'}
        </Button>
      </div>

      {/* Tabs */}
      <Tabs defaultValue="general">
        <TabsList>
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="limites">Limites FTL</TabsTrigger>
          <TabsTrigger value="abattements">Abattements</TabsTrigger>
          <TabsTrigger value="tsv">TSV Max</TabsTrigger>
          <TabsTrigger value="calendrier">Calendrier</TabsTrigger>
        </TabsList>

        {/* ═══ Onglet General ═══ */}
        <TabsContent value="general" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            {/* Identification */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Identification</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <Field label="Nom">
                  <Input
                    value={form.nom}
                    onChange={(e) => set('nom', e.target.value)}
                    placeholder="Nom du scenario"
                  />
                </Field>
                <Field label="Description">
                  <Input
                    value={form.description}
                    onChange={(e) => set('description', e.target.value)}
                    placeholder="Description (optionnel)"
                  />
                </Field>
              </CardContent>
            </Card>

            {/* Periode */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Periode</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="grid grid-cols-2 gap-3">
                  <Field label="Date debut">
                    <Input
                      type="date"
                      value={form.dateDebut}
                      onChange={(e) => set('dateDebut', e.target.value)}
                      className="font-data"
                    />
                  </Field>
                  <Field label="Date fin">
                    <Input
                      type="date"
                      value={form.dateFin}
                      onChange={(e) => set('dateFin', e.target.value)}
                      className="font-data"
                    />
                  </Field>
                </div>
                {nbJours > 0 && (
                  <p className="text-sm text-muted-foreground">
                    Duree : <span className="font-data font-semibold">{nbJours}</span> jours
                    ({weeks.length} semaines ISO)
                  </p>
                )}
              </CardContent>
            </Card>

            {/* Effectif (depuis import APM — lecture seule) */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Effectif</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-4 gap-3">
                  <Field label="CDB">
                    <div className="font-data text-lg font-semibold">{kpi?.cdb ?? '—'}</div>
                  </Field>
                  <Field label="OPL">
                    <div className="font-data text-lg font-semibold">{kpi?.opl ?? '—'}</div>
                  </Field>
                  <Field label="CC">
                    <div className="font-data text-lg font-semibold">{kpi?.cc ?? '—'}</div>
                  </Field>
                  <Field label="PNC">
                    <div className="font-data text-lg font-semibold">{kpi?.pnc ?? '—'}</div>
                  </Field>
                </div>
                <p className="mt-2 text-sm text-muted-foreground">
                  Total : <span className="font-data font-semibold">
                    {kpi ? kpi.cdb + kpi.opl + kpi.cc + kpi.pnc : 0}
                  </span> membres (import APM)
                </p>
              </CardContent>
            </Card>

            {/* Jours OFF */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Jours OFF</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 gap-3">
                  <Field label="OFF reglementaire">
                    <NumberInput
                      value={form.offReglementaire}
                      onChange={(v) => set('offReglementaire', v)}
                      min={0}
                    />
                  </Field>
                  <Field label="OFF accord entreprise">
                    <NumberInput
                      value={form.offAccordEntreprise}
                      onChange={(v) => set('offAccordEntreprise', v)}
                      min={0}
                    />
                  </Field>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* ═══ Onglet Limites FTL ═══ */}
        <TabsContent value="limites" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            {/* Limites FTL */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Limites FTL</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <Field label="TSV max journalier (h)">
                  <NumberInput
                    value={form.tsvMaxJournalier}
                    onChange={(v) => set('tsvMaxJournalier', v)}
                    step={0.5}
                  />
                </Field>
                <Field label="TSV moyen retenu (h)">
                  <NumberInput
                    value={form.tsvMoyenRetenu}
                    onChange={(v) => set('tsvMoyenRetenu', v)}
                    step={0.5}
                  />
                </Field>
                <Field label="Repos minimum (h)">
                  <NumberInput
                    value={form.reposMinimum}
                    onChange={(v) => set('reposMinimum', v)}
                    step={0.5}
                  />
                </Field>
              </CardContent>
            </Card>

            {/* Limites cumulatives */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Limites cumulatives (HDV)</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <Field label="Max 28 jours (h)">
                  <NumberInput value={form.h28Max} onChange={(v) => set('h28Max', v)} />
                </Field>
                <Field label="Max 90 jours (h)">
                  <NumberInput value={form.h90Max} onChange={(v) => set('h90Max', v)} />
                </Field>
                <Field label="Max 12 mois (h)">
                  <NumberInput value={form.h12Max} onChange={(v) => set('h12Max', v)} />
                </Field>
                <div className="border-t pt-3">
                  <p className="mb-2 text-xs font-medium text-muted-foreground">Cumuls entrants PNT</p>
                  <div className="grid grid-cols-3 gap-2">
                    <Field label="28j">
                      <NumberInput
                        value={form.cumulPntCumul28}
                        onChange={(v) => set('cumulPntCumul28', v)}
                      />
                    </Field>
                    <Field label="90j">
                      <NumberInput
                        value={form.cumulPntCumul90}
                        onChange={(v) => set('cumulPntCumul90', v)}
                      />
                    </Field>
                    <Field label="12m">
                      <NumberInput
                        value={form.cumulPntCumul12}
                        onChange={(v) => set('cumulPntCumul12', v)}
                      />
                    </Field>
                  </div>
                </div>
                <div className="border-t pt-3">
                  <p className="mb-2 text-xs font-medium text-muted-foreground">Cumuls entrants PNC</p>
                  <div className="grid grid-cols-3 gap-2">
                    <Field label="28j">
                      <NumberInput
                        value={form.cumulPncCumul28}
                        onChange={(v) => set('cumulPncCumul28', v)}
                      />
                    </Field>
                    <Field label="90j">
                      <NumberInput
                        value={form.cumulPncCumul90}
                        onChange={(v) => set('cumulPncCumul90', v)}
                      />
                    </Field>
                    <Field label="12m">
                      <NumberInput
                        value={form.cumulPncCumul12}
                        onChange={(v) => set('cumulPncCumul12', v)}
                      />
                    </Field>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Temps de service */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Temps de service</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <Field label="TS max 7 jours (h)">
                  <NumberInput value={form.tsMax7j} onChange={(v) => set('tsMax7j', v)} />
                </Field>
                <Field label="TS max 14 jours (h)">
                  <NumberInput value={form.tsMax14j} onChange={(v) => set('tsMax14j', v)} />
                </Field>
                <Field label="TS max 28 jours (h)">
                  <NumberInput value={form.tsMax28j} onChange={(v) => set('tsMax28j', v)} />
                </Field>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* ═══ Onglet Abattements ═══ */}
        <TabsContent value="abattements" className="space-y-4">
          <div className="grid gap-4 md:grid-cols-2">
            {/* Fonctions sol PNT */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Fonctions sol PNT</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {form.fonctionsSolPNT.map((f, i) => (
                  <div key={i} className="flex items-end gap-2">
                    <Field label="Fonction" className="flex-1">
                      <Input
                        value={f.nom}
                        onChange={(e) => updateFonctionSol('fonctionsSolPNT', i, 'nom', e.target.value)}
                        placeholder="Nom"
                      />
                    </Field>
                    <Field label="Pers.">
                      <NumberInput
                        value={f.nbPersonnes}
                        onChange={(v) => updateFonctionSol('fonctionsSolPNT', i, 'nbPersonnes', v)}
                        min={0}
                      />
                    </Field>
                    <Field label="Jours/mois">
                      <NumberInput
                        value={f.joursSolMois}
                        onChange={(v) => updateFonctionSol('fonctionsSolPNT', i, 'joursSolMois', v)}
                        min={0}
                      />
                    </Field>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => removeFonctionSol('fonctionsSolPNT', i)}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                ))}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => addFonctionSol('fonctionsSolPNT')}
                >
                  <Plus className="mr-1 h-3 w-3" /> Ajouter
                </Button>
              </CardContent>
            </Card>

            {/* Fonctions sol PNC */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Fonctions sol PNC</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {form.fonctionsSolPNC.map((f, i) => (
                  <div key={i} className="flex items-end gap-2">
                    <Field label="Fonction" className="flex-1">
                      <Input
                        value={f.nom}
                        onChange={(e) => updateFonctionSol('fonctionsSolPNC', i, 'nom', e.target.value)}
                        placeholder="Nom"
                      />
                    </Field>
                    <Field label="Pers.">
                      <NumberInput
                        value={f.nbPersonnes}
                        onChange={(v) => updateFonctionSol('fonctionsSolPNC', i, 'nbPersonnes', v)}
                        min={0}
                      />
                    </Field>
                    <Field label="Jours/mois">
                      <NumberInput
                        value={f.joursSolMois}
                        onChange={(v) => updateFonctionSol('fonctionsSolPNC', i, 'joursSolMois', v)}
                        min={0}
                      />
                    </Field>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => removeFonctionSol('fonctionsSolPNC', i)}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                ))}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => addFonctionSol('fonctionsSolPNC')}
                >
                  <Plus className="mr-1 h-3 w-3" /> Ajouter
                </Button>
              </CardContent>
            </Card>

            {/* Abattements PNT */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Abattements PNT</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {form.abattementsPNT.map((a, i) => (
                  <div key={i} className="flex items-end gap-2">
                    <Field label="Libelle" className="flex-1">
                      <Input
                        value={a.libelle}
                        onChange={(e) => updateAbattement('abattementsPNT', i, 'libelle', e.target.value)}
                        placeholder="Libelle"
                      />
                    </Field>
                    <Field label="Jours/pers.">
                      <NumberInput
                        value={a.joursPersonnel}
                        onChange={(v) => updateAbattement('abattementsPNT', i, 'joursPersonnel', v)}
                        min={0}
                      />
                    </Field>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => removeAbattement('abattementsPNT', i)}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                ))}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => addAbattement('abattementsPNT')}
                >
                  <Plus className="mr-1 h-3 w-3" /> Ajouter
                </Button>
              </CardContent>
            </Card>

            {/* Abattements PNC */}
            <Card>
              <CardHeader className="pb-3">
                <CardTitle className="text-sm">Abattements PNC</CardTitle>
              </CardHeader>
              <CardContent className="space-y-2">
                {form.abattementsPNC.map((a, i) => (
                  <div key={i} className="flex items-end gap-2">
                    <Field label="Libelle" className="flex-1">
                      <Input
                        value={a.libelle}
                        onChange={(e) => updateAbattement('abattementsPNC', i, 'libelle', e.target.value)}
                        placeholder="Libelle"
                      />
                    </Field>
                    <Field label="Jours/pers.">
                      <NumberInput
                        value={a.joursPersonnel}
                        onChange={(v) => updateAbattement('abattementsPNC', i, 'joursPersonnel', v)}
                        min={0}
                      />
                    </Field>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => removeAbattement('abattementsPNC', i)}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                ))}
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => addAbattement('abattementsPNC')}
                >
                  <Plus className="mr-1 h-3 w-3" /> Ajouter
                </Button>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* ═══ Onglet TSV Max ═══ */}
        <TabsContent value="tsv" className="space-y-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Table TSV Max par bande horaire</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {form.tableTsvMax.map((bande, bi) => (
                <div key={bi} className="rounded-md border p-3 space-y-2">
                  <div className="flex items-end gap-2">
                    <Field label="Debut bande">
                      <Input
                        type="time"
                        value={bande.debutBande}
                        onChange={(e) => updateTsvBande(bi, 'debutBande', e.target.value)}
                        className="font-data"
                      />
                    </Field>
                    <Field label="Fin bande">
                      <Input
                        type="time"
                        value={bande.finBande}
                        onChange={(e) => updateTsvBande(bi, 'finBande', e.target.value)}
                        className="font-data"
                      />
                    </Field>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => removeTsvBande(bi)}
                    >
                      <Trash2 className="h-4 w-4 text-destructive" />
                    </Button>
                  </div>
                  <div className="space-y-1">
                    <p className="text-xs text-muted-foreground">Max TSV par nombre d'etapes :</p>
                    {Object.entries(bande.maxParEtapes)
                      .sort(([a], [b]) => Number(a) - Number(b))
                      .map(([nbEtapes, maxTsv]) => (
                        <div key={nbEtapes} className="flex items-center gap-2">
                          <span className="w-20 text-xs font-data">{nbEtapes} etape(s)</span>
                          <Input
                            type="number"
                            value={maxTsv}
                            onChange={(e) => updateTsvEtape(bi, Number(nbEtapes), Number(e.target.value))}
                            className="w-24 font-data"
                            step={0.5}
                          />
                          <span className="text-xs text-muted-foreground">h</span>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-6 w-6"
                            onClick={() => removeTsvEtape(bi, Number(nbEtapes))}
                          >
                            <Trash2 className="h-3 w-3 text-destructive" />
                          </Button>
                        </div>
                      ))}
                    <div className="flex items-center gap-2 pt-1">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => {
                          const next = Object.keys(bande.maxParEtapes).length
                            ? Math.max(...Object.keys(bande.maxParEtapes).map(Number)) + 1
                            : 1;
                          updateTsvEtape(bi, next, 13);
                        }}
                      >
                        <Plus className="mr-1 h-3 w-3" /> Etape
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
              <Button variant="outline" size="sm" onClick={addTsvBande}>
                <Plus className="mr-1 h-3 w-3" /> Ajouter une bande
              </Button>
            </CardContent>
          </Card>
        </TabsContent>

        {/* ═══ Onglet Calendrier ═══ */}
        <TabsContent value="calendrier" className="space-y-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-sm">Affectation des semaines ISO</CardTitle>
            </CardHeader>
            <CardContent>
              {weeks.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  Definissez les dates de la periode dans l'onglet General pour voir les semaines.
                </p>
              ) : (
                <div className="space-y-2">
                  <div className="grid grid-cols-[80px_80px_1fr] gap-2 text-xs font-medium text-muted-foreground border-b pb-1">
                    <span>Semaine</span>
                    <span>Annee</span>
                    <span>Semaine type</span>
                  </div>
                  {weeks.map((w) => {
                    const current = form.calendrier.find(
                      (a) => a.semaine === w.semaine && a.annee === w.annee,
                    );
                    return (
                      <div
                        key={`${w.annee}-${w.semaine}`}
                        className="grid grid-cols-[80px_80px_1fr] items-center gap-2"
                      >
                        <span className="font-data text-sm">S{w.semaine}</span>
                        <span className="font-data text-sm">{w.annee}</span>
                        <Select
                          value={current?.semaineTypeId ?? '_none'}
                          onValueChange={(v) =>
                            updateCalendrierAffectation(
                              w.semaine,
                              w.annee,
                              v === '_none' ? '' : v,
                            )
                          }
                        >
                          <SelectTrigger className="w-full">
                            <SelectValue placeholder="Non affectee" />
                          </SelectTrigger>
                          <SelectContent>
                            <SelectItem value="_none">Non affectee</SelectItem>
                            {semaineTypes.map((st: SemaineTypeDto) => (
                              <SelectItem key={st.id} value={st.id}>
                                {st.reference} ({st.saison})
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </div>
                    );
                  })}
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
