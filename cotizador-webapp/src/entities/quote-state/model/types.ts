export interface QuoteStateDto {
  folioNumber: string;
  quoteStatus: 'draft' | 'in_progress' | 'calculated';
  version: number;
  progress: ProgressDto;
  locations: LocationsStateDto;
  readyForCalculation: boolean;
  calculationResult: CalculationResultDto | null;
}

export interface ProgressDto {
  generalInfo: boolean;
  layoutConfiguration: boolean;
  locations: boolean;
  coverageOptions: boolean;
}

export interface LocationsStateDto {
  total: number;
  calculable: number;
  incomplete: number;
  alerts: LocationAlertDto[];
}

export interface LocationAlertDto {
  index: number;
  locationName: string;
  missingFields: string[];
}

export interface CalculationResultDto {
  netPremium: number;
  commercialPremiumBeforeTax: number;
  commercialPremium: number;
  premiumsByLocation: LocationPremiumDto[];
}

export interface LocationPremiumDto {
  locationIndex: number;
  locationName: string;
  netPremium: number;
  validationStatus: string;
  coveragePremiums: CoveragePremiumDto[];
}

export interface CoveragePremiumDto {
  guaranteeKey: string;
  insuredAmount: number;
  rate: number;
  premium: number;
}
