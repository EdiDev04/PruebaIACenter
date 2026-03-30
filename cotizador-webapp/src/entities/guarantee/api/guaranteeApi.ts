import { httpClient, endpoints } from '@/shared/api';
import type { GuaranteesResponse } from '../model/types';

export const getGuarantees = (): Promise<GuaranteesResponse> =>
  httpClient.get<GuaranteesResponse>(endpoints.catalogs.guarantees);
