export interface LocationGuaranteeDto {
  guaranteeKey: string;
  insuredAmount: number;
}

export interface BusinessLineDto {
  code: string;
  description: string;
  fireKey: string;
  riskLevel: string;
}

export interface LocationDto {
  index: number;
  locationName: string;
  address: string;
  zipCode: string;
  state: string;
  municipality: string;
  neighborhood: string;
  city: string;
  constructionType: string;
  level: number;
  constructionYear: number;
  locationBusinessLine: BusinessLineDto | null;
  guarantees: LocationGuaranteeDto[];
  catZone: string;
  blockingAlerts: string[];
  validationStatus: 'calculable' | 'incomplete';
}

export interface LocationsResponse {
  data: {
    locations: LocationDto[];
    version: number;
  };
}

export interface LocationSummaryDto {
  index: number;
  locationName: string;
  validationStatus: 'calculable' | 'incomplete';
  blockingAlerts: string[];
}

export interface LocationsSummaryResponse {
  data: {
    locations: LocationSummaryDto[];
    totalCalculable: number;
    totalIncomplete: number;
    version: number;
  };
}

export interface UpdateLocationsRequest {
  locations: LocationDto[];
  version: number;
}

export interface PatchLocationRequest {
  locationName?: string;
  address?: string;
  zipCode?: string;
  state?: string;
  municipality?: string;
  neighborhood?: string;
  city?: string;
  catZone?: string;
  constructionType?: string;
  level?: number;
  constructionYear?: number;
  locationBusinessLine?: BusinessLineDto | null;
  guarantees?: LocationGuaranteeDto[];
  version: number;
}

export interface PatchLocationResponse {
  data: LocationDto & { version: number };
}
