import { useQuery } from '@tanstack/react-query';
import { getLocationsSummary } from '../api/locationApi';

export function useLocationsSummaryQuery(folio: string) {
  return useQuery({
    queryKey: ['locations-summary', folio],
    queryFn: () => getLocationsSummary(folio),
    select: (res) => res.data,
    enabled: !!folio,
  });
}
