import { httpClient, endpoints } from '@/shared/api';
import type { BusinessLinesResponse } from '../model/types';

export const getBusinessLines = (): Promise<BusinessLinesResponse> =>
  httpClient.get<BusinessLinesResponse>(endpoints.businessLines);
