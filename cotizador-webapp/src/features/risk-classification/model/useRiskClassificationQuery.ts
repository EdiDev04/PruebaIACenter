import { useQuery } from '@tanstack/react-query';
import { getRiskClassifications } from '../api/riskClassificationApi';

export function useRiskClassificationQuery() {
  return useQuery({
    queryKey: ['risk-classification'],
    queryFn: getRiskClassifications,
    select: (res) => res.data as string[],
    staleTime: 1000 * 60 * 10,
  });
}
