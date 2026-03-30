import { httpClient, endpoints } from '@/shared/api';
import type { ZipCodeResponse } from '../model/types';

export const getZipCode = (cp: string): Promise<ZipCodeResponse> =>
  httpClient.get<ZipCodeResponse>(endpoints.zipCode.get(cp));
