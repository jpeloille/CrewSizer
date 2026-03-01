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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Plane } from 'lucide-react';
import type { VolDto } from '@/types/vol';
import type { CreateVolPayload } from '@/api/programme';
import { AIRPORTS, AP_KEYS } from '../lib/constants';

interface VolFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  vol?: VolDto;
  onSubmit: (data: CreateVolPayload) => void;
  isPending?: boolean;
}

function volToPayload(vol: VolDto): CreateVolPayload {
  return {
    numero: vol.numero,
    depart: vol.depart,
    arrivee: vol.arrivee,
    heureDepart: vol.heureDepart,
    heureArrivee: vol.heureArrivee,
    mh: vol.mh,
  };
}

const defaultForm: CreateVolPayload = {
  numero: '',
  depart: 'NOU',
  arrivee: 'LIF',
  heureDepart: '07:00',
  heureArrivee: '07:40',
  mh: false,
};

export function VolForm({
  open,
  onOpenChange,
  vol,
  onSubmit,
  isPending,
}: VolFormProps) {
  const isEdit = !!vol;
  const [form, setForm] = useState<CreateVolPayload>(
    vol ? volToPayload(vol) : defaultForm
  );

  useEffect(() => {
    if (vol) setForm(volToPayload(vol));
    else setForm(defaultForm);
  }, [vol]);

  const set = <K extends keyof CreateVolPayload>(
    key: K,
    value: CreateVolPayload[K]
  ) => setForm((prev) => ({ ...prev, [key]: value }));

  const handleOpen = (isOpen: boolean) => {
    if (isOpen && vol) setForm(volToPayload(vol));
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
            <Plane className="h-4 w-4" />
            {isEdit ? `Editer ${vol.numero}` : 'Nouveau vol'}
          </DialogTitle>
        </DialogHeader>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-2">
            <Label>N° Vol</Label>
            <Input
              value={form.numero}
              onChange={(e) => set('numero', e.target.value)}
              placeholder="TPC 201"
              className="font-data font-bold"
            />
          </div>
          <div />

          <div className="space-y-2">
            <Label>Depart</Label>
            <Select
              value={form.depart}
              onValueChange={(v) => set('depart', v)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {AP_KEYS.map((a) => (
                  <SelectItem key={a} value={a}>
                    {a} — {AIRPORTS[a].city}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>Arrivee</Label>
            <Select
              value={form.arrivee}
              onValueChange={(v) => set('arrivee', v)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {AP_KEYS.map((a) => (
                  <SelectItem key={a} value={a}>
                    {a} — {AIRPORTS[a].city}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-2">
            <Label>STD (heure bloc depart)</Label>
            <Input
              type="time"
              value={form.heureDepart}
              onChange={(e) => set('heureDepart', e.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label>STA (heure bloc arrivee)</Label>
            <Input
              type="time"
              value={form.heureArrivee}
              onChange={(e) => set('heureArrivee', e.target.value)}
            />
          </div>

          <div className="col-span-2 flex items-center gap-2 pt-2">
            <Checkbox
              id="mh"
              checked={form.mh}
              onCheckedChange={(checked) => set('mh', checked === true)}
            />
            <Label htmlFor="mh" className="cursor-pointer">
              Modification Horaire (MH)
            </Label>
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Annuler
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!form.numero || isPending}
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
