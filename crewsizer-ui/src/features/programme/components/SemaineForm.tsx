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
import { Grid3X3, X } from 'lucide-react';
import type { SemaineTypeDto, BlocPlacementDto } from '@/types/semaine';
import type { BlocVolDto } from '@/types/bloc';
import type { CreateSemainePayload } from '@/api/programme';
import { DAYS, SEASONS } from '../lib/constants';
import { cn } from '@/lib/utils';

interface SemaineFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  semaine?: SemaineTypeDto;
  blocs: BlocVolDto[];
  onSubmit: (data: CreateSemainePayload) => void;
  isPending?: boolean;
}

interface FormState {
  reference: string;
  saison: string;
  placements: BlocPlacementDto[];
}

const defaultForm: FormState = {
  reference: '',
  saison: 'BASSE',
  placements: [],
};

function semaineToForm(s: SemaineTypeDto): FormState {
  return {
    reference: s.reference,
    saison: s.saison,
    placements: [...s.placements],
  };
}

export function SemaineForm({
  open,
  onOpenChange,
  semaine,
  blocs,
  onSubmit,
  isPending,
}: SemaineFormProps) {
  const isEdit = !!semaine;
  const [form, setForm] = useState<FormState>(
    semaine ? semaineToForm(semaine) : defaultForm
  );

  useEffect(() => {
    if (open) {
      setForm(semaine ? semaineToForm(semaine) : defaultForm);
    }
  }, [open, semaine]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  const handleOpen = (isOpen: boolean) => {
    if (isOpen && semaine) setForm(semaineToForm(semaine));
    else if (isOpen) setForm(defaultForm);
    onOpenChange(isOpen);
  };

  const addPlacement = (blocId: string, day: string) => {
    const seq =
      form.placements.filter((p) => p.jour === day).length + 1;
    set('placements', [
      ...form.placements,
      { blocId, jour: day, sequence: seq },
    ]);
  };

  const removePlacement = (index: number) => {
    const updated = form.placements.filter((_, i) => i !== index);
    set('placements', updated);
  };

  const blocMap = new Map(blocs.map((b) => [b.id, b]));

  const handleSubmit = () => {
    onSubmit({
      reference: form.reference,
      saison: form.saison,
      placements: form.placements,
    });
  };

  return (
    <Dialog open={open} onOpenChange={handleOpen}>
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Grid3X3 className="h-4 w-4" />
            {isEdit ? `Editer ${semaine.reference}` : 'Nouvelle semaine type'}
          </DialogTitle>
        </DialogHeader>
        <div className="space-y-4">
          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label>Reference</Label>
              <Input
                value={form.reference}
                onChange={(e) =>
                  set('reference', e.target.value.toUpperCase())
                }
                placeholder="BS_01"
                className="font-data font-bold"
              />
            </div>
            <div className="space-y-2">
              <Label>Saison</Label>
              <Select
                value={form.saison}
                onValueChange={(v) => set('saison', v)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {SEASONS.map((s) => (
                    <SelectItem key={s.value} value={s.value}>
                      {s.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          {/* Week grid */}
          <div>
            <Label className="mb-2 block">Composition de la semaine</Label>
            <div className="grid grid-cols-7 gap-1.5">
              {DAYS.map((day) => {
                const dayPlacements = form.placements
                  .map((p, i) => ({ ...p, _index: i }))
                  .filter((p) => p.jour === day)
                  .sort((a, b) => a.sequence - b.sequence);

                return (
                  <div
                    key={day}
                    className="min-h-[80px] rounded-lg border border-border bg-card p-2"
                  >
                    <div className="mb-1.5 font-data text-[10px] font-bold uppercase tracking-wider text-muted-foreground">
                      {day.slice(0, 3)}
                    </div>

                    {dayPlacements.length === 0 && (
                      <div className="text-[10px] italic text-muted-foreground/50">
                        —
                      </div>
                    )}

                    {dayPlacements.map((dp) => {
                      const blk = blocMap.get(dp.blocId);
                      if (!blk) return null;
                      const isMatin = blk.periode === 'matin';
                      return (
                        <div
                          key={dp._index}
                          className={cn(
                            'mb-1 flex items-center justify-between rounded px-1.5 py-0.5 font-data text-[11px] font-medium',
                            'border-l-2',
                            isMatin
                              ? 'border-l-amber-500 bg-amber-500/5'
                              : 'border-l-purple-500 bg-purple-500/5'
                          )}
                        >
                          <span>{blk.code}</span>
                          <button
                            className="text-muted-foreground hover:text-foreground"
                            onClick={() => removePlacement(dp._index)}
                          >
                            <X className="h-3 w-3" />
                          </button>
                        </div>
                      );
                    })}

                    <select
                      className="mt-1 w-full rounded border border-border bg-background px-1 py-0.5 font-data text-[10px] text-muted-foreground"
                      value=""
                      onChange={(e) => {
                        if (e.target.value)
                          addPlacement(e.target.value, day);
                        e.target.value = '';
                      }}
                    >
                      <option value="">+ Bloc...</option>
                      {blocs.map((b) => (
                        <option key={b.id} value={b.id}>
                          {b.code} — {b.nom}
                        </option>
                      ))}
                    </select>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Annuler
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={
              !form.reference || form.placements.length === 0 || isPending
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
