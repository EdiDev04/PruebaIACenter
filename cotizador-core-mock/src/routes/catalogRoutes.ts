import { Router, Request, Response } from 'express';
import riskClassificationData from '../fixtures/riskClassification.json';
import guaranteesData from '../fixtures/guarantees.json';
import { RiskClassification, Guarantee, ApiResponse } from '../types';

const router = Router();
const riskClassifications = riskClassificationData as RiskClassification[];
const guarantees = guaranteesData as Guarantee[];

// GET /v1/catalogs/risk-classification
router.get('/risk-classification', (_req: Request, res: Response) => {
  const response: ApiResponse<RiskClassification[]> = { data: riskClassifications };
  res.status(200).json(response);
});

// GET /v1/catalogs/guarantees
router.get('/guarantees', (_req: Request, res: Response) => {
  const response: ApiResponse<Guarantee[]> = { data: guarantees };
  res.status(200).json(response);
});

export default router;
