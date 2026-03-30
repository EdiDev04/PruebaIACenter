import { useQuery } from '@tanstack/react-query';
import { getCoverageOptions } from '../api/coverageOptionsApi';

export function useCoverageOptionsQuery(folio: string) {
  return useQuery({
    queryKey: ['coverage-options', folio] as const,
    queryFn: () => getCoverageOptions(folio),
    select: (res) => res.data,
    enabled: !!folio,
  });
}
