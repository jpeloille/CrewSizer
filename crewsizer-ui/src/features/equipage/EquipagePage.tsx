import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { KpiRow } from './components/KpiRow';
import { AlertesSection } from './components/AlertesSection';
import { MembresTab } from './components/MembresTab';
import { MatriceTab } from './components/MatriceTab';
import { ChecksTab } from './components/ChecksTab';
import { ImportTab } from './components/ImportTab';
import { useEquipageKpi, useEquipageAlertes } from './hooks/useEquipageQueries';

export function EquipagePage() {
  const { data: kpi, isLoading: kpiLoading } = useEquipageKpi();
  const { data: alertes = [] } = useEquipageAlertes();

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Gestion de l&apos;equipage</h1>

      {kpiLoading ? (
        <p className="text-muted-foreground">Chargement...</p>
      ) : kpi ? (
        <KpiRow kpi={kpi} />
      ) : null}

      <AlertesSection alertes={alertes} />

      <Tabs defaultValue="membres">
        <TabsList>
          <TabsTrigger value="membres">Membres</TabsTrigger>
          <TabsTrigger value="matrice">Matrice</TabsTrigger>
          <TabsTrigger value="checks">Checks</TabsTrigger>
          <TabsTrigger value="import">Import APM</TabsTrigger>
        </TabsList>
        <TabsContent value="membres" className="mt-4">
          <MembresTab />
        </TabsContent>
        <TabsContent value="matrice" className="mt-4">
          <MatriceTab />
        </TabsContent>
        <TabsContent value="checks" className="mt-4">
          <ChecksTab />
        </TabsContent>
        <TabsContent value="import" className="mt-4">
          <ImportTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
