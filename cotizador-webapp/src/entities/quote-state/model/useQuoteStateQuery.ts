import { useQuery } from '@tanstack/react-query';
import { getQuoteState } from '../api/quoteStateApi';

export function useQuoteStateQuery(folio: string) {
  return useQuery({
    queryKey: ['quote-state', folio],
    queryFn: () => getQuoteState(folio),
    staleTime: 0,
    enabled: !!folio,
  });
}
