import { useQuery } from '@tanstack/react-query';
import { getSubscribers } from '../api/subscriberApi';
import type { Subscriber } from '../api/subscriberApi';

export function useSubscribersQuery() {
  return useQuery({
    queryKey: ['subscribers'],
    queryFn: getSubscribers,
    select: (res) => res.data as Subscriber[],
    staleTime: 1000 * 60 * 10,
  });
}
