import { useQuery } from '@tanstack/react-query';
import { getLayout } from '../api/layoutApi';
import type { LayoutConfigurationDto } from './types';

interface UseLayoutQueryOptions {
  onInvalidFolio?: () => void;
  onFolioNotFound?: () => void;
}

interface ApiError {
  type: string;
  message: string;
  field: string | null;
}

export function useLayoutQuery(
  folio: string,
  options: UseLayoutQueryOptions = {}
) {
  return useQuery({
    queryKey: ['layout', folio],
    queryFn: async () => {
      try {
        const res = await getLayout(folio);
        return res.data as LayoutConfigurationDto;
      } catch (err) {
        const apiErr = err as ApiError;
        if (apiErr?.type === 'validationError') {
          options.onInvalidFolio?.();
        }
        if (apiErr?.type === 'folioNotFound') {
          options.onFolioNotFound?.();
        }
        throw err;
      }
    },
    enabled: !!folio,
    retry: (failureCount, error) => {
      const apiErr = error as unknown as ApiError;
      if (
        apiErr?.type === 'validationError' ||
        apiErr?.type === 'folioNotFound'
      ) {
        return false;
      }
      return failureCount < 1;
    },
  });
}
