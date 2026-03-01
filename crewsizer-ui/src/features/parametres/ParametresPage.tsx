import { useLocation, useNavigate } from 'react-router';
import { Tabs, TabsList, TabsTrigger, TabsContent } from '@/components/ui/tabs';
import { TypesAvionPage } from '@/features/types-avion/TypesAvionPage';
import { BlocTypesPage } from '@/features/bloc-types/BlocTypesPage';
import { RolesTab } from './components/RolesTab';
import { SolverTab } from './components/SolverTab';
import { Settings, PlaneTakeoff, Layers, Shield, Cpu } from 'lucide-react';

const TABS = ['types-avion', 'bloc-types', 'roles', 'solver'] as const;
type TabKey = (typeof TABS)[number];

function resolveTab(pathname: string): TabKey {
  const segment = pathname.split('/').pop();
  if (segment && TABS.includes(segment as TabKey)) return segment as TabKey;
  return 'types-avion';
}

export function ParametresPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = resolveTab(location.pathname);

  const handleTabChange = (tab: string) => {
    navigate(`/parametres/${tab}`, { replace: true });
  };

  return (
    <div className="space-y-5">
      <div className="flex items-center gap-2">
        <Settings className="h-5 w-5 text-muted-foreground" />
        <h1 className="text-2xl font-bold">Parametres</h1>
      </div>

      <Tabs value={activeTab} onValueChange={handleTabChange}>
        <TabsList>
          <TabsTrigger value="types-avion">
            <PlaneTakeoff className="mr-1.5 h-4 w-4" />
            Types avion
          </TabsTrigger>
          <TabsTrigger value="bloc-types">
            <Layers className="mr-1.5 h-4 w-4" />
            Types de blocs
          </TabsTrigger>
          <TabsTrigger value="roles">
            <Shield className="mr-1.5 h-4 w-4" />
            Roles
          </TabsTrigger>
          <TabsTrigger value="solver">
            <Cpu className="mr-1.5 h-4 w-4" />
            Solver
          </TabsTrigger>
        </TabsList>
        <TabsContent value="types-avion" className="mt-4">
          <TypesAvionPage />
        </TabsContent>
        <TabsContent value="bloc-types" className="mt-4">
          <BlocTypesPage />
        </TabsContent>
        <TabsContent value="roles" className="mt-4">
          <RolesTab />
        </TabsContent>
        <TabsContent value="solver" className="mt-4">
          <SolverTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
