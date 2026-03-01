import { createBrowserRouter, Navigate } from 'react-router';
import { AppLayout } from '@/components/layout/AppLayout';
import { AuthLayout } from '@/components/layout/AuthLayout';
import { AuthGuard } from '@/features/auth/AuthGuard';
import { LoginPage } from '@/features/auth/LoginPage';
import { DashboardPage } from '@/features/dashboard/DashboardPage';
import { ScenarioListPage } from '@/features/scenarios/ScenarioListPage';
import { ScenarioEditPage } from '@/features/scenarios/ScenarioEditPage';
import { EquipagePage } from '@/features/equipage/EquipagePage';
import { ProgrammePage } from '@/features/programme/ProgrammePage';
import { ResultatsPage } from '@/features/resultats/ResultatsPage';
import { ParametresPage } from '@/features/parametres/ParametresPage';

export const router = createBrowserRouter([
  {
    path: '/login',
    element: (
      <AuthLayout>
        <LoginPage />
      </AuthLayout>
    ),
  },
  {
    path: '/',
    element: (
      <AuthGuard>
        <AppLayout />
      </AuthGuard>
    ),
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'scenarios', element: <ScenarioListPage /> },
      { path: 'scenarios/:id', element: <ScenarioEditPage /> },
      { path: 'equipage', element: <EquipagePage /> },
      { path: 'resultats', element: <ResultatsPage /> },
      {
        path: 'programme',
        children: [
          { index: true, element: <Navigate to="vols" replace /> },
          { path: '*', element: <ProgrammePage /> },
        ],
      },
      {
        path: 'parametres',
        children: [
          { index: true, element: <Navigate to="types-avion" replace /> },
          { path: '*', element: <ParametresPage /> },
        ],
      },
      // Redirects de compatibilite anciennes URLs
      { path: 'bloc-types', element: <Navigate to="/parametres/bloc-types" replace /> },
      { path: 'types-avion', element: <Navigate to="/parametres/types-avion" replace /> },
    ],
  },
]);
