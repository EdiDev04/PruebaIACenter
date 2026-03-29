export interface Subscriber {
  code: string;
  name: string;
  office: string;
  active: boolean;
}

export interface Agent {
  code: string;
  name: string;
  region: string;
  active: boolean;
}

export interface BusinessLine {
  code: string;
  description: string;
  fireKey: string;
  riskLevel: string;
}

export interface ZipCodeData {
  zipCode: string;
  state: string;
  municipality: string;
  neighborhood: string;
  city: string;
  catZone: string;
  technicalLevel: number;
}

export interface RiskClassification {
  code: string;
  description: string;
  factor: number;
}

export interface Guarantee {
  key: string;
  name: string;
  description: string;
  category: string;
  requiresInsuredAmount: boolean;
}

export interface FireTariff {
  fireKey: string;
  baseRate: number;
  description: string;
}

export interface CatTariff {
  zone: string;
  tevFactor: number;
  fhmFactor: number;
}

export interface FhmTariff {
  group: number;
  zone: string;
  condition: string;
  rate: number;
}

export interface ElectronicEquipmentFactor {
  equipmentClass: string;
  zoneLevel: number;
  factor: number;
}

export interface CalculationParameters {
  expeditionExpenses: number;
  agentCommission: number;
  issuingRights: number;
  iva: number;
  surcharges: number;
  effectiveDate: string;
}

export interface FolioResponse {
  folioNumber: string;
}

export interface ApiResponse<T> {
  data: T;
}

export interface ApiError {
  type: string;
  message: string;
}
