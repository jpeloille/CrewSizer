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
import { Checkbox } from '@/components/ui/checkbox';
import { Layers } from 'lucide-react';
import type { BlocTypeDto } from '@/types/blocType';
import type { CreateBlocTypePayload } from '@/api/programme';

interface BlocTypeFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  blocType?: BlocTypeDto;
  onSubmit: (data: CreateBlocTypePayload) => void;
  isPending?: boolean;
}

interface FormState {
  code: string;
  libelle: string;
  debutPlage: string;
  finPlage: string;
  fdpMax: number;
  hauteSaison: boolean;
}

const defaultForm: FormState = {
  code: '',
  libelle: '',
  debutPlage: '06:00',
  finPlage: '13:29',
  fdpMax: 13,
  hauteSaison: false,
};

function dtoToForm(bt: BlocTypeDto): FormState {
  return {
    code: bt.code,
    libelle: bt.libelle,
    debutPlage: bt.debutPlage,
    finPlage: bt.finPlage,
    fdpMax: bt.fdpMax,
    hauteSaison: bt.hauteSaison,
  };
}

export function BlocTypeForm({
  open,
  onOpenChange,
  blocType,
  onSubmit,
  isPending,
}: BlocTypeFormProps) {
  const isEdit = !!blocType;
  const [form, setForm] = useState<FormState>(defaultForm);

  useEffect(() => {
    if (open) {
      setForm(blocType ? dtoToForm(blocType) : defaultForm);
    }
  }, [open, blocType]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  const handleOpen = (isOpen: boolean) => {
    if (isOpen && blocType) setForm(dtoToForm(blocType));
    else if (isOpen) setForm(defaultForm);
    onOpenChange(isOpen);
  };

  const handleSubmit = () => {
    onSubmit(form);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpen}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Layers className="h-4 w-4" />
            {isEdit ? `Editer ${blocType.code}` : 'Nouveau type de bloc'}
          </DialogTitle>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Code</Label>
            <Input
              value={form.code}
              onChange={(e) => set('code', e.target.value.toUpperCase())}
              placeholder="MATIN_STD"
              className="font-data font-bold"
            />
          </div>

          <div className="space-y-2">
            <Label>Libelle</Label>
            <Input
              value={form.libelle}
              onChange={(e) => set('libelle', e.target.value)}
              placeholder="Matin standard"
            />
          </div>

          <div className="space-y-2">
            <Label>Debut plage</Label>
            <Input
              type="time"
              value={form.debutPlage}
              onChange={(e) => set('debutPlage', e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>Fin plage</Label>
            <Input
              type="time"
              value={form.finPlage}
              onChange={(e) => set('finPlage', e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>FDP max (heures)</Label>
            <Input
              type="number"
              min={0}
              step={0.5}
              value={form.fdpMax}
              onChange={(e) => set('fdpMax', parseFloat(e.target.value) || 0)}
            />
          </div>
          <div />

          <div className="col-span-2 flex items-center gap-2 pt-2">
            <Checkbox
              id="hauteSaison"
              checked={form.hauteSaison}
              onCheckedChange={(checked) =>
                set('hauteSaison', checked === true)
              }
            />
            <Label htmlFor="hauteSaison" className="cursor-pointer">
              Haute saison
            </Label>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Annuler
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!form.code || !form.libelle || isPending}
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
