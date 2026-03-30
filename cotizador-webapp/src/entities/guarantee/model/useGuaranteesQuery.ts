import { useQuery } from '@tanstack/react-query';
import { getGuarantees } from '../api/guaranteeApi';

const THIRTY_MINUTES = 30 * 60 * 1000;

export function useGuaranteesQuery() {
  return useQuery({
    queryKey: ['guarantees'] as const,
    queryFn: getGuarantees,
    select: (res) => res.data,
    staleTime: THIRTY_MINUTES,
  });
}
