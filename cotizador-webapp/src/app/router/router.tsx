import { createBrowserRouter, Navigate } from 'react-router-dom';
import { FolioHomePage } from '@/pages/FolioHomePage';
import { FolioCreatedPage } from '@/pages/FolioCreatedPage';
import { GeneralInfoPage } from '@/pages/GeneralInfoPage';
import { LocationsPage } from '@/pages/LocationsPage';
import { TechnicalInfoPage } from '@/pages/TechnicalInfoPage';
import { ResultsPage } from '@/pages/ResultsPage';
import { WizardLayout } from '../WizardLayout';

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
      { path: 'locations', element: <LocationsPage /> },
      { path: 'technical-info', element: <TechnicalInfoPage /> },
      { path: 'results', element: <ResultsPage /> },
    ],
  },
]);
