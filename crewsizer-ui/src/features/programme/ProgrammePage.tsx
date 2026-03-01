import { useLocation, useNavigate } from 'react-router';
import { PipelineNav } from './components/PipelineNav';
import { VolsTab } from './components/VolsTab';
import { BlocsTab } from './components/BlocsTab';
import { SemainesTab } from './components/SemainesTab';
import { CalendrierTab } from './components/CalendrierTab';
import { useVols, useBlocs, useSemaines } from './hooks/useProgrammeQueries';

const TABS = ['vols', 'blocs', 'semaines', 'calendrier'] as const;
type TabKey = (typeof TABS)[number];

function resolveTab(pathname: string): TabKey {
  const segment = pathname.split('/').pop();
  if (segment && TABS.includes(segment as TabKey)) return segment as TabKey;
  return 'vols';
}

export function ProgrammePage() {
  const location = useLocation();
  const navigate = useNavigate();
  const activeTab = resolveTab(location.pathname);

  const { data: vols = [] } = useVols();
  const { data: blocs = [] } = useBlocs();
  const { data: semaines = [] } = useSemaines();

  const handleTabChange = (tab: string) => {
    navigate(`/programme/${tab}`, { replace: true });
  };

  return (
    <div className="space-y-5">
      <PipelineNav
        activeTab={activeTab}
        onTabChange={handleTabChange}
        volsCount={vols.length}
        blocsCount={blocs.length}
        semainesCount={semaines.length}
        calendarWeeks={52}
      />

      {activeTab === 'vols' && <VolsTab />}
      {activeTab === 'blocs' && <BlocsTab />}
      {activeTab === 'semaines' && <SemainesTab />}
      {activeTab === 'calendrier' && <CalendrierTab />}
    </div>
  );
}
