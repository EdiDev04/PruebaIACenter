import { httpClient } from '@/shared/api';

export interface RiskClassificationResponse {
  data: string[];
}

export const getRiskClassifications = (): Promise<RiskClassificationResponse> =>
  httpClient.get<RiskClassificationResponse>('/v1/catalogs/risk-classification');
