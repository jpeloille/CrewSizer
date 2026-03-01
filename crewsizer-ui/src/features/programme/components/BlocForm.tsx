import { useState, useEffect } from 'react';
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Layers, Plus, RefreshCw, X } from 'lucide-react';
import type { BlocVolDto, EtapeVolDto } from '@/types/bloc';
import type { BlocTypeDto } from '@/types/blocType';
import type { TypeAvionDto } from '@/types/typeAvion';
import type { VolDto } from '@/types/vol';
import type { CreateBlocPayload } from '@/api/programme';
import { DAYS, getRouteColor, hoursToHHMM, timeToMinutes, minutesToTime } from '../lib/constants';

interface BlocFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  bloc?: BlocVolDto;
  vols: VolDto[];
  blocTypes: BlocTypeDto[];
  typesAvion: TypeAvionDto[];
  onSubmit: (data: CreateBlocPayload) => void;
  isPending?: boolean;
}

interface FormState {
  code: string;
  sequence: number;
  jour: string;
  periode: string;
  debutDP: string;
  finDP: string;
  debutFDP: string;
  finFDP: string;
  blocTypeId: string;
  typeAvionId: string;
  etapes: EtapeVolDto[];
}

const defaultForm: FormState = {
  code: '',
  sequence: 1,
  jour: 'Lundi',
  periode: 'matin',
  debutDP: '06:00',
  finDP: '09:00',
  debutFDP: '06:15',
  finFDP: '08:45',
  blocTypeId: '',
  typeAvionId: '',
  etapes: [],
};

function effectiveTime(time: string, mod: number | null | undefined): string {
  if (!mod) return time;
  return minutesToTime(timeToMinutes(time) + mod);
}

function modToDisplay(minutes: number): string {
  const sign = minutes >= 0 ? '+' : '-';
  const abs = Math.abs(minutes);
  const h = Math.floor(abs / 60).toString().padStart(2, '0');
  const m = (abs % 60).toString().padStart(2, '0');
  return `${sign}${h}:${m}`;
}

function parseMod(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed || trimmed === '+00:00' || trimmed === '-00:00' || trimmed === '00:00') return null;
  const match = trimmed.match(/^([+-]?)(\d{1,2}):(\d{2})$/);
  if (!match) return null;
  const sign = match[1] === '-' ? -1 : 1;
  return sign * (parseInt(match[2]) * 60 + parseInt(match[3]));
}

function computeDpFdp(
  etapes: EtapeVolDto[],
  volMap: Map<string, VolDto>
): { debutDP: string; finDP: string; debutFDP: string; finFDP: string } | null {
  if (etapes.length === 0) return null;

  const sorted = [...etapes].sort((a, b) => a.position - b.position);
  const resolved = sorted
    .map((e) => {
      const vol = volMap.get(e.volId);
      if (!vol) return null;
      return {
        depart: vol.depart,
        arrivee: vol.arrivee,
        heureDepart: effectiveTime(vol.heureDepart, e.modificateur),
        heureArrivee: effectiveTime(vol.heureArrivee, e.modificateur),
      };
    })
    .filter(Boolean) as { depart: string; arrivee: string; heureDepart: string; heureArrivee: string }[];

  if (resolved.length === 0) return null;

  const first = resolved[0];
  const last = resolved[resolved.length - 1];

  const isNouVli = first.depart === 'NOU' && first.arrivee === 'VLI';
  const preMargin = isNouVli ? 70 : 50;

  const firstDepMin = timeToMinutes(first.heureDepart);
  const lastArrMin = timeToMinutes(last.heureArrivee);

  const debutFDP = minutesToTime(firstDepMin - preMargin);
  const finFDP = last.heureArrivee;
  const debutDP = debutFDP;
  const finDP = minutesToTime(lastArrMin + 20);

  return { debutDP, finDP, debutFDP, finFDP };
}

function blocToForm(bloc: BlocVolDto): FormState {
  return {
    code: bloc.code,
    sequence: bloc.sequence,
    jour: bloc.jour,
    periode: bloc.periode,
    debutDP: bloc.debutDP,
    finDP: bloc.finDP,
    debutFDP: bloc.debutFDP,
    finFDP: bloc.finFDP,
    blocTypeId: bloc.blocTypeId || '',
    typeAvionId: bloc.typeAvionId || '',
    etapes: [...bloc.etapes],
  };
}

export function BlocForm({
  open,
  onOpenChange,
  bloc,
  vols,
  blocTypes,
  typesAvion,
  onSubmit,
  isPending,
}: BlocFormProps) {
  const isEdit = !!bloc;
  const [form, setForm] = useState<FormState>(defaultForm);
  const [addVolId, setAddVolId] = useState('');

  useEffect(() => {
    if (open) {
      setForm(bloc ? blocToForm(bloc) : defaultForm);
      setAddVolId('');
    }
  }, [open, bloc]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  const volMap = new Map(vols.map((v) => [v.id, v]));

  const handleOpen = (isOpen: boolean) => {
    onOpenChange(isOpen);
  };

  const addEtape = () => {
    if (!addVolId) return;
    const newEtapes = [...form.etapes, { position: form.etapes.length + 1, volId: addVolId }];
    const dpFdp = computeDpFdp(newEtapes, volMap);
    setForm((prev) => ({ ...prev, etapes: newEtapes, ...(dpFdp ?? {}) }));
    setAddVolId('');
  };

  const removeEtape = (index: number) => {
    const updated = form.etapes
      .filter((_, i) => i !== index)
      .map((e, i) => ({ ...e, position: i + 1 }));
    const dpFdp = computeDpFdp(updated, volMap);
    setForm((prev) => ({ ...prev, etapes: updated, ...(dpFdp ?? {}) }));
  };

  const setModificateur = (index: number, value: string) => {
    const mod = parseMod(value);
    const updated = form.etapes.map((e, i) =>
      i === index ? { ...e, modificateur: mod } : e
    );
    setForm((prev) => ({ ...prev, etapes: updated }));
  };

  const recalcDpFdp = () => {
    if (form.etapes.length === 0) return;
    const sorted = [...form.etapes]
      .sort((a, b) => {
        const volA = volMap.get(a.volId);
        const volB = volMap.get(b.volId);
        const depA = timeToMinutes(effectiveTime(volA?.heureDepart ?? '00:00', a.modificateur));
        const depB = timeToMinutes(effectiveTime(volB?.heureDepart ?? '00:00', b.modificateur));
        return depA - depB;
      })
      .map((e, i) => ({ ...e, position: i + 1 }));
    const dpFdp = computeDpFdp(sorted, volMap);
    setForm((prev) => ({ ...prev, etapes: sorted, ...(dpFdp ?? {}) }));
  };

  const etapeTotalHdv = form.etapes.reduce((sum, e) => {
    const vol = volMap.get(e.volId);
    return sum + (vol?.hdvVol ?? 0);
  }, 0);

  const handleSubmit = () => {
    onSubmit({
      code: form.code,
      sequence: form.sequence,
      jour: form.jour,
      periode: form.periode,
      debutDP: form.debutDP,
      finDP: form.finDP,
      debutFDP: form.debutFDP,
      finFDP: form.finFDP,
      etapes: form.etapes,
      blocTypeId: form.blocTypeId || undefined,
      typeAvionId: form.typeAvionId,
    });
  };

  return (
    <Dialog open={open} onOpenChange={handleOpen}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Layers className="h-4 w-4" />
            {isEdit ? `Editer ${bloc.code}` : 'Nouveau bloc'}
          </DialogTitle>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Code</Label>
            <Input
              value={form.code}
              onChange={(e) => set('code', e.target.value.toUpperCase())}
              placeholder="BM1"
              className="font-data font-bold"
            />
          </div>

          <div className="space-y-2">
            <Label>Sequence</Label>
            <Input
              type="number"
              min={1}
              value={form.sequence}
              onChange={(e) => set('sequence', parseInt(e.target.value) || 1)}
            />
          </div>

          <div className="space-y-2">
            <Label>Jour</Label>
            <Select value={form.jour} onValueChange={(v) => set('jour', v)}>
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {DAYS.map((d) => (
                  <SelectItem key={d} value={d}>
                    {d}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Periode</Label>
            <Select
              value={form.periode}
              onValueChange={(v) => set('periode', v)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="matin">Matin</SelectItem>
                <SelectItem value="apres-midi">Apres-midi</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="col-span-2 space-y-2">
            <Label>Type de bloc</Label>
            <Select
              value={form.blocTypeId || '__none__'}
              onValueChange={(v) => set('blocTypeId', v === '__none__' ? '' : v)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Aucun type..." />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__none__">Aucun</SelectItem>
                {blocTypes.map((bt) => (
                  <SelectItem key={bt.id} value={bt.id}>
                    {bt.code} — {bt.libelle}
                    {bt.hauteSaison ? ' (HS)' : ' (BS)'}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="col-span-2 space-y-2">
            <Label>Type avion *</Label>
            <Select
              value={form.typeAvionId || '__none__'}
              onValueChange={(v) => set('typeAvionId', v === '__none__' ? '' : v)}
            >
              <SelectTrigger>
                <SelectValue placeholder="Selectionner un type avion..." />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__none__" disabled>
                  Selectionner un type avion...
                </SelectItem>
                {typesAvion.map((ta) => (
                  <SelectItem key={ta.id} value={ta.id}>
                    {ta.code} — {ta.libelle} ({ta.nbCdb + ta.nbOpl + ta.nbCc + ta.nbPnc} crew)
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>DP debut</Label>
            <Input
              type="time"
              value={form.debutDP}
              onChange={(e) => set('debutDP', e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>DP fin</Label>
            <Input
              type="time"
              value={form.finDP}
              onChange={(e) => set('finDP', e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>FDP debut</Label>
            <Input
              type="time"
              value={form.debutFDP}
              onChange={(e) => set('debutFDP', e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>FDP fin</Label>
            <Input
              type="time"
              value={form.finFDP}
              onChange={(e) => set('finFDP', e.target.value)}
            />
          </div>

          {/* Etapes */}
          <div className="col-span-2 space-y-3">
            <Label>Etapes (vols dans ce bloc)</Label>

            {form.etapes.length > 0 && (
              <div className="space-y-1">
                {form.etapes.map((etape, i) => {
                  const vol = volMap.get(etape.volId);
                  if (!vol) return null;
                  const hasMod = !!etape.modificateur;
                  const effDep = effectiveTime(vol.heureDepart, etape.modificateur);
                  const effArr = effectiveTime(vol.heureArrivee, etape.modificateur);
                  return (
                    <div
                      key={i}
                      className="flex items-center gap-2 rounded-md bg-muted px-3 py-1.5 text-sm"
                    >
                      <span className="font-data text-xs font-bold text-muted-foreground">
                        {etape.position}
                      </span>
                      <span className="font-data text-xs font-semibold text-primary">
                        {vol.numero}
                      </span>
                      {hasMod && (
                        <span className="rounded bg-orange-500/15 px-1.5 py-0.5 font-data text-[9px] font-bold text-orange-500">
                          MH
                        </span>
                      )}
                      <span
                        className="text-xs"
                        style={{
                          color: getRouteColor(vol.depart, vol.arrivee),
                        }}
                      >
                        {vol.depart} → {vol.arrivee}
                      </span>
                      <span className="font-data text-xs text-muted-foreground">
                        {hasMod ? (
                          <>
                            <span className="line-through opacity-50">{vol.heureDepart}–{vol.heureArrivee}</span>
                            {' '}
                            <span className="font-semibold text-orange-500">{effDep}–{effArr}</span>
                          </>
                        ) : (
                          <>{vol.heureDepart}–{vol.heureArrivee}</>
                        )}
                      </span>
                      <span className="flex-1" />
                      <Input
                        className="h-6 w-[76px] px-1 font-data text-xs text-center"
                        placeholder="+00:00"
                        defaultValue={etape.modificateur ? modToDisplay(etape.modificateur) : ''}
                        onBlur={(e) => setModificateur(i, e.target.value)}
                      />
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-5 w-5"
                        onClick={() => removeEtape(i)}
                      >
                        <X className="h-3 w-3" />
                      </Button>
                    </div>
                  );
                })}
              </div>
            )}

            <div className="flex items-end gap-2">
              <div className="flex-1">
                <Select value={addVolId} onValueChange={setAddVolId}>
                  <SelectTrigger>
                    <SelectValue placeholder="Selectionner un vol..." />
                  </SelectTrigger>
                  <SelectContent>
                    {vols.map((v) => (
                      <SelectItem key={v.id} value={v.id}>
                        {v.numero} ({v.depart} → {v.arrivee})
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={addEtape}
                disabled={!addVolId}
              >
                <Plus className="mr-1 h-3 w-3" />
                Ajouter
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={recalcDpFdp}
                disabled={form.etapes.length === 0}
                title="Recalculer DP/FDP"
              >
                <RefreshCw className="mr-1 h-3 w-3" />
                DP/FDP
              </Button>
            </div>

            {form.etapes.length > 0 && (
              <div className="rounded-md bg-muted/50 px-3 py-2 font-data text-xs text-muted-foreground">
                {form.etapes.length} etape(s) · Temps de bloc total :{' '}
                {hoursToHHMM(etapeTotalHdv)}
              </div>
            )}
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Annuler
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={
              !form.code || !form.typeAvionId || form.etapes.length === 0 || isPending
            }
          >
            {isPending
              ? 'Enregistrement...'
              : isEdit
                ? 'Enregistrer'
                : 'Creer'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
