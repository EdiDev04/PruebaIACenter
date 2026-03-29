import { httpClient } from '@/shared/api';

export interface Subscriber {
  code: string;
  name: string;
  office: string;
  active: boolean;
}

export interface SubscribersResponse {
  data: Subscriber[];
}

export const getSubscribers = (): Promise<SubscribersResponse> =>
  httpClient.get<SubscribersResponse>('/v1/subscribers');
