import { httpClient } from '@/shared/api';

export interface RiskClassification {
  code: string;
  description: string;
  factor: number;
}

export interface RiskClassificationResponse {
  data: RiskClassification[];
}

export const getRiskClassifications = (): Promise<RiskClassificationResponse> =>
  httpClient.get<RiskClassificationResponse>('/v1/catalogs/risk-classification');
