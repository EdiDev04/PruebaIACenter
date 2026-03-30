import { useQuery } from '@tanstack/react-query';
import { getBusinessLines } from '../api/businessLineApi';

export function useBusinessLinesQuery() {
  return useQuery({
    queryKey: ['business-lines'],
    queryFn: getBusinessLines,
    select: (res) => res.data,
    staleTime: 1000 * 60 * 30,
  });
}
