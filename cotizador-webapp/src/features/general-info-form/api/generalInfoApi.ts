import { httpClient } from '@/shared/api';
import type { GeneralInfoPayload } from '../model/generalInfoSchema';

export interface GeneralInfoResponse {
  data: {
    insuredData: {
      name: string;
      taxId: string;
      email?: string;
      phone?: string;
    };
    conductionData: {
      subscriberCode: string;
      officeName: string;
    };
    agentCode: string;
    businessType: string;
    riskClassification: string;
    version: number;
  };
}

export const getGeneralInfo = (folioNumber: string): Promise<GeneralInfoResponse> =>
  httpClient.get<GeneralInfoResponse>(`/v1/quotes/${encodeURIComponent(folioNumber)}/general-info`);

export const putGeneralInfo = (
  folioNumber: string,
  payload: GeneralInfoPayload
): Promise<GeneralInfoResponse> => {
  const body = {
    insuredData: {
      name: payload.name,
      taxId: payload.taxId,
      email: payload.email || undefined,
      phone: payload.phone || undefined,
    },
    conductionData: {
      subscriberCode: payload.subscriberCode,
      officeName: payload.officeName,
    },
    agentCode: payload.agentCode,
    businessType: payload.businessType,
    riskClassification: payload.riskClassification,
    version: payload.version,
  };
  return httpClient.put<GeneralInfoResponse>(
    `/v1/quotes/${encodeURIComponent(folioNumber)}/general-info`,
    body
  );
};
