import { httpClient } from '@/shared/api';

export interface CreatedFolioResponse {
  data: {
    folioNumber: string;
    quoteStatus: 'draft' | 'in_progress' | 'calculated' | 'closed';
    version: number;
    metadata: {
      createdAt: string;
      lastWizardStep: number;
    };
  };
}

export const createFolio = (idempotencyKey: string): Promise<CreatedFolioResponse> =>
  httpClient.post<CreatedFolioResponse>('/v1/folios', {}, { 'Idempotency-Key': idempotencyKey });
