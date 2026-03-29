import { createBrowserRouter, Navigate } from 'react-router-dom';
import { FolioHomePage } from '@/pages/FolioHomePage';
import { FolioCreatedPage } from '@/pages/FolioCreatedPage';
import { GeneralInfoPage } from '@/pages/GeneralInfoPage';
import { PlaceholderPage } from '@/pages/PlaceholderPage';
import { WizardLayout } from '@/widgets/WizardLayout';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <FolioHomePage />,
  },
  {
    path: '/quotes/:folioNumber/created',
    element: <FolioCreatedPage />,
  },
  {
    path: '/quotes/:folioNumber',
    element: <WizardLayout />,
    children: [
      { index: true, element: <Navigate to="general-info" replace /> },
      { path: 'general-info', element: <GeneralInfoPage /> },
      { path: 'locations', element: <PlaceholderPage label="Ubicaciones" stepNumber={2} /> },
      { path: 'coverages', element: <PlaceholderPage label="Coberturas" stepNumber={3} /> },
      { path: 'results', element: <PlaceholderPage label="Resultados" stepNumber={4} /> },
    ],
  },
]);
