import { useQuery } from '@tanstack/react-query';
import { getZipCode } from '../api/zipCodeApi';

export function useZipCodeQuery(cp: string) {
  return useQuery({
    queryKey: ['zip-code', cp],
    queryFn: () => getZipCode(cp),
    select: (res) => res.data,
    enabled: /^\d{5}$/.test(cp),
    staleTime: 1000 * 60 * 30,
    retry: false,
  });
}
