import { useState, useRef, useCallback } from 'react';
import { Button } from '@/components/ui/button';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Upload, FileUp, X } from 'lucide-react';
import { useImportEquipage } from '../hooks/useEquipageQueries';
import type { ImportEquipageResultDto } from '@/types/equipage';
import { formatDateFr } from '../lib/statut-helpers';

type FileSlot = 'pnt' | 'pnc' | 'checkStatus' | 'checkDesc';

const slotLabels: Record<FileSlot, string> = {
  pnt: 'PNT Crew List',
  pnc: 'PNC Crew List',
  checkStatus: 'Check Status',
  checkDesc: 'Check Description',
};

export function ImportTab() {
  const [files, setFiles] = useState<Record<FileSlot, File | null>>({
    pnt: null,
    pnc: null,
    checkStatus: null,
    checkDesc: null,
  });
  const [dragOver, setDragOver] = useState(false);
  const [result, setResult] = useState<ImportEquipageResultDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const dropRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const importMutation = useImportEquipage();

  const hasFiles = Object.values(files).some(Boolean);
  const fileCount = Object.values(files).filter(Boolean).length;

  const assignFile = useCallback((file: File) => {
    const name = file.name.toLowerCase();
    setFiles((prev) => {
      if (name.includes('pntcrewlist') || name.includes('pnt')) return { ...prev, pnt: file };
      if (name.includes('pnccrewlist') || name.includes('pnc')) return { ...prev, pnc: file };
      if (name.includes('checkstatus') || name.includes('crewcheckstatus')) return { ...prev, checkStatus: file };
      if (name.includes('checkdesc') || name.includes('check description')) return { ...prev, checkDesc: file };
      // Fallback: fill first empty slot
      for (const slot of ['pnt', 'pnc', 'checkStatus', 'checkDesc'] as FileSlot[]) {
        if (!prev[slot]) return { ...prev, [slot]: file };
      }
      return prev;
    });
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const droppedFiles = Array.from(e.dataTransfer.files).filter((f) => f.name.endsWith('.xml'));
    droppedFiles.forEach(assignFile);
  }, [assignFile]);

  const handleFileInput = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = Array.from(e.target.files ?? []);
    selected.forEach(assignFile);
    if (inputRef.current) inputRef.current.value = '';
  }, [assignFile]);

  const removeFile = (slot: FileSlot) => {
    setFiles((prev) => ({ ...prev, [slot]: null }));
  };

  const handleImport = async () => {
    setResult(null);
    setError(null);
    const formData = new FormData();
    if (files.pnt) formData.append('pnt', files.pnt);
    if (files.pnc) formData.append('pnc', files.pnc);
    if (files.checkStatus) formData.append('checkStatus', files.checkStatus);
    if (files.checkDesc) formData.append('checkDesc', files.checkDesc);
    try {
      const res = await importMutation.mutateAsync(formData);
      setResult(res);
    } catch {
      setError("Erreur lors de l'import. Verifiez les fichiers et reessayez.");
    }
  };

  const resetForm = () => {
    setFiles({ pnt: null, pnc: null, checkStatus: null, checkDesc: null });
    setResult(null);
    setError(null);
  };

  return (
    <div className="max-w-2xl space-y-6">
      <p className="text-sm text-muted-foreground">
        Importez les fichiers XML exportes depuis APM. L'import remplace toutes les donnees existantes.
      </p>

      {/* Drop zone */}
      <div
        ref={dropRef}
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
        onClick={() => inputRef.current?.click()}
        className={`flex cursor-pointer flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed p-8 transition-colors ${
          dragOver
            ? 'border-primary bg-primary/5'
            : 'border-border hover:border-primary/50 hover:bg-muted/30'
        }`}
      >
        <div className={`flex h-12 w-12 items-center justify-center rounded-full ${
          dragOver ? 'bg-primary/20 text-primary' : 'bg-muted text-muted-foreground'
        }`}>
          <FileUp className="h-6 w-6" />
        </div>
        <div className="text-center">
          <p className="text-sm font-medium">
            {dragOver ? 'Deposez les fichiers ici' : 'Glissez-deposez vos fichiers XML ici'}
          </p>
          <p className="text-xs text-muted-foreground">ou cliquez pour parcourir</p>
        </div>
        <input
          ref={inputRef}
          type="file"
          accept=".xml"
          multiple
          className="hidden"
          onChange={handleFileInput}
        />
      </div>

      {/* File slots */}
      {hasFiles && (
        <div className="grid grid-cols-2 gap-2">
          {(Object.entries(slotLabels) as [FileSlot, string][]).map(([slot, label]) => (
            <div
              key={slot}
              className={`flex items-center justify-between rounded-md border px-3 py-2 text-sm ${
                files[slot] ? 'border-primary/30 bg-primary/5' : 'border-border'
              }`}
            >
              <div className="min-w-0">
                <p className="text-xs text-muted-foreground">{label}</p>
                <p className="truncate font-data text-xs">
                  {files[slot]?.name ?? <span className="italic text-muted-foreground">—</span>}
                </p>
              </div>
              {files[slot] && (
                <button
                  onClick={(e) => { e.stopPropagation(); removeFile(slot); }}
                  className="ml-2 shrink-0 text-muted-foreground hover:text-foreground"
                >
                  <X className="h-3.5 w-3.5" />
                </button>
              )}
            </div>
          ))}
        </div>
      )}

      <div className="flex gap-2">
        <Button
          onClick={handleImport}
          disabled={!hasFiles || importMutation.isPending}
        >
          <Upload className="mr-2 h-4 w-4" />
          {importMutation.isPending ? 'Import en cours...' : `Importer (${fileCount} fichier${fileCount > 1 ? 's' : ''})`}
        </Button>
        {(result || error || hasFiles) && (
          <Button variant="outline" onClick={resetForm}>
            Reinitialiser
          </Button>
        )}
      </div>

      {result && (
        <Alert className="border-emerald-500/30 bg-emerald-500/5">
          <AlertDescription>
            <p className="font-semibold text-emerald-400">Import reussi</p>
            <p className="font-data text-sm">
              {result.nbMembresImportes} membres importes, {result.nbChecksImportes} checks
            </p>
            <p className="text-sm">Date extraction : {formatDateFr(result.dateExtraction)}</p>
            {result.avertissements.length > 0 && (
              <ul className="mt-2 list-disc pl-4 text-sm text-amber-400">
                {result.avertissements.map((a, i) => (
                  <li key={i}>{a}</li>
                ))}
              </ul>
            )}
          </AlertDescription>
        </Alert>
      )}

      {error && (
        <Alert variant="destructive">
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}
    </div>
  );
}
