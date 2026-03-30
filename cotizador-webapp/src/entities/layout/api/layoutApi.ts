import { httpClient } from '@/shared/api';
import { endpoints } from '@/shared/api/endpoints';
import type { LayoutConfigurationDto, UpdateLayoutRequest } from '../model/types';

export interface LayoutResponse {
  data: LayoutConfigurationDto;
}

export const getLayout = (folio: string): Promise<LayoutResponse> =>
  httpClient.get<LayoutResponse>(endpoints.layout.get(folio));

export const updateLayout = (
  folio: string,
  request: UpdateLayoutRequest
): Promise<LayoutResponse> =>
  httpClient.put<LayoutResponse>(endpoints.layout.update(folio), request);
