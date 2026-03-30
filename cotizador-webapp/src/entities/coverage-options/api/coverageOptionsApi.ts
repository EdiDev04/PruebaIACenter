import { httpClient, endpoints } from '@/shared/api';
import type { CoverageOptionsResponse, UpdateCoverageOptionsRequest } from '../model/types';

export const getCoverageOptions = (folio: string): Promise<CoverageOptionsResponse> =>
  httpClient.get<CoverageOptionsResponse>(endpoints.coverageOptions.get(folio));

export const updateCoverageOptions = (
  folio: string,
  body: UpdateCoverageOptionsRequest,
): Promise<CoverageOptionsResponse> =>
  httpClient.put<CoverageOptionsResponse>(endpoints.coverageOptions.update(folio), body);
