import { httpClient, endpoints } from '@/shared/api';
import type {
  LocationsResponse,
  LocationsSummaryResponse,
  UpdateLocationsRequest,
  PatchLocationRequest,
  PatchLocationResponse,
} from '../model/types';

export const getLocations = (folio: string): Promise<LocationsResponse> =>
  httpClient.get<LocationsResponse>(endpoints.locations.list(folio));

export const getLocationsSummary = (folio: string): Promise<LocationsSummaryResponse> =>
  httpClient.get<LocationsSummaryResponse>(endpoints.locations.summary(folio));

export const updateLocations = (
  folio: string,
  body: UpdateLocationsRequest,
): Promise<LocationsResponse> =>
  httpClient.put<LocationsResponse>(endpoints.locations.update(folio), body);

export const patchLocation = (
  folio: string,
  index: number,
  body: PatchLocationRequest,
): Promise<PatchLocationResponse> =>
  httpClient.patch<PatchLocationResponse>(endpoints.locations.patch(folio, index), body);
