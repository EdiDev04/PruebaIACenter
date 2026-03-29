import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useDispatch } from 'react-redux';
import { initWizard, setFolioVersion } from '@/entities/folio';
import { getQuote } from '../api/quoteApi';

const STEP_ROUTES: Record<number, string> = {
  0: 'general-info',
  1: 'general-info',
  2: 'locations',
  3: 'coverages',
  4: 'results',
};

function isAppError(err: unknown): err is { type?: string; message?: string } {
  return typeof err === 'object' && err !== null;
}

export function useFolioSearch() {
  const [isLoading, setIsLoading] = useState(false);
  const navigate = useNavigate();
  const dispatch = useDispatch();

  const searchFolio = async (
    folioNumber: string,
    onError: (msg: string) => void
  ) => {
    setIsLoading(true);
    try {
      const res = await getQuote(folioNumber);
      const { metadata, version } = res.data;
      const step = metadata.lastWizardStep ?? 0;
      dispatch(initWizard({ activeFolio: folioNumber, currentStep: step }));
      dispatch(setFolioVersion(version));
      const routeSegment = STEP_ROUTES[step] ?? 'general-info';
      navigate(`/quotes/${folioNumber}/${routeSegment}`);
    } catch (err: unknown) {
      if (isAppError(err) && err.type === 'folioNotFound') {
        onError(`El folio ${folioNumber} no existe`);
      } else {
        onError('Error al buscar el folio. Intente nuevamente.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return { searchFolio, isLoading };
}
