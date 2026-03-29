import { Router, Request, Response } from 'express';
import fireTariffsData from '../fixtures/fireTariffs.json';
import catTariffsData from '../fixtures/catTariffs.json';
import fhmTariffsData from '../fixtures/fhmTariffs.json';
import electronicEquipmentFactorsData from '../fixtures/electronicEquipmentFactors.json';
import calculationParametersData from '../fixtures/calculationParameters.json';
import {
  FireTariff,
  CatTariff,
  FhmTariff,
  ElectronicEquipmentFactor,
  CalculationParameters,
  ApiResponse,
  ApiError,
} from '../types';

const router = Router();

const fireTariffs = fireTariffsData as FireTariff[];
const catTariffs = catTariffsData as CatTariff[];
const fhmTariffs = fhmTariffsData as FhmTariff[];
const electronicEquipmentFactors = electronicEquipmentFactorsData as ElectronicEquipmentFactor[];
const calculationParameters = calculationParametersData as CalculationParameters;

// GET /v1/tariffs/calculation-parameters — must be registered before /:type
router.get('/calculation-parameters', (_req: Request, res: Response) => {
  const response: ApiResponse<CalculationParameters> = { data: calculationParameters };
  res.status(200).json(response);
});

// GET /v1/tariffs/:type  (fire | cat | fhm | electronic-equipment)
router.get('/:type', (req: Request, res: Response) => {
  const { type } = req.params;

  switch (type) {
    case 'fire': {
      const response: ApiResponse<FireTariff[]> = { data: fireTariffs };
      res.status(200).json(response);
      break;
    }
    case 'cat': {
      const response: ApiResponse<CatTariff[]> = { data: catTariffs };
      res.status(200).json(response);
      break;
    }
    case 'fhm': {
      const response: ApiResponse<FhmTariff[]> = { data: fhmTariffs };
      res.status(200).json(response);
      break;
    }
    case 'electronic-equipment': {
      const response: ApiResponse<ElectronicEquipmentFactor[]> = { data: electronicEquipmentFactors };
      res.status(200).json(response);
      break;
    }
    default: {
      const error: ApiError = {
        type: 'TariffNotFoundException',
        message: `Tariff type "${type}" not found. Valid types: fire, cat, fhm, electronic-equipment`,
      };
      res.status(404).json(error);
    }
  }
});

export default router;
