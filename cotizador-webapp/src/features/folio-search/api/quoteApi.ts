import { httpClient } from '@/shared/api';

export interface FolioSummaryResponse {
  data: {
    folioNumber: string;
    quoteStatus: 'draft' | 'in_progress' | 'calculated' | 'closed';
    version: number;
    metadata: {
      lastWizardStep: number;
      createdAt: string;
      updatedAt?: string;
    };
  };
}

export const getQuote = (folioNumber: string): Promise<FolioSummaryResponse> =>
  httpClient.get<FolioSummaryResponse>(`/v1/quotes/${encodeURIComponent(folioNumber)}`);
