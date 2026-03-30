export interface ZipCodeDto {
  zipCode: string;
  state: string;
  municipality: string;
  neighborhood: string;
  city: string;
  catZone: string;
  technicalLevel: number;
}

export interface ZipCodeResponse {
  data: ZipCodeDto;
}
