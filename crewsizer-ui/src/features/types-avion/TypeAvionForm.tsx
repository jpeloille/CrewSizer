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
import { PlaneTakeoff } from 'lucide-react';
import type { TypeAvionDto } from '@/types/typeAvion';
import type { CreateTypeAvionPayload } from '@/api/programme';

interface TypeAvionFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  typeAvion?: TypeAvionDto;
  onSubmit: (data: CreateTypeAvionPayload) => void;
  isPending?: boolean;
}

interface FormState {
  code: string;
  libelle: string;
  nbCdb: number;
  nbOpl: number;
  nbCc: number;
  nbPnc: number;
}

const defaultForm: FormState = {
  code: '',
  libelle: '',
  nbCdb: 1,
  nbOpl: 1,
  nbCc: 1,
  nbPnc: 0,
};

function dtoToForm(ta: TypeAvionDto): FormState {
  return {
    code: ta.code,
    libelle: ta.libelle,
    nbCdb: ta.nbCdb,
    nbOpl: ta.nbOpl,
    nbCc: ta.nbCc,
    nbPnc: ta.nbPnc,
  };
}

export function TypeAvionForm({
  open,
  onOpenChange,
  typeAvion,
  onSubmit,
  isPending,
}: TypeAvionFormProps) {
  const isEdit = !!typeAvion;
  const [form, setForm] = useState<FormState>(defaultForm);

  useEffect(() => {
    if (open) {
      setForm(typeAvion ? dtoToForm(typeAvion) : defaultForm);
    }
  }, [open, typeAvion]);

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((prev) => ({ ...prev, [key]: value }));

  const handleOpen = (isOpen: boolean) => {
    if (isOpen && typeAvion) setForm(dtoToForm(typeAvion));
    else if (isOpen) setForm(defaultForm);
    onOpenChange(isOpen);
  };

  const handleSubmit = () => {
    onSubmit(form);
  };

  const total = form.nbCdb + form.nbOpl + form.nbCc + form.nbPnc;

  return (
    <Dialog open={open} onOpenChange={handleOpen}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <PlaneTakeoff className="h-4 w-4" />
            {isEdit ? `Editer ${typeAvion.code}` : 'Nouveau type avion'}
          </DialogTitle>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>Code</Label>
            <Input
              value={form.code}
              onChange={(e) => set('code', e.target.value.toUpperCase())}
              placeholder="ATR72"
              className="font-data font-bold"
            />
          </div>

          <div className="space-y-2">
            <Label>Libelle</Label>
            <Input
              value={form.libelle}
              onChange={(e) => set('libelle', e.target.value)}
              placeholder="ATR 72-600"
            />
          </div>

          <div className="col-span-2">
            <p className="mb-3 text-xs font-medium text-muted-foreground">
              Composition equipage
            </p>
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label>CDB (Commandants)</Label>
                <Input
                  type="number"
                  min={0}
                  step={1}
                  value={form.nbCdb}
                  onChange={(e) =>
                    set('nbCdb', parseInt(e.target.value) || 0)
                  }
                />
              </div>

              <div className="space-y-2">
                <Label>OPL (Copilotes)</Label>
                <Input
                  type="number"
                  min={0}
                  step={1}
                  value={form.nbOpl}
                  onChange={(e) =>
                    set('nbOpl', parseInt(e.target.value) || 0)
                  }
                />
              </div>

              <div className="space-y-2">
                <Label>CC (Chefs cabine)</Label>
                <Input
                  type="number"
                  min={0}
                  step={1}
                  value={form.nbCc}
                  onChange={(e) =>
                    set('nbCc', parseInt(e.target.value) || 0)
                  }
                />
              </div>

              <div className="space-y-2">
                <Label>PNC (Cabine)</Label>
                <Input
                  type="number"
                  min={0}
                  step={1}
                  value={form.nbPnc}
                  onChange={(e) =>
                    set('nbPnc', parseInt(e.target.value) || 0)
                  }
                />
              </div>
            </div>
          </div>

          <div className="col-span-2 rounded-md bg-muted/50 px-3 py-2 font-data text-xs text-muted-foreground">
            Total : {total} membre{total !== 1 ? 's' : ''} d'equipage
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
