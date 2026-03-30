import { useQuery } from '@tanstack/react-query';
import { getLocations } from '../api/locationApi';

export function useLocationsQuery(folio: string) {
  return useQuery({
    queryKey: ['locations', folio],
    queryFn: () => getLocations(folio),
    select: (res) => res.data,
    enabled: !!folio,
    staleTime: 0,
  });
}
